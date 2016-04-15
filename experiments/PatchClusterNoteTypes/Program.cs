using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Serilog;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;

namespace PatchClusterNoteTypes
{
    class Program
    {
        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                  .MinimumLevel.Verbose()
                  .WriteTo.LiterateConsole(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({Name:l}){NewLine} {Message}{NewLine}{Exception}")
                  .CreateLogger();

            try
            {
                RunAsync(cancellationTokenSource.Token).Wait();
            }
            finally
            {
                runCompleteEvent.Set();
            }


        }

        private static async Task RunAsync(CancellationToken token)
        {
            var ac = new AuthenticationContext("https://login.windows.net/common");
            var AuthenticationInfo = ac.AcquireToken(
                                     resource: "https://management.azure.com/",
                                     clientId: "1950a258-227b-4e31-a9cf-717495945fc2",
                                     redirectUri: new Uri("urn:ietf:wg:oauth:2.0:oob"), promptBehavior: PromptBehavior.RefreshSession);

            Console.WriteLine(AuthenticationInfo.AccessToken);
            Console.WriteLine(AuthenticationInfo.UserInfo.DisplayableId);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", AuthenticationInfo.AccessToken);
            httpClient.BaseAddress = new Uri("https://management.azure.com/");
            var get = await httpClient.GetAsync("/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourcegroups/TestServiceFabric11/providers/Microsoft.ServiceFabric/clusters/pksservicefabric11?api-version=2016-03-01");
            Console.WriteLine(await get.Content.ReadAsStringAsync());

            //  var patch = new HttpRequestMessage(new HttpMethod("PATCH"), "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourcegroups/TestServiceFabric11/providers/Microsoft.ServiceFabric/clusters/pksservicefabric11?api-version=2016-03-01");

            var client = new ServiceFabricClient(new AuthenticationHeaderValue("bearer", AuthenticationInfo.AccessToken));
            var cluster = await client.GetServiceFabricClusterInfoAsync(Guid.Parse("8393a037-5d39-462d-a583-09915b4493df"), "TestServiceFabric11", "pksservicefabric11");
            var update = cluster.ToDTO();
            var prim = update.Properties.NodeTypes.Single(n => n.IsPrimary);
            var noteType = new ServiceFabricNode()
            {
                Name = "myTstNt2",
                VMInstanceCount = 1,
                DurabilityLevel = "Bronze",
            }.CopyPortsFrom(prim);





            await client.AddNodeAsync(Guid.Parse("8393a037-5d39-462d-a583-09915b4493df"), "TestServiceFabric11", "pksservicefabric11",
              noteType);


        }


    }
}
