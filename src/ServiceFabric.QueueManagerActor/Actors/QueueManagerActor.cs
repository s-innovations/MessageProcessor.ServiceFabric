using System;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using Newtonsoft.Json;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
{

    public class TopicManagerActor : Actor, ITopicManagerActor, IRemindable
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        private const string MonitoringReminderName = "monitoringCheck";


        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }

        public TopicManagerActor(IMessageClusterConfigurationStore clusterProvider)
        {
            ClusterConfigStore = clusterProvider;
        }

        public Task<bool> IsInitializedAsync()
        {
            return StateManager.GetStateAsync<bool>("IsInitialized");
        }

        protected override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        public async Task StartMonitoringAsync()
        {
            

            if (!await StateManager.GetStateAsync<bool>("IsMonitoring"))
            {
               
                await StartProvisionReminder();
                await StateManager.SetStateAsync("IsMonitoring", true);
            }
        }

        private Task StartProvisionReminder()
        {
            return RegisterReminderAsync(
                                      MonitoringReminderName,
                                       new Byte[0],
                                       TimeSpan.FromMinutes(0),
                                       TimeSpan.FromMinutes(5));

        }

        public async Task StopMonitoringAsync()
        {
            if (await StateManager.GetStateAsync<bool>("IsMonitoring"))
            {
                await UnregisterReminderAsync(GetReminder(MonitoringReminderName));
                await StateManager.SetStateAsync("IsMonitoring", false);
               
            }
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {

            if (reminderName.Equals(MonitoringReminderName))
            {
                try
                {
                    var nodeKey = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename; 
                    var topic = await ClusterConfigStore.GetMessageClusterResourceAsync(nodeKey) as TopicInfo;

                    if(!await IsInitializedAsync())
                    {

                        var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());

                        var keysState = await StateManager.TryGetStateAsync<ServicebusAuthorizationKeys>("keys");
                        ServicebusAuthorizationKeys keys;

                        if(!keysState.HasValue|| !keysState.Value.IsAccessible)
                        {
                            var authRuleResourceId = topic.Properties.ServiceBus.AuthRuleResourceId;

                            keys = await client.ListKeysAsync<ServicebusAuthorizationKeys>(authRuleResourceId, "2015-08-01");
                            await StateManager.SetStateAsync("keys", keys);
                            

                        }else
                        {
                            keys = keysState.Value;
                        }

                        if (!keys.IsAccessible)
                        {
                            Logger.Error("Servicebus keys  are not accessible");
                            return;
                        }


                         var ns = NamespaceManager.CreateFromConnectionString(keys.PrimaryConnectionString);
                        
                            for (int i = 0, ii = topic.Properties.TopicScaleCount; i < ii; ++i)
                            {
                                var topicPath = topic.Name + i.ToString("D3");
                                if (!await ns.TopicExistsAsync(topicPath))
                                {
                                    await ns.CreateTopicAsync(topicPath);
                                }
                            }

                        await StateManager.SetStateAsync("IsInitialized", true);
                        await UnregisterReminderAsync(GetReminder(reminderName));

                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Reminder Error:", ex);
                    throw;
                }


            }

        }
    }

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
            //     [DataMember]
            //     public DateTimeOffset LastActive { get; set; }
           // [DataMember]
           // public DateTimeOffset LastScaleAction { get; set; }
            // [DataMember]
            // public DateTimeOffset LastScaleDownAction { get; set; }
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

        private FabricClient GetFabricClient()
        {
            return new FabricClient();
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {

            if (reminderName.Equals(CheckProvision))
            {
                ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
                try
                {
                    var nodeKey = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename; 
                    var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(nodeKey) as ClusterQueueInfo;

                    if (!State.IsInitialized)
                    {

                        var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());

                        if (State.Keys == null || !State.Keys.IsAccessible)
                        {

                            var authRuleResourceId = queue.Properties.ServiceBus.AuthRuleResourceId;

                            State.Keys = await client.ListKeysAsync<ServicebusAuthorizationKeys>(authRuleResourceId, "2015-08-01");
                            State.Path = queue.Name;


                        }

                        if (!State.Keys.IsAccessible)
                        {
                            Logger.Error("Servicebus keys  are not accessible");
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

                    }

                    if (State.IsInitialized)
                    {
                        var ns = NamespaceManager.CreateFromConnectionString(State.Keys.PrimaryConnectionString);

                        var sbQueue = await ns.GetQueueAsync(queue.Name);
                        Logger.Info($"Checking Queue information for {sbQueue.Path}, {sbQueue.MessageCount}, {sbQueue.MessageCountDetails.ActiveMessageCount}, {sbQueue.MessageCountDetails.DeadLetterMessageCount}, {sbQueue.MessageCountDetails.ScheduledMessageCount}, {sbQueue.MessageCountDetails.TransferDeadLetterMessageCount}, {sbQueue.MessageCountDetails.TransferMessageCount}");
                        var parts = nodeKey.Split('/');
                        var applicationName = new Uri($"fabric:/{parts[parts.Length - 2]}");
                        var serviceName = new Uri($"fabric:/{parts[parts.Length - 2]}/{parts[parts.Length-1]}");

                        var vmssManager = ActorProxy.Create<IVmssManagerActor>(new ActorId(string.Join("/", parts.Take(parts.Length - 1)) + "/" + queue.Properties.ListenerDescription.ProcessorNode));
                        var fabricClient = GetFabricClient();
                        var primNodes = 0;
                        if (queue.Properties.ListenerDescription.UsePrimaryNode)
                        {
                          var nodes=  await fabricClient.QueryManager.GetNodeListAsync();
                            primNodes = nodes.Aggregate(0, (c, p) => c + (p.NodeType == "nt1vm" ? 1 : 0));
                        }

                        await vmssManager.ReportQueueMessageCountAsync(Id.GetStringId(), sbQueue.MessageCountDetails.ActiveMessageCount, primNodes);

                        if (sbQueue.MessageCountDetails.ActiveMessageCount > 0)
                        {
                           
                            //Handle Listener Application Deployment

                           
                      
                            var listenerDescription = queue.Properties.ListenerDescription;

                            var apps = await fabricClient.QueryManager.GetApplicationListAsync(applicationName);
                            if (!apps.Any())
                            {

                                var appTypes = await fabricClient.QueryManager.GetApplicationTypeListAsync(listenerDescription.ApplicationTypeName);
                                if (!appTypes.Any(a => a.ApplicationTypeName == listenerDescription.ApplicationTypeName && a.ApplicationTypeVersion == listenerDescription.ApplicationTypeVersion))
                                {
                                    Logger.Error("The listener application was not registed with service fabric");
                                    return;
                                }

                                await fabricClient.ApplicationManager.CreateApplicationAsync(new ApplicationDescription
                                {
                                    ApplicationName = applicationName,
                                    ApplicationTypeName = listenerDescription.ApplicationTypeName,
                                    ApplicationTypeVersion = listenerDescription.ApplicationTypeVersion,
                                });

                            }



                            var registered = await fabricClient.QueryManager.GetServiceListAsync(applicationName, serviceName);


                            if (!registered.Any())
                            {

                                var serviceType =await fabricClient.QueryManager.GetServiceTypeListAsync(listenerDescription.ApplicationTypeName, listenerDescription.ApplicationTypeVersion,listenerDescription.ServiceTypeName);
                                if (!serviceType.Any())
                                {
                                    Logger.Error("The listener application service type was not registed with service fabric");
                                    return;
                                }


                                try
                                {
                                    var listenerConfiguration = new MessageProcessorOptions
                                    {
                                         ConnectionString = State.Keys.PrimaryConnectionString,
                                         QueuePath = sbQueue.Path,                                         
                                    };
                                    var placementConttraints = $"NodeTypeName == {queue.Properties.ListenerDescription.ProcessorNode}";
                                    if (queue.Properties.ListenerDescription.UsePrimaryNode)
                                    {
                                        placementConttraints += " || isPrimary == true";
                                    }
                                    
                                    await fabricClient.ServiceManager.CreateServiceAsync(new StatelessServiceDescription
                                    {
                                        ServiceTypeName = listenerDescription.ServiceTypeName, //QueueListenerService.ServiceType, // ServiceFabricConstants.ActorServiceTypes.QueueListenerActorService,
                                        ServiceName = serviceName,
                                        PartitionSchemeDescription = new UniformInt64RangePartitionSchemeDescription
                                        {
                                            PartitionCount = queue.Properties.ListenerDescription.PartitionCount,
                                            LowKey = Int64.MinValue,
                                            HighKey = Int64.MaxValue
                                        },
                                        InstanceCount = -1, //One for each node,
                                        PlacementConstraints = $"NodeTypeName == {queue.Properties.ListenerDescription.ProcessorNode}",
                                        ApplicationName = applicationName,
                                        InitializationData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(listenerConfiguration)),
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Logger.ErrorException("Could not register service for queue", ex);
                                    throw;
                                }
                            }

                        }
                        else
                        {

                           if ((DateTimeOffset.UtcNow - (XmlConvert.ToTimeSpan(queue.Properties.ListenerDescription.IdleTimeout))) > sbQueue.AccessedAt)
                            {
                              //  await vmssManager.SetCapacityAsync(0);
                               
                                var registered = await fabricClient.QueryManager.GetServiceListAsync(applicationName, serviceName);
                                if (registered.Any())
                                {
                                    await fabricClient.ServiceManager.DeleteServiceAsync(serviceName);
                                }

                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Logger.ErrorException("Reminder Error:", ex);
                    throw;
                }
                finally
                {
                    await StateManager.SetStateAsync(StateKey, State);
                }
            }

        }




        protected override Task OnActivateAsync()
        {
            return StateManager.TryAddStateAsync(StateKey, new ActorState
            {
                Keys = null,
                //    LastActive = DateTimeOffset.MinValue,
              //  LastScaleAction = DateTimeOffset.MinValue,
                //  LastScaleUpAction = DateTimeOffset.MinValue,
            });
        }

        public async Task StartMonitoringAsync()
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

        public async Task StopMonitoringAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

            if (State.IsStarted)
            {
                await UnregisterReminderAsync(GetReminder(CheckProvision));
                State.IsStarted = false;
                State.IsInitialized = false;

                await StateManager.SetStateAsync(StateKey, State);
            }
        }
    }
}
