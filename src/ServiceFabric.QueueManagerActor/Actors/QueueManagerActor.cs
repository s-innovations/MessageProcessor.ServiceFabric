using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Actors.Runtime;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
{



    public class QueueManagerActor : Actor, IQueueManagerActor, IRemindable
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
            public bool IsInitialized { get; set; }
            [DataMember]
            public string Path { get; set; }
        }

        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }

        public QueueManagerActor(IMessageClusterConfigurationStore clusterProvider)
        {
            ClusterConfigStore = clusterProvider;
        }
        public async Task<string> GetPathAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
            return State.Path;

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
                ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

                var clusterKey = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename; 
                var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(clusterKey) as ClusterQueueInfo;

                if (State.Keys == null || !State.Keys.IsAccessible)
                {

                    var authRuleResourceId = queue.Properties.ServiceBus.AuthRuleResourceId;
                    var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());
                    State.Keys = await client.ListKeysAsync<ServicebusAuthorizationKeys>(authRuleResourceId, "2015-08-01");
                    State.Path = queue.Name;


                }

                if (!State.Keys.IsAccessible)
                {
                    Logger.Warn("Servicebus keys  are not accessible");
                    return;
                }


                //  queue.Properties.ServiceBus.ServicebusNamespaceId
                var ns = NamespaceManager.CreateFromConnectionString(State.Keys.PrimaryConnectionString);
                if (!await ns.QueueExistsAsync(queue.Name))
                {
                    var qd = queue.Properties.QueueDescription;
                    if (qd == null)
                    {
                        Logger.Warn("Servicebus queue do not exist");
                        return;
                    }

                    var q = new QueueDescription(queue.Name);
                    if (qd.AutoDeleteOnIdle.IsPresent())
                        q.AutoDeleteOnIdle = XmlConvert.ToTimeSpan(qd.AutoDeleteOnIdle);
                    if (qd.DefaultMessageTimeToLive.IsPresent())
                        q.DefaultMessageTimeToLive = XmlConvert.ToTimeSpan(qd.DefaultMessageTimeToLive);
                    if (qd.DuplicateDetectionHistoryTimeWindow.IsPresent())
                    {
                        q.RequiresDuplicateDetection = true;
                        q.DuplicateDetectionHistoryTimeWindow = XmlConvert.ToTimeSpan(qd.DuplicateDetectionHistoryTimeWindow);
                    }
                    q.EnableBatchedOperations = qd.EnableBatchedOperations;
                    q.EnableDeadLetteringOnMessageExpiration = qd.EnableDeadLetteringOnMessageExpiration;
                    q.EnableExpress = qd.EnableExpress;
                    q.EnablePartitioning = qd.EnablePartitioning;
                    if (qd.ForwardDeadLetteredMessagesTo.IsPresent())
                    {
                        q.ForwardDeadLetteredMessagesTo = qd.ForwardDeadLetteredMessagesTo;
                    }
                    if (qd.ForwardTo.IsPresent())
                    {
                        q.ForwardTo = qd.ForwardTo;
                    }

                    await ns.CreateQueueAsync(q);
                }

                State.IsInitialized = true;
                await StateManager.SetStateAsync(StateKey, State);

                var sbQueue = await ns.GetQueueAsync(queue.Name);
                Logger.Info($"Checking Queue information for {sbQueue.Path}, {sbQueue.MessageCount}, {sbQueue.MessageCountDetails.ActiveMessageCount}, {sbQueue.MessageCountDetails.DeadLetterMessageCount}, {sbQueue.MessageCountDetails.ScheduledMessageCount}, {sbQueue.MessageCountDetails.TransferDeadLetterMessageCount}, {sbQueue.MessageCountDetails.TransferMessageCount}");




            }

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
