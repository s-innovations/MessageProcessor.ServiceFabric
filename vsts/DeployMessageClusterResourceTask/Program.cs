using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace DeployMessageClusterResourceTask
{

    [EntryPoint("Deploying Message Cluster Resource")]
    public class ProgramOptions
    {
        [Display(Name = "Basic Auth", Description = "The pair of basic authentication username:password to use on messagecluster management api")]
        [Option("BasicAuth")]
        public string BasicAuth { get; set; }


        [Display(Name = "Cluster Definition", ResourceType = typeof(GlobPath), Description = "Path to the cluster.json definition file")]
        [Option("ClusterDefinition")]
        public string ClusterDefinition { get; set; }

        [Option("ResourceEndpoint")]
        public string Endpoint { get; set; }
    }

    class Program
    {
        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
#if DEBUG
            args = new[] { "--build" };

            args = args.LoadFrom<ProgramOptions>(@"c:\dev\creds.txt")
              .Concat(new[] {
                   "--ResourceGroupName" ,"ci-sf-tests",
                  "--ClusterName", "citestcluster",
                  "--BasicAuth", "pks:kodeal",
                  "--AzureADServicePrincipal", "********",
                  "--PlacementConstraints", "NodeTypeName==nt1vm",
                  "--StorageName", "w6tzgqkautoc1",
                  "--PackagePath", @"C:\dev\sinnovations\MessageProcessor.ServiceFabric\src\MessageProcessor.ServiceFabricHost\pkg\Debug",
                  "--Thumbprint", "61C26E136639BE85D873235EF24F398E23C9794A",
                  "--GatewayEndpoint", "citestcluster.westeurope.cloudapp.azure.com:19000",
                   "--ApplicatioName", "fabric:/MessageCluster"
              }).ToArray();
            //
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
            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Create or updating messagecluster resource", args);
            var encodedCreds = Convert.ToBase64String(Encoding.ASCII.GetBytes(options.BasicAuth));
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedCreds);
           var response= await httpClient.PutAsync(options.Endpoint, new StringContent(File.ReadAllText(options.ClusterDefinition), Encoding.UTF8, "application/json"));
            // httpClient.PostAsJsonAsync

            response.EnsureSuccessStatusCode();
        }
    }
}
