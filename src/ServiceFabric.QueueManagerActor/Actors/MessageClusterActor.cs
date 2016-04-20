
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Tracing;
using System.Fabric;
using System.Fabric.Description;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using System.Xml.Linq;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Net.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using System.Security.Cryptography.X509Certificates;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
{
    /// <remarks>
    /// Each ActorID maps to an instance of this class.
    /// The IProjName  interface (in a separate DLL that client code can
    /// reference) defines the operations exposed by ProjName objects.
    /// </remarks>

    [StatePersistence(StatePersistence.Persisted)]
    public class MessageClusterActor : Actor, IMessageClusterActor, IRemindable
    {
        //    public const string CheckQueueSizesReminderName = "CheckQueueSizes";
        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        public const string CheckProvisionReminderName = "CheckProvision";
        private const string StateKey = "mystate";

        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }

        public MessageClusterActor(IMessageClusterConfigurationStore clusterProvider)
        {
            ClusterConfigStore = clusterProvider;
        }


        /// <summary>
        /// This class contains each actor's replicated state.
        /// Each instance of this class is serialized and replicated every time an actor's state is saved.
        /// For more information, see http://aka.ms/servicefabricactorsstateserialization
        /// </summary>
        [DataContract]
        public class ActorState
        {
            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture, "QueueManagerActor.ActorState[Count = {0}]", RunningActors?.Count ?? 0);
            }

            [DataMember]
            public Dictionary<string, string> RunningActors { get; set; }

            [DataMember]
            public bool IsInitialized { get; set; }
        }


        /// <summary>
        /// This method is called whenever an actor is activated.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            return StateManager.TryAddStateAsync(StateKey, new ActorState { RunningActors = new Dictionary<string, string>() });
        }



        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
          var clusterKey = this.Id.GetStringId();

            if (reminderName.Equals(CheckProvisionReminderName))
            {
                var state = await StateManager.GetStateAsync<ActorState>(StateKey);
                ServiceFabricEventSource.Current.ActorMessage(this, $"Checking ProvisionState of Service Fabric Cluster");
                if (!state.IsInitialized)
                {

                    var config = this.GetConfigurationInfo();

                    var azureClient = new ServiceFabricClient(new AuthenticationHeaderValue("bearer", await config.GetAccessToken()));
                    var fabricInfo = await azureClient.GetServiceFabricClusterInfoAsync(config.SubscriptionId.AsGuid(), config.ResourceGroupName, config.ClusterName);

                    if (fabricInfo.Properties.ProvisioningState == "Failed")
                    {
                        Logger.Error("Provision of cluster resource failed.");

                        fabricInfo = await azureClient.PutClusterInfoAsync(fabricInfo);

                    }
                    //If in succeeded provision state, add any missing nodes.
                    if (fabricInfo.Properties.ProvisioningState == "Succeeded")// || fabricInfo.Properties.ProvisioningState == "Failed")
                    {
                        var messageClusterConfiguration = await ClusterConfigStore.GetMessageClusterAsync(clusterKey);
                        var processorNodes = messageClusterConfiguration.Resources.OfType<ClusterProcessorNode>();
                        var updateInfo = GetUpdateInformation(fabricInfo, processorNodes);

                        var removeVMSSs = updateInfo.ShouldBeRemoved.Select(node => ActorProxy.Create<IVmssManagerActor>(new ActorId(clusterKey + "/" + node)).RemoveIfNotRemovedAsync()).ToArray();

                        //if no new nodes are to be added start queue monitoring.
                        if (updateInfo.ShouldBeRemoved.Any() || updateInfo.ShouldBeAdded.Any())
                        {
                            fabricInfo = await azureClient.PutClusterInfoAsync(updateInfo.Cluster);

                            if (fabricInfo.Properties.ProvisioningState == "Succeeded")
                            {
                                await ServiceFabricClusterProvisioned(messageClusterConfiguration.Resources);
                            }

                        }
                        else
                        {
                            await ServiceFabricClusterProvisioned(messageClusterConfiguration.Resources);
                        }


                    }


                }
                else
                {

                }
            }

        }

        private async Task ServiceFabricClusterProvisioned(IEnumerable<MessageClusterResourceBase> resources)
        {
            var queueNodes = resources.OfType<ClusterQueueInfo>();
            var allCreated = true;
            foreach (var queue in queueNodes)
            {
               

                var isVMSSCreated = await ActorProxy.Create<IVmssManagerActor>(new ActorId(this.Id.GetStringId() + "/" + queue.Properties.ListenerDescription.ProcessorNode)).CreateIfNotExistsAsync();
                if (isVMSSCreated)
                {
                    await ActorProxy.Create<IQueueManagerActor>(new ActorId(this.Id.GetStringId() + "/" + queue.Name)).StartMonitoringAsync();
                }
                else
                {
                    allCreated = false;
                }
            }

            foreach (var dispatcheer in resources.OfType<ClusterDispatcherInfo>())
            {
                var all = await Task.WhenAll(dispatcheer.Properties.CorrelationFilters.Values.Select(d => ActorProxy.Create<IQueueManagerActor>(new ActorId(this.Id.GetStringId() + "/" + d)).IsInitializedAsync()));
                if (all.All(d => d))
                {
                    await ActorProxy.Create<IDispatcherManagerActor>(new ActorId(this.Id.GetStringId() + "/" + dispatcheer.Name)).StartMonitoringAsync();
                }
                else
                {
                    allCreated = false;
                }

            }

            if (allCreated)
            {
                await UnregisterReminderAsync(GetReminder(CheckProvisionReminderName));
                var state = await StateManager.GetStateAsync<ActorState>(StateKey);
                state.IsInitialized = true;
                await StateManager.SetStateAsync(StateKey, state);
            }

        }


        public async Task<string> StartMonitoringAsync()
        {
            ServiceFabricEventSource.Current.ActorMessage(this, "StartMonitoringAsync");
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

            //$"{subscriptionid}/{resourceGroupName}/{clusterName}"
            var clusterKey = this.Id.GetStringId();
            var running = State.RunningActors.ContainsKey(clusterKey);


            if (!running)
            {
                await StartProvisionReminderAsync(clusterKey);
                State.RunningActors.Add(clusterKey, "started");
                await StateManager.SetStateAsync(StateKey, State);
                return "Started";

            }


            return "Running";
        }

        public async Task<string> StopMonitoringAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);


            var clusterKey = this.Id.GetStringId();
            var running = State.RunningActors.ContainsKey(clusterKey);

            if (running)
            {

                State.RunningActors.Remove(clusterKey);

                var messageClusterConfiguration = await ClusterConfigStore.GetMessageClusterAsync(clusterKey);
                var queueNodes = messageClusterConfiguration.Resources.OfType<ClusterQueueInfo>();

                foreach (var queue in queueNodes)
                {
                    await ActorProxy.Create<IQueueManagerActor>(new ActorId(clusterKey + "/" + queue.Name)).StopMonitoringAsync();

                }


                //  await UnregisterReminderAsync(GetReminder(CheckProvisionReminderName));
                //     await UnregisterReminderAsync(GetReminder(CheckQueueSizesReminderName));

                await StateManager.SetStateAsync(StateKey, State);

                return "stopping";
            }
            return "stopped";
        }




        private Task StartProvisionReminderAsync(string clusterKey)
        {
            return RegisterReminderAsync(
                                 CheckProvisionReminderName,
                                 Encoding.UTF8.GetBytes(clusterKey),
                                 TimeSpan.FromMinutes(0),
                                 TimeSpan.FromMinutes(1));
        }

        private static ServiceFabricClusterUpdateInformation GetUpdateInformation(ServiceFabricCluster cluster, IEnumerable<ClusterProcessorNode> allQueues)
        {
            var shouldBeAdded = allQueues.Where(f => !cluster.Properties.NodeTypes.Any(n => n.Name == f.Name)).Select(k => k.Name).ToArray();
            var shouldBeRemoved = cluster.Properties.NodeTypes.Where(n => !n.IsPrimary && !allQueues.Any(f => f.Name == n.Name)).Select(k => k.Name).ToArray();


            var update = cluster.ToDTO();
            var prim = update.Properties.NodeTypes.Single(n => n.IsPrimary);
            update.Properties.NodeTypes.AddRange(allQueues.Select(n => new ServiceFabricNode
            {
                Name = n.Name,
                PlacementProperties = n.Properties.PlacementProperties,
                Capacities = n.Properties.Capacities,
                VMInstanceCount = 1,
            }.CopyPortsFrom(prim)
            ).Where(f => !update.Properties.NodeTypes.Any(n => n.Name == f.Name)));

          //  update.Properties.NodeTypes.RemoveAll(n => shouldBeRemoved.Contains(n.Name));

            return new ServiceFabricClusterUpdateInformation { Cluster = update, ShouldBeAdded = shouldBeAdded, ShouldBeRemoved = shouldBeRemoved };
        }


    }

    public class ServiceFabricClusterUpdateInformation
    {
        public ServiceFabricCluster Cluster { get; set; }
        public string[] ShouldBeAdded { get; set; }
        public string[] ShouldBeRemoved { get; set; }

    }
}

