
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
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using System.Security.Cryptography.X509Certificates;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
{
    public class DebugTimer : IDisposable
    {
        Actor actor;
        string message;
        public DebugTimer(Actor actor,string message)
        {
            this.actor = actor;
            this.message = message;

        }
        public Stopwatch sw = Stopwatch.StartNew();
        public void Dispose()
        {
            sw.Stop();
            ServiceFabricEventSource.Current.ActorMessage(actor,$"{sw.Elapsed} : {message}");
        }
    }
    [StatePersistence(StatePersistence.Persisted)]
    public class MessageClusterActor : Actor, IMessageClusterActor, IRemindable
    {
 
        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        private const string CheckProvisionReminderName = "CheckProvision";
       

        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore PersistantStore { get; private set; }

        public MessageClusterActor(IMessageClusterConfigurationStore clusterProvider)
        {
            PersistantStore = clusterProvider;
        }

        
        /// <summary>
        /// This method is called whenever an actor is activated.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            using (new DebugTimer(this, "OnActivateAsync"))
            {
                if (await PersistantStore.ClusterExistsAsync((this.Id.GetStringId())))
                {
                    var cluster = await PersistantStore.GetMessageClusterAsync(this.Id.GetStringId());
                    await StateManager.TrySetJsonModelAsync("model", cluster);
                }

            }
          //  await StateManager.TryAddStateAsync(StateKey, new ActorState { RunningActors = new Dictionary<string, string>() });
        }


        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            using (new DebugTimer(this, "ReceiveReminderAsync"))
            {
                var clusterKey = this.Id.GetStringId();

                if (reminderName.Equals(CheckProvisionReminderName))
                {
                    //   var isInitialized = await StateManager.TryGetStateAsync<bool>("isProvisioned");
                    ServiceFabricEventSource.Current.ActorMessage(this, $"Checking ProvisionState of Service Fabric Cluster");
                    if (!await this.IsInitialized())
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
                            var messageClusterConfiguration = await StateManager.GetJsonModelAsync<MessageClusterResource>("model");

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
                await StateManager.SetStateAsync("isInitialized", true);
            }

        }
        public async Task<JsonModel<MessageClusterResource>> GetModelAsync()
        {
            using (new DebugTimer(this, "GetModelAsync"))
            {
                var state = await StateManager.TryGetStateAsync<JsonModel<MessageClusterResource>>("model");
                if (state.HasValue)
                {
                    return state.Value;
                }

                var cluster = await StateManager.GetJsonModelAsync<MessageClusterResource>(this.Id.GetStringId());

                return await StateManager.SetJsonModelAsync("model", cluster);
            }
        }
        public async Task<JsonModel<MessageClusterResource>> UpdateModelAsync(JsonModel<MessageClusterResource> model)
        {
            using (new DebugTimer(this, "UpdateModelAsync"))
            {
                var cluster = model.Model;


                var provisioningStatus = await this.GetProvisioningStatus();

                if (provisioningStatus == ClusterActorProvisioningStatus.Unprovisioned)
                {
                    provisioningStatus = await StartMonitoringAsync();
                }
                else if (!await this.IsInitialized())
                {
                    provisioningStatus = ClusterActorProvisioningStatus.Initializing;
                }
                else
                {
                    var config = this.GetConfigurationInfo();
                    var azureClient = new ServiceFabricClient(new AuthenticationHeaderValue("bearer", await config.GetAccessToken()));
                    var fabricInfo = await azureClient.GetServiceFabricClusterInfoAsync(config.SubscriptionId.AsGuid(), config.ResourceGroupName, config.ClusterName);

                    var processorNodes = cluster.Resources.OfType<ClusterProcessorNode>();
                    var updateInfo = GetUpdateInformation(fabricInfo, processorNodes);

                    //if no new nodes are to be added start queue monitoring.
                    if (updateInfo.ShouldBeRemoved.Any() || updateInfo.ShouldBeAdded.Any())
                    {
                        provisioningStatus = await this.SetProvisioningStatus("Updating");
                        await StartProvisionReminderAsync();

                    }

                }

                cluster.ProvisioningState = provisioningStatus;

                await PersistantStore.PutMessageClusterAsync(Id.GetStringId(), cluster);
                var modelState = await StateManager.SetJsonModelAsync("model", cluster);

                return modelState;
            }

        }

        public async Task<string> StartMonitoringAsync()
        {
            using (new DebugTimer(this, "StartMonitoringAsync"))
            {
                ServiceFabricEventSource.Current.ActorMessage(this, "StartMonitoringAsync");

                if (!await this.IsInitialized())
                {
                    await StartProvisionReminderAsync();
                    return ClusterActorProvisioningStatus.Initializing;
                }

                var provisioningStatus = await this.GetProvisioningStatus();

                if (provisioningStatus != ClusterActorProvisioningStatus.Provisioning)
                {
                    await StartProvisionReminderAsync();
                    return await this.SetProvisioningStatus(ClusterActorProvisioningStatus.Updating);

                }


                return provisioningStatus;
            }
        }

        public async Task<string> StopMonitoringAsync()
        {
          //  ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);


            var clusterKey = this.Id.GetStringId();
            var provisioningStatus = await this.GetProvisioningStatus();

            if (provisioningStatus != ClusterActorProvisioningStatus.Unprovisioned)
            {

           //     State.RunningActors.Remove(clusterKey);

                var messageClusterConfiguration = await StateManager.GetJsonModelAsync<MessageClusterResource>("model");
                var queueNodes = messageClusterConfiguration.Resources.OfType<ClusterQueueInfo>();

                foreach (var queue in queueNodes)
                {
                    await ActorProxy.Create<IQueueManagerActor>(new ActorId(clusterKey + "/" + queue.Name)).StopMonitoringAsync();

                }


                //  await UnregisterReminderAsync(GetReminder(CheckProvisionReminderName));
                //     await UnregisterReminderAsync(GetReminder(CheckQueueSizesReminderName));

           //     await StateManager.SetStateAsync(StateKey, State);

                return "stopping";
            }
            return ClusterActorProvisioningStatus.Unprovisioned;
        }




        private Task StartProvisionReminderAsync()
        {
            return RegisterReminderAsync(
                                 CheckProvisionReminderName,
                                 new byte[0],
                                 TimeSpan.FromSeconds(5),
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

