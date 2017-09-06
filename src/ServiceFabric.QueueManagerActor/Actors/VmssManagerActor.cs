using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Tracing;
using SInnovations.Azure.ResourceManager;
using SInnovations.Azure.ResourceManager.TemplateActions;
using Microsoft.ServiceFabric.Actors;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
{

    public class VmssArmTemplateOptions
    {
        public int Capacity { get; set; }

        public string ClusterResourceGroupName { get; set; }
    }
    public class VmssArmTemplate : ResourceSource
    {
        // private ClusterQueueInfoProperties properties;

        public VmssArmTemplate(ClusterVmssInfo properties, int? capacity = null) : this(new VmssArmTemplateOptions { Capacity = capacity ?? 0 })
        {

            Add(new JsonPathSetter("variables.vmImagePublisher", properties.VmImagePublisher));
            Add(new JsonPathSetter("variables.vmImageOffer", properties.VmImageOffer));
            Add(new JsonPathSetter("variables.vmImageSku", properties.VmImageSku));
            Add(new JsonPathSetter("variables.vmImageVersion", properties.VmImageVersion));
            Add(new JsonPathSetter("variables.vmNodeTypeSize", properties.Name));
            Add(new JsonPathSetter("variables.vmNodeTypeTier", properties.Tier));


        }

        public VmssArmTemplate(VmssArmTemplateOptions options = null) : base(ServiceFabricConstants.VmssTemplate, typeof(ServiceFabricConstants).Assembly)
        {
            options = options ?? new VmssArmTemplateOptions();

            Add(new JsonPathSetter("variables.capacity", options.Capacity.ToString()));
            if (options.ClusterResourceGroupName.IsPresent())
                Add(new JsonPathSetter("variables.clusterResourceGroupName", options.ClusterResourceGroupName));


        }



    }

    public class VmssManagerActor : Actor, IVmssManagerActor, IRemindable
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();

        public const string CheckProvision = "CheckProvision";
        private const string StateKey = "mystate";


        [DataContract]
        public class ActorState
        {
            [DataMember]
            public int Capacity { get; internal set; }
            [DataMember]
            public bool IsInitialized { get; set; }

            [DataMember]
            public bool IsProvisioning { get; set; }
            [DataMember]
            public string VMSSResourceId { get; set; }

            [DataMember]
            public Dictionary<string, long> QueeuActors { get; set; } = new Dictionary<string, long>();

            [DataMember]
            public DateTimeOffset LastScaleAction { get; set; } = DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }

        public VmssManagerActor(IMessageClusterConfigurationStore clusterProvider, ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
            ClusterConfigStore = clusterProvider;
        }




        protected override Task OnActivateAsync()
        {
            return StateManager.TryAddStateAsync(StateKey, new ActorState { });
        }
        protected override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        private async Task StartProvisionReminderAsync(bool initial = true)
        {
            if (initial)
            {
                ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
                State.IsProvisioning = true;
                await StateManager.SetStateAsync(StateKey, State);
            }

            await RegisterReminderAsync(
                                     CheckProvision,
                                     new Byte[0],
                                     TimeSpan.FromMinutes(0),
                                     TimeSpan.FromMinutes(1));

        }
        public async Task<bool> RemoveIfNotRemovedAsync()
        {
            //      await StartProvisionReminderAsync();
            return false;
        }
        public async Task<bool> IsInitializedAsync()
        {

            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
            return State.IsInitialized;
        }

        public async Task ReportQueueMessageCountAsync(string queueActorId, long messageCount, int additinalNodesAvaible)
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

            if (!State.QueeuActors.ContainsKey(queueActorId) || State.QueeuActors[queueActorId] != messageCount)
            {
                State.QueeuActors[queueActorId] = messageCount;
                await StateManager.SetStateAsync(StateKey, State);
            }

            var processorNode = await ClusterConfigStore.GetMessageClusterResourceAsync(this.Id.GetStringId()) as ClusterProcessorNode;

            //Handle Capacity Scaling of VMSS
            var messageSize = State.QueeuActors.Aggregate(0L, (p, c) => p + c.Value);
            var wantedCapacity = Math.Max(processorNode.Properties.MinCapacity,
                    Math.Min(processorNode.Properties.MaxCapacity, processorNode.Properties.MessagesPerInstance > 0 ?
                (messageSize / processorNode.Properties.MessagesPerInstance) + (messageSize > 0 ? 1 : 0) :
                1));

            wantedCapacity -= Math.Min(wantedCapacity, additinalNodesAvaible);

            var currentCapacity = State.Capacity;

            if ((wantedCapacity > currentCapacity && DateTimeOffset.UtcNow - XmlConvert.ToTimeSpan(processorNode.Properties.ScaleUpCooldown) > State.LastScaleAction) ||
                (wantedCapacity < currentCapacity && DateTimeOffset.UtcNow - XmlConvert.ToTimeSpan(processorNode.Properties.ScaleDownCooldown) > State.LastScaleAction))
            {
                await SetCapacityAsync((int)wantedCapacity);
                //   State.LastScaleAction = DateTimeOffset.UtcNow;
            }

        }
        public async Task<bool> SetCapacityAsync(int capacity)
        {

            var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

            if (!State.IsInitialized)
            {
                return false;
            }

            if (State.Capacity != capacity)
            {
                var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(Id.GetStringId()) as ClusterProcessorNode;

                var obj = await client.PatchAsync(State.VMSSResourceId, new JObject(
                    new JProperty("location", queue.Properties.Location),
                    new JProperty("sku", new JObject(
                        new JProperty("capacity", capacity),
                        new JProperty("name", queue.Properties.Name),
                        new JProperty("tier", queue.Properties.Tier)
                        ))
                    ), "2016-03-30");

                State.Capacity = obj.SelectToken("sku.capacity").ToObject<int>();
                State.LastScaleAction = DateTimeOffset.UtcNow;
                await StateManager.SetStateAsync(StateKey, State);

                await StartProvisionReminderAsync();
                return true;
            }

            return false;

        }
        public async Task<bool> CreateIfNotExistsAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

            if (State.IsInitialized)
                return true;
            if (State.IsProvisioning)
                return false;
            //var clusterKey = this.Id.GetStringId();
            //var config = this.GetConfigurationInfo();
            //var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(clusterKey) as ClusterQueueInfo;
            //var nodeName = queue.Name;

            //using (var armClient = new ResourceManagementClient(new TokenCredentials(await config.GetAccessToken())))
            //{
            //    armClient.SubscriptionId = config.SubscriptionId;

            //    var deployments = await armClient.Deployments.ListAsync(config.ResourceGroupName);
            //    var last = deployments.FirstOrDefault(f => f.Name.StartsWith($"vmss-{nodeName}-"));
            //    while(last==null && !string.IsNullOrEmpty( deployments.NextPageLink))
            //    {
            //       deployments = await armClient.Deployments.ListNextAsync(deployments.NextPageLink);
            //       last = deployments.FirstOrDefault(f => f.Name.StartsWith($"vmss-{nodeName}-"));
            //    }
            //    if(last.Properties.ProvisioningState == "Succeeded")
            //    {
            //        ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
            //        State.IsInitialized = true;
            //        await StateManager.SetStateAsync(StateKey, State);
            //        return true;
            //    }

            //}

            await StartProvisionReminderAsync();


            return false;
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
           
            if (reminderName.Equals(CheckProvision))
            {
                ServiceFabricEventSource.Current.ActorMessage(this, "Checking Provision");
                var clusterKey = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename;
                ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

                var processorNode = await ClusterConfigStore.GetMessageClusterResourceAsync(clusterKey) as ClusterProcessorNode;


                if (!State.IsInitialized)
                {


                    if (processorNode != null)
                    {

                        var nodeName = processorNode.Name;



                        var config = this.GetConfigurationInfo();

                        var armClinet = new ArmClient(await config.GetAccessToken());

                        var vmss = await armClinet.GetAsync<VMSS>($"/subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourceGroupName}/providers/Microsoft.Compute/virtualMachineScaleSets/vm{nodeName.ToLower()}", "2016-03-30");
                        var fabric = await armClinet.GetAsync<JObject>($"/subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourceGroupName}/providers/Microsoft.ServiceFabric/clusters/{config.ClusterName}", "2016-03-01");
                        var primvmss = await armClinet.GetAsync<JObject>($"/subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourceGroupName}/providers/Microsoft.Compute/virtualMachineScaleSets/{config.PrimaryScaleSetName}", "2016-03-30");

                        var parameters = new JObject(
                            ResourceManagerHelper.CreateValue("clusterName",
                                                                config.ClusterName),
                            ResourceManagerHelper.CreateValue("clusterLocation",
                                                                "West Europe"),
                            ResourceManagerHelper.CreateValue("nodeTypeName", nodeName),
                            ResourceManagerHelper.CreateValue("certificateThumbprint", fabric.SelectToken("properties.certificate.thumbprint").ToString()),
                            ResourceManagerHelper.CreateValue("adminPassword", "JgT5FFJK"),
                            ResourceManagerHelper.CreateValue("sourceVaultValue", primvmss.SelectToken("properties.virtualMachineProfile.osProfile.secrets[0].sourceVault.id").ToString()),
                            ResourceManagerHelper.CreateValue("certificateUrlValue", primvmss.SelectToken("properties.virtualMachineProfile.osProfile.secrets[0].vaultCertificates[0].certificateUrl").ToString())
                            );

                        var deployment = await ResourceManagerHelper.CreateTemplateDeploymentAsync(
                                                 new ApplicationCredentials
                                                 {
                                                     AccessToken = await config.GetAccessToken(),
                                                     SubscriptionId = config.SubscriptionId,
                                                 },
                                                   config.ResourceGroupName,
                                                 $"vmss-{nodeName}",//-{DateTimeOffset.UtcNow.ToString("s").Replace(":", "-")}",
                                                 new VmssArmTemplate(processorNode.Properties, vmss?.Sku?.capacity ?? 0),
                                                 parameters,
                                                 false,appendTimestamp:true
                                                 );
                        if (deployment.Properties.ProvisioningState == "Succeeded")
                        {

                            State.IsInitialized = true;
                            State.VMSSResourceId = (deployment.Properties.Outputs as JObject).SelectToken("vmssResourceId.value").ToString();
                            ServiceFabricEventSource.Current.ActorMessage(this, "Initialization Complated");
                        }
                    }


                }

                if (State.IsInitialized)
                {

                    ServiceFabricEventSource.Current.ActorMessage(this, "Validating node configuration with vmss");

                    var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());
                    var vmms = await client.GetAsync<JObject>(State.VMSSResourceId, "2016-03-30");
                    State.Capacity = vmms.SelectToken("sku.capacity").ToObject<int>();

                    var virtualMachines = await client.GetAsync<JObject>(State.VMSSResourceId + "/virtualMachines", "2016-03-30");
                    var deleting = virtualMachines.SelectTokens("$value[?(@.properties.provisioningState == 'Deleting')]");

                    var fabric = new FabricClient();

                    //Remove all nodes that are i teh deleting state of VMSS;
                    foreach (JObject delete in deleting)
                    {
                        var instanceId = delete.SelectToken("instanceId").ToString();
                        var name = delete.SelectToken("name").ToString();
                        var node = await fabric.QueryManager.GetNodeListAsync("_" + name);
                        if (node.Any())
                        {
                            ServiceFabricEventSource.Current.ActorMessage(this, $"Removing {node.First().NodeName} state due to vm being deleted");
                            await fabric.ClusterManager.RemoveNodeStateAsync(node.First().NodeName);
                        }


                    }

                    //Remove all nodes that are down and not in the virtualmachine list.

                    var nodes = await fabric.QueryManager.GetNodeListAsync();
                    foreach (var node in nodes.Where(n => n.NodeStatus == System.Fabric.Query.NodeStatus.Down
                                                      && n.NodeName.StartsWith($"_vm{NodeTypeName}")))
                    {

                        if (!virtualMachines.SelectTokens($"$value[?(@.instanceId == '{node.NodeName.Split('_').Last()}')]").Any())
                        {
                            ServiceFabricEventSource.Current.ActorMessage(this, $"Removing {node.NodeName} state due to vm not existing");
                            await fabric.ClusterManager.RemoveNodeStateAsync(node.NodeName);
                        }
                    }

                    if (vmms.SelectToken("properties.provisioningState").ToString() == "Succeeded")
                    {
                        State.IsProvisioning = false;
                        await UnregisterReminderAsync(GetReminder(reminderName));
                    }
                }

                await StateManager.SetStateAsync(StateKey, State);




            }
        }
        public string NodeTypeName { get { return this.Id.GetStringId().Substring(this.Id.GetStringId().LastIndexOf('/') + 1); } }

        //public async Task<bool> IsNodeRemovedAsync(string instanceNumber)
        //{

        //    var client = new ArmClient(await this.GetConfigurationInfo().GetAccessToken());
        //    var config = this.GetConfigurationInfo();
        //    var resourceId = $"/subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourceGroupName}/providers/Microsoft.Compute/virtualMachineScaleSets/vm{NodeTypeName}/virtualMachines/{instanceNumber}";
        //    var vm = await client.GetAsync<ArmErrorBase>(resourceId, "2016-03-30");

        //    return vm.Error!=null && vm.Error.Code == "NotFound";

        //}
    }

    public class VMSS : ArmErrorBase
    {
        public class VMSSSKU
        {

            [DataMember]
            public int capacity { get; set; }
        }
        [DataMember]
        public VMSSSKU Sku { get; set; }
    }
}
