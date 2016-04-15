using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Management
{
    public class PortRange
    {
        public int StartPort { get; set; }
        public int EndPort { get; set; }
    }
    public class ServiceFabricNode
    {
        public string Name { get; set; }

        public Dictionary<string, string> PlacementProperties { get; set; }
        public Dictionary<string,int> Capacities { get; set; }

        public int ClientConnectionEndpointPort { get; set; }
        public int HttpGatewayEndpointPort { get; set; }
        public PortRange ApplicationPorts { get; set; }
        public PortRange EphemeralPorts { get; set; }
        public bool IsPrimary { get; set; }

        public int? VMInstanceCount { get; set; }
        public string DurabilityLevel { get; set; }

        public ServiceFabricNode CopyPortsFrom(ServiceFabricNode prim)
        {
            ClientConnectionEndpointPort = prim.ClientConnectionEndpointPort;
            ApplicationPorts = prim.ApplicationPorts;
            EphemeralPorts = prim.EphemeralPorts;
            HttpGatewayEndpointPort = prim.HttpGatewayEndpointPort;
         

            return this;
        }
    }
    public class ExpectedVmResource
    {
        public string Name { get; set; }
        public string NodeTypeRef { get; set; }
        public int VmInstanceCount { get; set; }
        public bool IsVmss { get; set; }
    }
    public class ServiceFabricClusterCertificate
    {
        public string Thumbprint { get; set; }
        public string X509StoreName { get; set; }
    }
    public class ServiceFabricSettingParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public class ServiceFabricSetting
    {
        public string Name { get; set; }
        public ServiceFabricSettingParameter[] Parameters { get; set; }
    }
    public class DiagnosticsStorageAccountConfig
    {
        public string StorageAccountName { get; set; }
        public string ProtectedAccountKeyName { get; set; }
        public string BlobEndpoint { get; set; }
        public string QueueEndpoint { get; set; }
        public string TableEndpoint { get; set; }
    }
    public class ServiceFabricClusterProperties
    {
        
        public string ProvisioningState { get; set; }
     
        public string ClusterId { get; set; }
        public string ClusterCodeVersion { get; set; }
        public string ClusterState { get; set; }
        public string ManagementEndpoint { get; set; }
        public string ClusterEndpoint { get; set; }
        public ServiceFabricClusterCertificate Certificate { get; set; }
        public ServiceFabricSetting[] FabricSettings { get; set; }
        public DiagnosticsStorageAccountConfig DiagnosticsStorageAccountConfig { get; set; }
        public List<ServiceFabricNode> NodeTypes { get; set; }
       
   
        public ServiceFabricClusterProperties ToDTO()
        {
            return new ServiceFabricClusterProperties
            {
                Certificate = Certificate,
                ManagementEndpoint = ManagementEndpoint,
                NodeTypes = new List<ServiceFabricNode>( NodeTypes),
                FabricSettings = FabricSettings,
                DiagnosticsStorageAccountConfig = DiagnosticsStorageAccountConfig,

            };
        }
    }
    public class ServiceFabricCluster
    {
        public string Location { get; set; }
        public string Id { get; set; }
        public ServiceFabricClusterProperties Properties { get; set; }

        public Dictionary<string,string> Tags { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }

        public ServiceFabricCluster ToDTO()
        {
            return new ServiceFabricCluster
            {
                Location = Location,
                Id = Id,
                Name = Name,
                Tags = Tags,
                Type = Type,
                Properties = Properties.ToDTO()
            };
        }
    }
    public class ServiceFabricClient
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();

        public string ApiVersion { get; set; } = "2016-03-01";
        protected HttpClient Client { get; set; }
        public ServiceFabricClient(AuthenticationHeaderValue authorization)
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Authorization = authorization;


        }

        public Task<ServiceFabricCluster> GetServiceFabricClusterInfoAsync(Guid subscription, string resourceGroup, string clusterName)
        {
            var resourceUrl = $"https://management.azure.com/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceFabric/clusters/{clusterName}?api-version={ApiVersion}";

            return Client.GetAsync(resourceUrl)
                .As<ServiceFabricCluster>();
        }
        public async Task<ServiceFabricCluster> AddNodeAsync(Guid subscription, string resourceGroup, string clusterName, ServiceFabricNode node, bool copyPortsFromPrimary=true)
        {
            var resourceUrl = $"https://management.azure.com/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceFabric/clusters/{clusterName}?api-version={ApiVersion}";

            var req = await Client.GetAsync(resourceUrl)
                .As<ServiceFabricCluster>();

            
            req.Properties.NodeTypes.Add(node);
            if (copyPortsFromPrimary)
            {
                var prim = req.Properties.NodeTypes.Single(k => k.IsPrimary);
                node.EphemeralPorts = prim.EphemeralPorts;
                node.ApplicationPorts = prim.ApplicationPorts;
                node.ClientConnectionEndpointPort = prim.ClientConnectionEndpointPort;
                node.HttpGatewayEndpointPort = prim.HttpGatewayEndpointPort;                
            }

            Logger.Debug(() => JsonConvert.SerializeObject(req,new JsonSerializerSettings { NullValueHandling= NullValueHandling.Ignore,Formatting = Formatting.Indented, ContractResolver = new CamelCasePropertyNamesContractResolver() }));
            return await Client.PutAsync(resourceUrl, new StringContent(JsonConvert.SerializeObject(req, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() }),Encoding.UTF8,"application/json")).As<ServiceFabricCluster>();

        }

        public Task<ServiceFabricCluster> PutClusterInfoAsync(ServiceFabricCluster update)
        {
            var resourceUrl = $"https://management.azure.com/{update.Id}?api-version={ApiVersion}";

            return Client.PutAsync(resourceUrl, new StringContent(JsonConvert.SerializeObject(update, new JsonSerializerSettings
            {
                 NullValueHandling = NullValueHandling.Ignore,
                 ContractResolver =  new CamelCasePropertyNamesContractResolver(),
            }),Encoding.UTF8,"application/json"))
              .As<ServiceFabricCluster>();
        }




    }
}
