using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management.Api.Attributes;
using SInnovations.WebApi.Bindings;

namespace ServiceFabric.Management.Api.Controllers
{

    public class MessageClusterController : ApiController
    {
        
        public MessageClusterController()
        {
           
        }


        //POST {subscriptionId}/{resourceGroup}/providers/SInnovations.MessageProcessor/MessageCluster/{clusterName}/start"
        [ClusterRoute("start")]
        [HttpGet]
        [HttpPost]
        public async Task<IHttpActionResult> StartCluster([FromClusterRoute]IMessageClusterActor cluster)
        {
            var status = await cluster.StartMonitoringAsync();
            return Ok(status);
        }
        
        //POST {subscriptionId}/{resourceGroup}/providers/SInnovations.MessageProcessor/MessageCluster/{clusterName}/stop"
        [ClusterRoute("stop")]
        [HttpPost]
        public async Task<IHttpActionResult> StopCluster([FromClusterRoute]IMessageClusterActor cluster)
        {
            var status = await cluster.StopMonitoringAsync();
            return Ok(status);
        }

        //POST {subscriptionId}/{resourceGroup}/providers/SInnovations.MessageProcessor/MessageCluster/{clusterName}/pause"
        [ClusterRoute("pause")]
        [HttpPost]
        public async Task<IHttpActionResult> PauseCluster([FromClusterRoute]IMessageClusterActor cluster)
        {
            var status = await cluster.StartMonitoringAsync();
            return Ok(status);
        }
    }
}
