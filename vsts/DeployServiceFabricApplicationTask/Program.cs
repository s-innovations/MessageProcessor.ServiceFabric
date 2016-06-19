using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Rest.Azure;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace DeployServiceFabricApplicationTask
{

    [EntryPoint("Deploying Service Fabric Application")]
    [Group(DisplayName = "Application Type", isExpanded =true, Name = "provision")]
    public class ProgramOptions
    {
        [Option("Thumbprint", HelpText = "Thumbprint  of certificate to use for connection")]
        public string Thumbprint { get; set; }

        [Option("GatewayEndpoint", HelpText = "Gateway Endpoint for the cluster")]
        public string Gateway { get; set; }

        [Display(GroupName = "provision")]
        [Option("ImagePath", HelpText = "Cluster Image Path to provision the application to")]
        public string ImagePath { get; set; }

        [Display(GroupName = "provision")]
        [Option("ApplicationTypeName", HelpText = "Application type ame to provision")]
        public string ApplicationTypeName { get; set; }

        [Display(GroupName = "provision")]
        [Option("ApplicationVersion", HelpText = "Application version to provision")]
        public string ApplicationTypeVersion { get; set; }


        [Option("ApplicatioName", HelpText = "Deploy the application type as fabric/:name ")]
        public string ApplicatioName { get; set; }

        [Option("ServiceTypeName", HelpText = "Deploy a service to application fabric/:name ")]
        public string ServiceTypeName { get; set; }

        [Option("ServiceName", HelpText = "Deploy a service as fabric:/applicationName/serviceName ")]
        public string ServiceName { get; set; }

        [Required]
        [Display(Description = "The application to deploy or provision", Name = "Application Package", ShortName = "AppPath", ResourceType = typeof(GlobPath))]
        public string ApplicationPath { get; set; }

    }
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
        private static void WriteDetails(IList<CloudError> err)
        {
            foreach (var m in err)
            {
                Console.WriteLine(m.Message);
                WriteDetails(m.Details);
            }
        }

        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {// "FD738EDFE63C8397FFBF3797D7DB722074C028A2"

#if DEBUG

            args = new[] { "--Thumbprint", "FD738EDFE63C8397FFBF3797D7DB722074C028A2",
                "--GatewayEndpoint", "axyzsfstagweu.westeurope.cloudapp.azure.com:19000",
                "--AppPath", @"C:\dev\sinnovations\MessageProcessor.ServiceFabric\sfapps\MyDemoApp\pkg\Debug",
                "--ApplicationTypeName","MyDemoAppType1",
                "--ApplicationVersion","1.0.0",
                "--ApplicatioName","helloworld",
                "--ServiceTypeName","TestProcessorType",
                "--ServiceName", "blabla1"
            };
#endif
            var client = CreateTelemetryClientFromInstrumentationkey("d386c62e-7df6-42c2-9e56-d5e01f347782");
            client.TrackEvent("Service Fabric");

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
            try
            {
                var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Create or updating servicefabric", args);





                X509Credentials cert = new X509Credentials
                {
                    FindType = X509FindType.FindByThumbprint,
                    FindValue = options.Thumbprint,
                    ProtectionLevel = ProtectionLevel.EncryptAndSign,
                    StoreLocation = StoreLocation.CurrentUser,
                    StoreName = "My",
                };
                cert.RemoteCertThumbprints.Add(options.Thumbprint);


                var fabricClient = new FabricClient(cert,
                    new FabricClientSettings
                    {
                        ClientFriendlyName = "S-Innovations VSTS Deployment Client"
                    }, options.Gateway);

                var applicationType = ProvisionApplicationType(options, fabricClient);

                var applicationName = new Uri(options.ApplicatioName.StartsWith("fabric:/") ? options.ApplicatioName : $"fabric:/{options.ApplicatioName}");
                var application = new ApplicationDescription
                {
                    ApplicationName = applicationName,
                    ApplicationTypeName = applicationType.ApplicationTypeName,
                    ApplicationTypeVersion = applicationType.ApplicationTypeVersion
                };

                var applications = await fabricClient.QueryManager.GetApplicationListAsync(application.ApplicationName);
                if (!applications.Any(a=>a.ApplicationTypeName == application.ApplicationTypeName && a.ApplicationTypeVersion == application.ApplicationTypeVersion))
                {
                    await fabricClient.ApplicationManager.CreateApplicationAsync(application);
                }





                //   var applicationsTypes1 = fabricClient.QueryManager.GetApplicationTypeListAsync(options.ApplicationTypeName).Result;

                //    var applications = fabricClient.QueryManager.GetApplicationListAsync().Result;


                var serviceTypes = await fabricClient.QueryManager.GetServiceTypeListAsync(application.ApplicationTypeName, application.ApplicationTypeVersion, options.ServiceTypeName);
                if (serviceTypes.Any())
                {
                   
                    var serviceName = new Uri($"{application.ApplicationName.AbsoluteUri}/{options.ServiceName}");
                    var services = await fabricClient.QueryManager.GetServiceListAsync(application.ApplicationName, serviceName);
                    if (!services.Any())
                    {
                        await fabricClient.ServiceManager.CreateServiceFromTemplateAsync(application.ApplicationName, serviceName, serviceTypes.First().ServiceTypeDescription.ServiceTypeName, new byte[0]);
                    }
                }
                
                //    fabricClient.ApplicationManager.ProvisionApplicationAsync

            }
            catch (CloudException ex)
            {
                Console.WriteLine(ex.Body.Message);
                WriteDetails(ex.Body.Details);
                throw;
            }

        }

        private static ApplicationType ProvisionApplicationType(ProgramOptions options, FabricClient fabricClient)
        {
            var imagePath = options.ImagePath ?? options.ApplicationTypeName;
            var appType = fabricClient.QueryManager.GetApplicationTypeListAsync(options.ApplicationTypeName).Result;

            if (!appType.Any(a => a.ApplicationTypeName == options.ApplicationTypeName && a.ApplicationTypeVersion == options.ApplicationTypeVersion))
            {
                fabricClient.ApplicationManager.CopyApplicationPackage("fabric:ImageStore", options.ApplicationPath, imagePath);

                try
                {
                    fabricClient.ApplicationManager.ProvisionApplicationAsync(imagePath).Wait();

                }
                catch (AggregateException ae)
                {
                    ae.Handle((x) =>
                    {
                        if (x is FabricElementAlreadyExistsException) // This we know how to handle.
                        {

                            Console.WriteLine("Fabric already had the package provisioned");
                            return true;
                        }
                        return false; // Let anything else stop the application.
                    });
                }
                
            }
            appType = fabricClient.QueryManager.GetApplicationTypeListAsync(options.ApplicationTypeName).Result;
            if (!appType.Any(a => a.ApplicationTypeName == options.ApplicationTypeName && a.ApplicationTypeVersion == options.ApplicationTypeVersion))
            {
                throw new Exception("The application type name and version is not found in the package or the cluster");

            }

            return appType.Single(a => a.ApplicationTypeName == options.ApplicationTypeName && a.ApplicationTypeVersion == options.ApplicationTypeVersion);



        }
    }
}
