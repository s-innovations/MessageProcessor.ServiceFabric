using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration
{
   
    public static class ServiceInitializationParametersExtensions
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();
         
        public static ServiceFabricClusterConfiguration GetConfigurationInfo(this Actor actor)
        {
            return actor.ActorService.Context.GetClusterInfo();
        }
       
        public static ServiceFabricClusterConfiguration GetClusterInfo(this StatefulServiceContext parameters)
        {
            Logger.Debug("Getting Cluster Information");

            var configurationPackage = parameters.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = configurationPackage.Settings.Sections["AppSettings"].Parameters;
            var azureADServicePrincipal = section["AzureADServicePrincipal"].Value;

            if(string.IsNullOrEmpty(azureADServicePrincipal))
            {

                Logger.Error("The Azure AD Service Principal Credentials was not set");
                throw new KeyNotFoundException("AzureADServicePrincipal");
            }

            var envelope = new EnvelopedCms();
            envelope.Decode(Convert.FromBase64String(azureADServicePrincipal));
            try
            {
                envelope.Decrypt();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed to decrypt service principal key", ex);
                throw new Exception("Failed to decrypt service principal key");
            }

            var AADCredentials = Encoding.UTF8.GetString(envelope.ContentInfo.Content).Split(':');

            return new ServiceFabricClusterConfiguration
            {
                ClusterName = section["ClusterName"].Value,
                ResourceGroupName = section["ResourceGroupName"].Value,
                SubscriptionId = section["SubscriptionId"].Value,
                AzureADServicePrincipalName = AADCredentials[0],
                AzureADServicePrincipalKey = AADCredentials[1],
                TenantId = section["TenantId"].Value
            };
        }
    }
}
