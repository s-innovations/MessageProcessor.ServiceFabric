using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using SInnovations.VSTeamServices.TasksBuilder.AzureResourceManager.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;

namespace DeployMessageClusterApplicationTask
{
    public class ProgramOptions
    {
        [Required]
        [Display(Name = "Service Principal", ShortName = "ConnectedServiceName", ResourceType = typeof(ServiceEndpoint), Description = "Azure Service Principal to obtain tokens from")]
        public ServiceEndpoint ConnectedServiceName { get; set; }

        [Display(Name = "ResourceGroup Name",Description ="The resource group for which to deploy child resources for the message cluter")]
        [Option("ResourceGroupName")]
        public string ResourceGroupName { get; set; }

        [Display(Name = "Cluster Name", Description = "The Service Fabric cluster name")]
        [Option("ClusterName")]
        public string ClusterName { get; set; }

        [Display(Name = "Basic Auth", Description = "The pair of basic authentication username:password to use on messagecluster management api")]
        [Option("BasicAuth")]
        public string BasicAuth { get; set; }

        [Display(Name = "Service Principal", Description = "The clientid:key of a azure service principal to use when deploying child resources")]
        [Option("AzureADServicePrincipal")]
        public string AzureADServicePrincipal { get; set; }

        [Display(Name = "Placement Constraints", Description = "PlacementConstraints for the message cluster application")]
        [Option("PlacementConstraints", DefaultValue = "NodeTypeName==nt1vm")]
        public string PlacementConstraints { get; set; }

        [Display(Name = "Storage Account", Description = "The name of a storage account in the resource group to use for persistant data")]
        [Option("StorageName")]
        public string StorageName { get; set; }


        [Display(Name ="Package Path", ResourceType =typeof(GlobPath), Description ="The folder where the application is stored")]
        [Option("PackagePath")]
        public string PackagePath { get; set; }


        [Option("Thumbprint", HelpText = "Thumbprint  of certificate to use for connection")]
        public string Thumbprint { get; set; }


        [Option("GatewayEndpoint", HelpText = "Gateway Endpoint for the cluster")]
        public string Gateway { get; set; }

        [Option("ApplicatioName", HelpText = "Deploy the application type as fabric/:name ", DefaultValue = "fabric:/MessageClusterApp")]
        public string ApplicatioName { get; set; }
        public string ImagePath { get; internal set; }
        public string ApplicationTypeName { get; internal set; } = "MessageProcessor.ServiceFabricHostType";
    }
    class Program
    {
        private static readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {

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

            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Create or updating messagecluster", args);


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

            var applicationType = await ProvisionApplicationTypeAsync(options, fabricClient);

            var applicationName = new Uri(options.ApplicatioName.StartsWith("fabric:/") ? options.ApplicatioName : $"fabric:/{options.ApplicatioName}");

            

            var application = new ApplicationDescription
            {
                ApplicationName = applicationName,
                ApplicationTypeName = applicationType.ApplicationTypeName,
                ApplicationTypeVersion = applicationType.ApplicationTypeVersion
            };

            application.ApplicationParameters.Add("SubscriptionId", options.ConnectedServiceName.SubscriptionId);
            application.ApplicationParameters.Add("ResourceGroupName", options.ResourceGroupName);
            application.ApplicationParameters.Add("ClusterName", options.ClusterName);
            application.ApplicationParameters.Add("TenantId", options.ConnectedServiceName.TenantId);
            application.ApplicationParameters.Add("StorageName", options.StorageName);
            application.ApplicationParameters.Add("BasicAuth", options.BasicAuth);
            application.ApplicationParameters.Add("AzureADServicePrincipal", options.AzureADServicePrincipal);
            application.ApplicationParameters.Add("PlacementConstraints", options.PlacementConstraints);


            var applications = await fabricClient.QueryManager.GetApplicationListAsync(application.ApplicationName);
            if (!applications.Any(a => a.ApplicationTypeName == application.ApplicationTypeName && a.ApplicationTypeVersion == application.ApplicationTypeVersion))
            {
                await fabricClient.ApplicationManager.CreateApplicationAsync(application);
            }


        }
        private static async Task<ApplicationType> ProvisionApplicationTypeAsync(ProgramOptions options, FabricClient fabricClient)
        {
            var imagePath = options.ImagePath ?? options.ApplicationTypeName;
            var appType = fabricClient.QueryManager.GetApplicationTypeListAsync(options.ApplicationTypeName).Result;

            if (!appType.Any())
            {
                fabricClient.ApplicationManager.CopyApplicationPackage("fabric:ImageStore", options.PackagePath, imagePath);

                try
                {
                    await fabricClient.ApplicationManager.ProvisionApplicationAsync(imagePath);

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
            if (!appType.Any())
            {
                throw new Exception("The application type name and version is not found in the package or the cluster");

            }

            return appType.OrderByDescending(a => a.ApplicationTypeVersion).FirstOrDefault();



        }


    }
}
