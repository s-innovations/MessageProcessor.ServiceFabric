
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
        public const string CheckQueueSizesReminderName = "CheckQueueSizes";
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

            if (reminderName.Equals(CheckQueueSizesReminderName))
            {
                var messageClusterConfiguration = await ClusterConfigStore.GetMessageClusterAsync(clusterKey);
                var queueNodes = messageClusterConfiguration.Resources.OfType<ClusterQueueInfo>();
                var startTasks = queueNodes.Select(node => ActorProxy.Create<IQueueManagerActor>(new ActorId(clusterKey + "/" + node.Name)).StartQueueLengthMonitorAsync()).ToArray();
                await Task.WhenAll(startTasks);
            }
            else if (reminderName.Equals(CheckProvisionReminderName))
            {

                ServiceFabricEventSource.Current.ActorMessage(this, $"Checking ProvisionState of Service Fabric Cluster");

                var config = this.GetConfigurationInfo();
               
                var azureClient = new ServiceFabricClient(new AuthenticationHeaderValue("bearer", await config.GetAccessToken()));
                var fabricInfo = await azureClient.GetServiceFabricClusterInfoAsync(config.SubscriptionId.AsGuid(), config.ResourceGroupName, config.ClusterName);
               
                //If in succeeded provision state, add any missing nodes.
                if (fabricInfo.Properties.ProvisioningState == "Succeeded")
                {
                    var messageClusterConfiguration = await ClusterConfigStore.GetMessageClusterAsync(clusterKey);
                    var queueNodes = messageClusterConfiguration.Resources.OfType<ClusterQueueInfo>();
                    ServiceFabricCluster update = GetUpdateInformation(fabricInfo, queueNodes);
                    var nodesNotFound = queueNodes.Where(n => !fabricInfo.Properties.NodeTypes.Any(nn => nn.Name == n.Name));

                    //Ensure All VMSS Nodes have been created
                    var vmssNodes = queueNodes.Select(node => ActorProxy.Create<IVmssManagerActor>(new ActorId(clusterKey + "/" + node.Name)).CreateIfNotExistsAsync()).ToArray();
                    
                    //if no new nodes are to be added start queue monitoring.
                    if (nodesNotFound.Any())
                    {
                        fabricInfo = await azureClient.PutClusterInfoAsync(update);

                        if (fabricInfo.Properties.ProvisioningState == "Succeeded")
                        {
                            await StartQueueMonitorReminderAsync(clusterKey);
                        }

                    }
                    else {                       
                        await StartQueueMonitorReminderAsync(clusterKey);
                    }

                    await Task.WhenAll(vmssNodes);
                }


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

                await UnregisterReminderAsync(GetReminder(CheckProvisionReminderName));
                await UnregisterReminderAsync(GetReminder(CheckQueueSizesReminderName));

                await StateManager.SetStateAsync(StateKey, State);

                return "stopping";
            }
            return "stopped";
        }


        private async Task StartQueueMonitorReminderAsync(string clusterKey)
        {
            await UnregisterReminderAsync(GetReminder(CheckProvisionReminderName));

            await RegisterReminderAsync(
                                CheckQueueSizesReminderName,
                                Encoding.UTF8.GetBytes(clusterKey),
                                TimeSpan.FromMinutes(0),
                                TimeSpan.FromMinutes(10));
        }

        private Task StartProvisionReminderAsync(string clusterKey)
        {
           return RegisterReminderAsync(
                                CheckProvisionReminderName,
                                Encoding.UTF8.GetBytes(clusterKey),
                                TimeSpan.FromMinutes(0),
                                TimeSpan.FromMinutes(1));
        }

        private static ServiceFabricCluster GetUpdateInformation(ServiceFabricCluster cluster, IEnumerable<ClusterQueueInfo> newNodes)
        {
            var update = cluster.ToDTO();
            var prim = update.Properties.NodeTypes.Single(n => n.IsPrimary);
            update.Properties.NodeTypes.AddRange(newNodes.Select(n => new ServiceFabricNode
            {
                Name = n.Name,
                PlacementProperties = n.Properties.PlacementProperties
            }.CopyPortsFrom(prim)
            ).Where(f => !update.Properties.NodeTypes.Any(n => n.Name == f.Name)));
            return update;
        }

      
    }
}

