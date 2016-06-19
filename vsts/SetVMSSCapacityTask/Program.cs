using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;

namespace SetVMSSCapacityTask
{
    [EntryPoint("Update VMSS Capacity")]
    public class ProgramOptions
    {


        [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [Option("VmssResourceId",HelpText ="The VMSS Resource Id to Patch capacity on")]
        public string VmssResourceId { get; set; }

        [Option("Capacity", HelpText ="The Capacity")]
        public int Capacity { get; set; }

    }
    class Program
    {

        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
#if DEBUG

            args = args.LoadFrom<ProgramOptions>(@"c:\dev\credsSinno.txt")
               .Concat(new[] {
                "--Capacity","5",
                "--VmssResourceId","/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/ci-sf-tests/providers/Microsoft.Compute/virtualMachineScaleSets/nt1vm",
                
           }).ToArray();

            args = new[] { "--build" };
#endif

            try
            {
                RunAsync(args, cancellationTokenSource.Token).Wait();
            }
            finally
            {
                runCompleteEvent.Set();
            }
        }

        private static async Task RunAsync(string[] args, CancellationToken token)
        {
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Parsing arguments", args);
            var client = new ArmClient(options.ConnectedServiceName.GetToken("https://management.azure.com/"));

            var resource = await client.GetAsync<JObject>(options.VmssResourceId, "2016-03-30");

            var obj = await client.PatchAsync(options.VmssResourceId, new JObject(
                   new JProperty("sku", new JObject(
                       new JProperty("capacity", options.Capacity),
                       new JProperty("name",resource.SelectToken("$.sku.name").ToString() ),
                       new JProperty("tier", resource.SelectToken("$.sku.tier").ToString())
                       ))
                   ), "2016-03-30");

            Console.WriteLine(obj.ToString());
        }
    }
}
