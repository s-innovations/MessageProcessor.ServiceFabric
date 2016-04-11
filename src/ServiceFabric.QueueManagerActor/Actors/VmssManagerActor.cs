using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
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
            return base.OnActivateAsync();
        }
        protected override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        private Task StartProvisionReminder()
        {
            Task<IActorReminder> reg2 = RegisterReminderAsync(
                                      CheckProvision,
                                      new Byte[0],
                                      TimeSpan.FromMinutes(0),
                                      TimeSpan.FromMinutes(1));
            return reg2;
        }

        public async Task CreateIfNotExistsAsync()
        {
            await StartProvisionReminder();
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals(CheckProvision))
            {
                var clusterKey = this.Id.GetStringId();//Subscription/ResourceGroup/clustername/nodename;

                var queue = await ClusterConfigStore.GetMessageClusterResourceAsync(clusterKey) as ClusterQueueInfo;


                var nodeName = queue.Name;


                var config = this.GetConfigurationInfo();

                var parameters = new JObject(
                    ResourceManagerHelper.CreateValue("clusterName", 
                                                        config.ClusterName),
                    ResourceManagerHelper.CreateValue("clusterLocation", 
                                                        "West Europe"),
                    ResourceManagerHelper.CreateValue("nodeTypeName", nodeName),
                    ResourceManagerHelper.CreateValue("certificateThumbprint", 
                                                        "4B729ADE19BF2742BB09BB257C6BD8538DBDB1A4"),
                    ResourceManagerHelper.CreateValue("adminPassword", "JgT5FFJK"),
                    ResourceManagerHelper.CreateValue("sourceVaultValue", 
                                        "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/ServiceFabricTest/providers/Microsoft.KeyVault/vaults/PksTestSFVault"),
                    ResourceManagerHelper.CreateValue("certificateUrlValue", "https://pkstestsfvault.vault.azure.net:443/secrets/ServiceFabricCert/5a4356ee60064c7fa7e2581e6e3527dc")
                    );
                await ResourceManagerHelper.CreateTemplateDeploymentAsync(
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

                await UnregisterReminderAsync(GetReminder(reminderName));
            }
        }
    }
}
