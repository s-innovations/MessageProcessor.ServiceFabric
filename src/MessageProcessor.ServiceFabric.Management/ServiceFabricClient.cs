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
        public int ClientConnectionEndpointPort { get; set; }
        public int HttpGatewayEndpointPort { get; set; }
        public PortRange ApplicationPorts { get; set; }
        public PortRange EphemeralPorts { get; set; }
        public bool IsPrimary { get; set; }
        public Dictionary<string,string> PlacementProperties { get; set; }

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
    public class ServiceFabricClusterProperties
    {
        
        public string ProvisioningState { get; set; }
     
        public string ClusterId { get; set; }
        public string ClusterCodeVersion { get; set; }
        public string ClusterState { get; set; }
        public string ManagementEndpoint { get; set; }
        public string ClusterEndpoint { get; set; }
        public ServiceFabricClusterCertificate Certificate { get; set; }
        public List<ServiceFabricNode> NodeTypes { get; set; }
        public List<ExpectedVmResource> ExpectedVmResources { get; set; }

        public ServiceFabricClusterProperties ToDTO()
        {
            return new ServiceFabricClusterProperties
            {
                Certificate = Certificate,
                ManagementEndpoint = ManagementEndpoint,
                ExpectedVmResources = ExpectedVmResources.Where(n => n.Name== NodeTypes.Single(k => k.IsPrimary).Name).ToList(),
                NodeTypes = NodeTypes,
            };
        }
    }
    public class ServiceFabricCluster
    {
        public string Location { get; set; }
        public string Id { get; set; }
        public ServiceFabricClusterProperties Properties { get; set; }

        public ServiceFabricCluster ToDTO()
        {
            return new ServiceFabricCluster
            {
                Location = Location,
                Id = Id,
                Properties = Properties.ToDTO()
            };
        }
    }
    public class ServiceFabricClient
    {
        protected HttpClient Client { get; set; }
        public ServiceFabricClient(AuthenticationHeaderValue authorization)
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Authorization = authorization;


        }

        public Task<ServiceFabricCluster> GetServiceFabricClusterInfoAsync(Guid subscription, string resourceGroup, string clusterName)
        {
            var resourceUrl = $"https://management.azure.com/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceFabric/clusters/{clusterName}?api-version=2015-01-01-alpha";

            return Client.GetAsync(resourceUrl)
                .As<ServiceFabricCluster>();
        }
        public async Task<ServiceFabricCluster> AddNodeAsync(Guid subscription, string resourceGroup, string clusterName, ServiceFabricNode node, bool copyPortsFromPrimary=true)
        {
            var resourceUrl = $"https://management.azure.com/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ServiceFabric/clusters/{clusterName}?api-version=2015-01-01-alpha";

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
       

            return await Client.PutAsync(resourceUrl, new StringContent(JsonConvert.SerializeObject(req))).As<ServiceFabricCluster>();

        }

        public Task<ServiceFabricCluster> PutClusterInfoAsync(ServiceFabricCluster update)
        {
            var resourceUrl = $"https://management.azure.com/{update.Id}?api-version=2015-01-01-alpha";

            return Client.PutAsync(resourceUrl, new StringContent(JsonConvert.SerializeObject(update, new JsonSerializerSettings
            {
                 NullValueHandling = NullValueHandling.Ignore,
                 ContractResolver =  new CamelCasePropertyNamesContractResolver(),
            }),Encoding.UTF8,"application/json"))
              .As<ServiceFabricCluster>();
        }
    }
}
