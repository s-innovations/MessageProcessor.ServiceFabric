using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.Pkcs;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
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
            return configurationPackage.GetClusterConfiguraiton();
        }
        
        static ClientCredential HandleSecureString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                var secureStringPassword = new SecureString();
                var nonsecureStringPassword = new StringBuilder();
                var chars = new char[1];
                var clientId = new StringBuilder();
                var clientIdDone = false;
                for (int i = 0; i < value.Length; i++)
                {
                    short unicodeChar = Marshal.ReadInt16(valuePtr, i * 2);
                    var c = Convert.ToChar(unicodeChar);


                    if (!clientIdDone)
                    {
                        if (c != ':')
                        {
                            clientId.Append(c);
                        }
                        else
                        {
                            clientIdDone = true;
                        }
                    }
                    else if (c != '\0')
                    {
                        secureStringPassword.AppendChar(c);
                        nonsecureStringPassword.Append(c);
                    }

                    // handle unicodeChar
                }
           //     return new UserPasswordCredential(clientId.ToString(), secureStringPassword);
                return new ClientCredential(clientId.ToString(), nonsecureStringPassword.ToString());

            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
     
        public static ServiceFabricClusterConfiguration GetClusterConfiguraiton(this ConfigurationPackage configurationPackage)
        {
            var section = configurationPackage.Settings.Sections["AppSettings"].Parameters;
            var a = section["AzureADServicePrincipal"].DecryptValue();
            var adClientCredential = HandleSecureString(a);

            var storageName = section["StorageName"]?.Value;
            if (storageName.StartsWith("/subscriptions"))
                storageName = storageName.Split('/').Last();

            return new ServiceFabricClusterConfiguration
            {
                ClusterName = section["ClusterName"].Value,
                ResourceGroupName = section["ResourceGroupName"].Value,
                SubscriptionId = section["SubscriptionId"].Value,
                AzureADServiceCredentials = adClientCredential,
                TenantId = section["TenantId"].Value,
                StorageName = storageName
            };
        }
    }
}
