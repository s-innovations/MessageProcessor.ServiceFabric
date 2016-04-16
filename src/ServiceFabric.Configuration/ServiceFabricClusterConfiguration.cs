using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration
{
    class FileCache : TokenCache
    {
        public string CacheFilePath;
        private static readonly object FileLock = new object();

        // Initializes the cache against a local file.
        // If the file is already present, it loads its content in the ADAL cache
        public FileCache(string filePath = @".\TokenCache.dat")
        {
            CacheFilePath = filePath;
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            lock (FileLock)
            {
                this.Deserialize(File.Exists(CacheFilePath) ?
                    ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath),
                                            null,
                                            DataProtectionScope.CurrentUser)
                    : null);
            }
        }

        // Empties the persistent store.
        public override void Clear()
        {
            base.Clear();
            File.Delete(CacheFilePath);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                this.Deserialize(File.Exists(CacheFilePath) ?
                    ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath),
                                            null,
                                            DataProtectionScope.CurrentUser)
                    : null);
            }
        }

        // Triggered right after ADAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (this.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(CacheFilePath,
                        ProtectedData.Protect(this.Serialize(),
                                                null,
                                                DataProtectionScope.CurrentUser));
                    // once the write operation took place, restore the HasStateChanged bit to false
                    this.HasStateChanged = false;
                }
            }
        }
    }
    public class ServiceFabricClusterConfiguration
    {
        public string ResourceGroupName { get; set; }
        public string SubscriptionId { get; set; }
        public string ClusterName { get; set; }
        public string AzureADServicePrincipalName { get; set; }
        public string TenantId { get;  set; }
        public string AzureADServicePrincipalKey { get;  set; }


        private static FileCache _cache = new FileCache();
        public async Task<string> GetAccessToken()
        {
  
            var ctx = new AuthenticationContext($"https://login.microsoftonline.com/{TenantId}",_cache);
            var cred = new ClientCredential(AzureADServicePrincipalName, AzureADServicePrincipalKey);
            var token = await ctx.AcquireTokenAsync("https://management.azure.com/", cred);

            return token.AccessToken;
        }
    }
}
