using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace DeployServiceFabricApplicationTask
{
    class Program
    {
        private static TelemetryClient CreateTelemetryClientFromInstrumentationkey(string instrumentationKey = "")
        {
            var telemetryClient = new TelemetryClient();

            if (string.IsNullOrWhiteSpace(instrumentationKey) == false)
            {
                telemetryClient.InstrumentationKey = instrumentationKey;
            }

            telemetryClient.TrackTrace("Initializing from Service Fabric", SeverityLevel.Information);
            telemetryClient.Flush();
            return telemetryClient;
        }

        static void Main(string[] args)
        {


            var client = CreateTelemetryClientFromInstrumentationkey("d386c62e-7df6-42c2-9e56-d5e01f347782");
            client.TrackTrace("Cool", SeverityLevel.Error);
            client.TrackEvent("coolevent");




           
            X509Credentials cert = new X509Credentials
            {
                FindType = X509FindType.FindByThumbprint,
                FindValue = "584C645A30253DDA98EF8B7ED09B87F61468F3EE",
                ProtectionLevel = ProtectionLevel.EncryptAndSign,
                StoreLocation = StoreLocation.LocalMachine,
                StoreName = "My",
            };
            cert.RemoteCertThumbprints.Add("584C645A30253DDA98EF8B7ED09B87F61468F3EE"); 
           
     
            var fabricClient = new FabricClient(cert,
                new FabricClientSettings
                {
                    ClientFriendlyName = "S-Innovations VSTS Deployment Client"
                }, "pksservicefabric12.westeurope.cloudapp.azure.com:19000");

            var a = fabricClient.QueryManager.GetApplicationListAsync().Result;
            var b = fabricClient.QueryManager.GetNodeListAsync().Result;
           // new FabricClient().ClusterManager.RemoveNodeStateAsync("mynode");
            fabricClient.ApplicationManager.CopyApplicationPackage("fabric:ImageStore", @"C:\dev\sinnovations\MessageProcessor.ServiceFabric\sfapps\MyDemoApp\pkg\Debug", "xxx");

            fabricClient.ApplicationManager.ProvisionApplicationAsync("xxx").Wait();

            //fabricClient.ApplicationManager.CreateApplicationAsync(new ApplicationDescription
            //{
            //    ApplicationName = new Uri("fabric:/helloworld2"),
            //    ApplicationTypeName = "MyDemoAppType1",
            //    ApplicationTypeVersion = "1.0.0",
            //}).Wait();
 
           var applications =  fabricClient.QueryManager.GetApplicationListAsync().Result;
            var applicationsTypes = fabricClient.QueryManager.GetApplicationTypeListAsync("MyDemoAppType1").Result;

            var serviceTypes = fabricClient.QueryManager.GetServiceTypeListAsync("MyDemoAppType1", "1.0.0", "TestProcessorType").Result;
            //    fabricClient.ApplicationManager.ProvisionApplicationAsync
        }
    }
}
