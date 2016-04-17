using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DeployServiceFabricApplicationTask
{
    class Program
    {
        static void Main(string[] args)
        {
            X509Credentials cert = new X509Credentials
            {
                FindType = X509FindType.FindByThumbprint,
                FindValue = "10A9BF925F41370FE55A4BDED2EF803505100C35",
                ProtectionLevel = ProtectionLevel.EncryptAndSign,
                StoreLocation = StoreLocation.CurrentUser,
                StoreName = "My",
            };
            cert.RemoteCertThumbprints.Add("10A9BF925F41370FE55A4BDED2EF803505100C35"); 
           
     
            var fabricClient = new FabricClient(cert,
                new FabricClientSettings
                {
                    ClientFriendlyName = "S-Innovations VSTS Deployment Client"
                }, "pksservicefabric11.westeurope.cloudapp.azure.com:19000");

            var a = fabricClient.QueryManager.GetApplicationListAsync().Result;
            var b = fabricClient.QueryManager.GetNodeListAsync().Result;
            //   fabricClient.ApplicationManager.CopyApplicationPackage("fabric:ImageStore", @"C:\dev\sinnovations\MessageProcessor.ServiceFabric\sfapps\MyDemoApp\pkg\Debug", "MyDemoAppTest");

            //   fabricClient.ApplicationManager.ProvisionApplicationAsync("MyDemoAppTest").Wait();

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
