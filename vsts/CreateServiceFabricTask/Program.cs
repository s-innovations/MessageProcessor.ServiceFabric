using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Fabric;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Rest.Azure;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using SInnovations.Azure.ResourceManager;
using SInnovations.Azure.ResourceManager.TemplateActions;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace CreateServiceFabricTask
{

    [EntryPoint("Deploying Service Fabric")]
    public class ServiceFabricOptions : ArmTemplateOptions<ServiceFabricOptions>
    {
        public ServiceFabricOptions() : base(ServiceFabricConstants.ClusterTemplate, typeof(ServiceFabricConstants).Assembly)
        {

        }

        public override void OnTemplateLoaded()
        {
            this.Source.Add(new JsonPathSetter("variables.capacity", Capacity));
        }

        [Option("DefaultCapacity", HelpText = "The Default Vmss Capacity", DefaultValue = 5)]
        public int Capacity { get; set; }
    }

  
    class Program
    {
        static void Main(string[] args)
        {

#if DEBUG
           
            args = args.LoadFrom<ServiceFabricOptions>(@"c:\dev\credsSinno.txt")
                .LoadFrom<ResourceGroupOptions>(null,
                    o => o.ResourceGroup ?? "TestServiceFabric11",
                    o => o.CreateResourceGroup || true,
                    o => o.DeploymentName ?? "fabric",
                    o => o.ResourceGroupLocation ?? "West Europe")
                .Concat(new[] {
                  "--clusterLocation","West Europe",
                  "--clusterName", "pksservicefabric11",
                  "--adminPassword","JgT5FFJK",
                  "--certificateThumbprint","10A9BF925F41370FE55A4BDED2EF803505100C35",
                  "--sourceVaultValue", "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric11/providers/Microsoft.KeyVault/vaults/kv-qczknbuyveqr6qczknbu",
                  "--certificateUrlValue","https://kv-qczknbuyveqr6qczknbu.vault.azure.net/secrets/ServiceFabricCert/2d05b9c715fa4b26bc0874cf550b5993",
                  "--vmNodeTypeSize","Standard_A0"
            }).ToArray();
          //   args = new[] { "--build" };
#endif
            try
            {
                var options = ConsoleHelper.ParseAndHandleArguments<ServiceFabricOptions>("Create or updating servicefabric", args);

            }
            catch (CloudException ex)
            {
                Console.WriteLine(ex.Body.Message);
                WriteDetails(ex.Body.Details);
                throw;
            }
        }

        private static void WriteDetails(IList<CloudError> err)
        {
            foreach (var m in err)
            {
                Console.WriteLine(m.Message);
                WriteDetails(m.Details);
            }
        }
    }
}

