using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Stores
{
    public class BlobStorageClusterStore : IMessageClusterConfigurationStore
    {
        private readonly CloudBlobContainer container;
        private readonly string prefix;
        public BlobStorageClusterStore(CloudBlobContainer container, string prefix = "")
        {
            this.container = container;
            this.prefix = prefix;
        }
        public Task<bool> ClusterExistsAsync(string clusterKey)
        {
            return container.GetBlockBlobReference(clusterKey).ExistsAsync();
        }

        public async Task<MessageClusterResource> GetMessageClusterAsync(string clusterKey)
        {
            using (var blobStream = await container.GetBlockBlobReference(clusterKey).OpenReadAsync())
            {
                var cluster = JsonConvert.DeserializeObject<MessageClusterResource>(await new StreamReader(blobStream).ReadToEndAsync(), new JsonSerializerSettings { });
                return cluster;
            }
        }

        public async Task<MessageClusterResourceBase> GetMessageClusterResourceAsync(string clusterKey)
        {
            var cluster = await GetMessageClusterAsync(clusterKey.Substring(0, clusterKey.LastIndexOf('/')));
            var name = clusterKey.Substring(clusterKey.LastIndexOf('/') + 1);
            return cluster.Resources.FirstOrDefault(n => n.Name == name);
        }

        public async Task<MessageClusterResource> PutMessageClusterAsync(string clusterKey, MessageClusterResource model)
        {
            var blob = container.GetBlockBlobReference(clusterKey);
            blob.Properties.ContentType = "application/json";
           
            var cluster = JsonConvert.SerializeObject(model,new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver()});
            await blob.UploadTextAsync(cluster);
            return model;
        }
    }
}
