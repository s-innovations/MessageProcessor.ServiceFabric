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
                var chars = new char[1];
                var clientId = new List<char>();
                var clientIdDone = false;
                for (int i = 0; i < value.Length; i++)
                {
                    short unicodeChar = Marshal.ReadInt16(valuePtr, i * 2);
                    var c = Convert.ToChar(unicodeChar);


                    if (!clientIdDone)
                    {
                        if (c != ':')
                        {
                            clientId.Add(c);
                        }
                        else
                        {
                            clientIdDone = true;
                        }
                    }
                    else if (c != '\0')
                        secureStringPassword.AppendChar(c);

                    // handle unicodeChar
                }

                return new ClientCredential(new string(clientId.ToArray()), secureStringPassword);

            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
     
        public static ServiceFabricClusterConfiguration GetClusterConfiguraiton(this ConfigurationPackage configurationPackage)
        {
            var section = configurationPackage.Settings.Sections["AppSettings"].Parameters;
        //    var azureADServicePrincipal = section["AzureADServicePrincipal"].Value;
            var a = section["AzureADServicePrincipal"].DecryptValue();
            //var b = SecureStringToString(a);
            //var c = Encoding.Unicode.GetBytes(b);
            //var d = Encoding.UTF8.GetString(c);
            var adClientCredential = HandleSecureString(a);


            //if (string.IsNullOrEmpty(azureADServicePrincipal))
            //{

            //    Logger.Error("The Azure AD Service Principal Credentials was not set");
            //    throw new KeyNotFoundException("AzureADServicePrincipal");
            //}

            //var envelope = new EnvelopedCms();
            //envelope.Decode(Convert.FromBase64String(azureADServicePrincipal));
            //try
            //{
            //    envelope.Decrypt();
            //}
            //catch (Exception ex)
            //{
            //    Logger.ErrorException("Failed to decrypt service principal key", ex);
            //    throw new Exception("Failed to decrypt service principal key");
            //}

            //var AADCredentials = Encoding.Unicode.GetString(envelope.ContentInfo.Content).Split(':');

            return new ServiceFabricClusterConfiguration
            {
                ClusterName = section["ClusterName"].Value,
                ResourceGroupName = section["ResourceGroupName"].Value,
                SubscriptionId = section["SubscriptionId"].Value,
             //   AzureADServicePrincipalName = AADCredentials[0],
            //    AzureADServicePrincipalKey = AADCredentials[1],
                AzureADServiceCredentials = adClientCredential,
                TenantId = section["TenantId"].Value,
                StorageName = section["StorageName"]?.Value
            };
        }
    }
}
