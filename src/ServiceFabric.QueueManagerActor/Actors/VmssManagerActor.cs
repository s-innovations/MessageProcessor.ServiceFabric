using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
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

        public VmssArmTemplate(ClusterVmssInfo properties) : this()
        {

            Add(new JsonPathSetter("variables.vmImagePublisher", properties.VmImagePublisher ));
            Add(new JsonPathSetter("variables.vmImageOffer", properties.VmImageOffer));
            Add(new JsonPathSetter("variables.vmImageSku", properties.VmImageSku));
            Add(new JsonPathSetter("variables.vmImageVersion", properties.VmImageVersion));
            Add(new JsonPathSetter("variables.vmNodeTypeSize", properties.Name));
            Add(new JsonPathSetter("variables.vmNodeTypeTire", properties.Tire));

            
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
            public bool IsInitialized { get; set; }

            [DataMember]
            public bool IsStarted { get; set; }

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

        private async Task StartProvisionReminderAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
            await RegisterReminderAsync(
                                      CheckProvision,
                                      new Byte[0],
                                      TimeSpan.FromMinutes(0),
                                      TimeSpan.FromMinutes(1));
            State.IsStarted = true;
            await StateManager.SetStateAsync(StateKey, State);

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

        public async Task<bool> CreateIfNotExistsAsync()
        {
            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);

            if (State.IsInitialized)
                return true;
            if (State.IsStarted)
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

                var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(clusterKey) as ClusterQueueInfo;
                try
                {
                    if (queue != null)
                    {

                        var nodeName = queue.Name;


                        var config = this.GetConfigurationInfo();

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
                                                 new VmssArmTemplate(queue.Properties.Vmss),
                                                 parameters,
                                                 false
                                                 );
                        if (deployment.Properties.ProvisioningState == "Succeeded")
                        {
                            ActorState State = await StateManager.GetStateAsync<ActorState>(StateKey);
                            State.IsInitialized = true;
                            await StateManager.SetStateAsync(StateKey, State);
                        }
                    }
                    else
                    {

                    }
                }
                finally
                {
                    await UnregisterReminderAsync(GetReminder(reminderName));
                }
            }
        }
    }
}
