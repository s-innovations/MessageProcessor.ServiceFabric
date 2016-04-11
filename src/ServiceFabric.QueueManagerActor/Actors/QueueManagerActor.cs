using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
{
    public class QueueManagerActor : Actor, IQueueManagerActor, IRemindable
    {
        public const string CheckProvision = "CheckProvision";
        private const string StateKey = "mystate";
        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }

        public QueueManagerActor(IMessageClusterConfigurationStore clusterProvider)
        {
            ClusterConfigStore = clusterProvider;
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {

            if (reminderName.Equals(CheckProvision))
            {
                ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
                var clusterKey = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename;

                var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(clusterKey) as ClusterQueueInfo;

                if (State.Keys == null)
                {
                    var ns = queue.Properties.ServiceBus.AuthRuleResourceId;
                    var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());
                    var keys = await client.ListKeysAsync<ServicebusAuthorizationKeys>(ns, "2015-08-01");

                    if (keys.IsAccessible)
                    {

                        State.Keys = keys;
                        await StateManager.SetStateAsync(StateKey, State);
                    }
                }

              //  queue.Properties.ServiceBus.ServicebusNamespaceId

            }

        }


        [DataContract]
        public class ActorState
        {
            [DataMember]
            public ServicebusAuthorizationKeys Keys { get; set; }
            [DataMember]
            public bool IsStarted { get; set; }
        }

        protected override Task OnActivateAsync()
        {
            return StateManager.TryAddStateAsync(StateKey, new ActorState { Keys = null });
        }

        public async Task StartQueueLengthMonitorAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

            if (!State.IsStarted)
            {
                State.IsStarted = true;
                await StartProvisionReminder();
                await StateManager.SetStateAsync(StateKey, State);
            }
        }

        private Task StartProvisionReminder()
        {
           return RegisterReminderAsync(
                                      CheckProvision,
                                      new Byte[0],
                                      TimeSpan.FromMinutes(0),
                                      TimeSpan.FromMinutes(5));
       
        }


    }
}
