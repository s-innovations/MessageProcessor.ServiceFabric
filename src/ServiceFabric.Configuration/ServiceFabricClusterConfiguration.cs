using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration
{
    public class ServiceFabricClusterConfiguration
    {
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
        public string ClusterName { get; set; }
        public string AzureADServicePrincipalName { get; set; }
        public string TenantId { get;  set; }
        public string AzureADServicePrincipalKey { get;  set; }

        public async Task<string> GetAccessToken()
        {
  
            var ctx = new AuthenticationContext($"https://login.microsoftonline.com/{TenantId}");
            var cred = new ClientCredential(AzureADServicePrincipalName, AzureADServicePrincipalKey);
            var token = await ctx.AcquireTokenAsync("https://management.azure.com/", cred);

            return token.AccessToken;
        }
    }
}
