using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Resources;
using Microsoft.Rest;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using SInnovations.Azure.ResourceManager;
using SInnovations.Azure.ResourceManager.TemplateActions;

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
            public string VMSSResourceId { get; internal set; }
        }

        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }

        public VmssManagerActor(IMessageClusterConfigurationStore clusterProvider)
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
        public async Task<int> GetCapacityAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
            return State.Capacity;
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
                var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(Id.GetStringId()) as ClusterQueueInfo;

                var obj = await client.PatchAsync(State.VMSSResourceId, new JObject(
                    new JProperty("location", queue.Properties.Vmss.Location),
                    new JProperty("sku", new JObject(
                        new JProperty("capacity", capacity),
                        new JProperty("name", queue.Properties.Vmss.Name),
                        new JProperty("tier", queue.Properties.Vmss.Tier)
                        ))
                    ), "2016-03-30");

                State.Capacity = obj.SelectToken("sku.capacity").ToObject<int>();

                await StateManager.SetStateAsync(StateKey, State);

                await StartProvisionReminderAsync(false);
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
                var clusterKey = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename;
                ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);


                if (!State.IsInitialized)
                {

                    var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(clusterKey) as ClusterQueueInfo;

                    if (queue != null)
                    {

                        var nodeName = queue.Name;



                        var config = this.GetConfigurationInfo();

                        var armClinet = new ArmClient(await config.GetAccessToken());
                        var vmss = await armClinet.GetAsync<VMSS>($"/subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourceGroupName}/providers/Microsoft.Compute/virtualMachineScaleSets/vm{nodeName.ToLower()}", "2016-03-30");

                        var parameters = new JObject(
                            ResourceManagerHelper.CreateValue("clusterName",
                                                                config.ClusterName),
                            ResourceManagerHelper.CreateValue("clusterLocation",
                                                                "West Europe"),
                            ResourceManagerHelper.CreateValue("nodeTypeName", nodeName),
                            ResourceManagerHelper.CreateValue("certificateThumbprint",
                                                                "10A9BF925F41370FE55A4BDED2EF803505100C35"),
                            ResourceManagerHelper.CreateValue("adminPassword", "JgT5FFJK"),
                            ResourceManagerHelper.CreateValue("sourceVaultValue",
                                                "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric11/providers/Microsoft.KeyVault/vaults/kv-qczknbuyveqr6qczknbu"),
                            ResourceManagerHelper.CreateValue("certificateUrlValue", "https://kv-qczknbuyveqr6qczknbu.vault.azure.net/secrets/ServiceFabricCert/2d05b9c715fa4b26bc0874cf550b5993")
                            );

                        var deployment = await ResourceManagerHelper.CreateTemplateDeploymentAsync(
                                                 new ApplicationCredentials
                                                 {
                                                     AccessToken = await config.GetAccessToken(),
                                                     SubscriptionId = config.SubscriptionId,
                                                 },
                                                   config.ResourceGroupName,
                                                 $"vmss-{nodeName}-{DateTimeOffset.UtcNow.ToString("s").Replace(":", "-")}",
                                                 new VmssArmTemplate(queue.Properties.Vmss, vmss?.Sku?.capacity ?? 0),
                                                 parameters,
                                                 false
                                                 );
                        if (deployment.Properties.ProvisioningState == "Succeeded")
                        {

                            State.IsInitialized = true;
                            State.VMSSResourceId = (deployment.Properties.Outputs as JObject).SelectToken("vmssResourceId.value").ToString();

                        }
                    }


                }

                if (State.IsInitialized)
                {
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
