using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
{
    public class DispatcherManagerActor : Actor, IDispatcherManagerActor, IRemindable
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        public const string CheckProvision = "CheckProvision";
        private const string StateKey = "mystate";

        [DataContract]
        public class ActorState
        {
            [DataMember]
            public ServicebusAuthorizationKeys Keys { get; set; }
            [DataMember]
            public bool IsStarted { get; set; }

            [DataMember]
            public bool IsInitialized { get; internal set; }
        }

        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }

        public DispatcherManagerActor(IMessageClusterConfigurationStore clusterProvider)
        {
            ClusterConfigStore = clusterProvider;
        }

        protected override Task OnActivateAsync()
        {
            return StateManager.TryAddStateAsync(StateKey, new ActorState { Keys = null });
        }

        public async Task InitializeAsync()
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
                                       TimeSpan.FromMinutes(1));

        }

        public async Task<bool> IsInitializedAsync()
        {

            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
            return State.IsInitialized;
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals(CheckProvision))
            {
                var disPatcherId = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename; 
                var dispatcher = await ClusterConfigStore.GetMessageClusterResourceAsync(disPatcherId) as ClusterDispatcherInfo;

                ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

                if (State.Keys == null || !State.Keys.IsAccessible)
                {

                    var authRuleResourceId = dispatcher.Properties.ServiceBus.AuthRuleResourceId;
                    var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());
                    State.Keys = await client.ListKeysAsync<ServicebusAuthorizationKeys>(authRuleResourceId, "2015-08-01");

                   
                }

                if (!State.Keys.IsAccessible)
                {
                    Logger.Warn("Servicebus keys  are not accessible");
                    return;
                }

                Logger.Debug($"Setting up {dispatcher.Name}");
                var ns = NamespaceManager.CreateFromConnectionString(State.Keys.PrimaryConnectionString);
                var filters = dispatcher.Properties.CorrelationFilters;
                for (int i = 0, ii = dispatcher.Properties.TopicScaleCount; i < ii; ++i)
                {
                    var topicPath = dispatcher.Name + i.ToString("D3");
                    if (!await ns.TopicExistsAsync(topicPath))
                    {
                        await ns.CreateTopicAsync(topicPath);
                    }

                    foreach (var correlationFilter in filters.Keys)
                    {
                        var queueId = disPatcherId.Split('/'); queueId[queueId.Length - 1] = filters[correlationFilter];
                        var queueActor = ActorProxy.Create<IQueueManagerActor>(new ActorId(string.Join("/", queueId)));

                        var forwardPath = await queueActor.GetPathAsync();
                        var name = dispatcher.Name + "2" + forwardPath;

                        if (!await ns.SubscriptionExistsAsync(topicPath, name))
                        {
                            Logger.DebugFormat($"Creating Subscription for {name}");
                            await ns.CreateSubscriptionAsync(
                                new SubscriptionDescription(topicPath, name)
                                {
                                    ForwardTo = forwardPath,
                                }, new CorrelationFilter(correlationFilter));
                        }
                        else
                        {
                            Logger.DebugFormat($"Subscription '{name}' already created");
                        }
                        

                    }
                }

                State.IsInitialized = true;
                await StateManager.SetStateAsync(StateKey, State);
                await UnregisterReminderAsync(GetReminder(reminderName));

            }
        }
    }
}
