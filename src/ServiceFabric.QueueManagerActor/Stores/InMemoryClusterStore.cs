using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Stores
{
    public class InMemoryClusterStore : IMessageClusterConfigurationStore
    {
        private Dictionary<string, MessageClusterResource> _clusters = new Dictionary<string, MessageClusterResource>();

        public Task<bool> ClusterExistsAsync(string clusterKey)
        {
            return Task.FromResult(_clusters.ContainsKey(clusterKey));
        }

        public async Task<MessageClusterResource> GetMessageClusterAsync(string clusterKey)
        {
            var parts = clusterKey.Split('/');

            var stream = typeof(ServiceFabricConstants).Assembly.GetManifestResourceStream("SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.sampleConfiguration.json");
            var cluster = JsonConvert.DeserializeObject<MessageClusterResource>(await new StreamReader(stream).ReadToEndAsync(), new JsonSerializerSettings { });

            if (cluster.Name != parts.Last())
            {
                if (_clusters.ContainsKey(clusterKey))
                {
                    return _clusters[clusterKey];
                }
                return null;
            }

            return cluster;
        }

        public async Task<MessageClusterResourceBase> GetMessageClusterResourceAsync(string clusterKey)
        {
            var cluster = await GetMessageClusterAsync(clusterKey.Substring(0, clusterKey.LastIndexOf('/')));
            var name = clusterKey.Substring(clusterKey.LastIndexOf('/') + 1);
            return cluster.Resources.FirstOrDefault(n => n.Name == name);
        }

        public Task<MessageClusterResource> PutMessageClusterAsync(string clusterKey, MessageClusterResource model)
        {
            _clusters[clusterKey] = model;
            return Task.FromResult(_clusters[clusterKey]);
        }
    }
}
