using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration
{
   
    public static class ServiceInitializationParametersExtensions
    {
        public static ServiceFabricClusterConfiguration GetConfigurationInfo(this Actor actor)
        {
            return actor.ActorService.Context.GetClusterInfo();
        }
       
        public static ServiceFabricClusterConfiguration GetClusterInfo(this StatefulServiceContext parameters)
        {
            var configurationPackage = parameters.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = configurationPackage.Settings.Sections["AppSettings"].Parameters;

            var azureADServicePrincipal = section["AzureADServicePrincipal"].Value;
            var envelope = new EnvelopedCms();
            envelope.Decode(Convert.FromBase64String(azureADServicePrincipal));
            envelope.Decrypt();
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
