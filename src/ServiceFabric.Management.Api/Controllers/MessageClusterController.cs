using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management.Api.Attributes;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;
using SInnovations.WebApi.Bindings;
using Owin;
using Microsoft.Owin.Extensions;
using System.Net.Http;

namespace ServiceFabric.Management.Api.Controllers
{

    [Authorize]
    public class MessageClusterController : ApiController
    {
        /// <summary>
        /// Cluster Configuration Store
        /// </summary>       
        protected IMessageClusterConfigurationStore ClusterConfigStore { get; private set; }


        public MessageClusterController(IMessageClusterConfigurationStore clusterProvider)
        {
            ClusterConfigStore = clusterProvider;
        }


        //GET {subscriptionId}/{resourceGroup}/providers/SInnovations.MessageProcessor/MessageCluster/{clusterName}"
        [ClusterRoute]
        [HttpGet]
        public async Task<IHttpActionResult> GetClusterInfo([FromClusterRoute(true)]IMessageClusterActor cluster)
        {
            if (cluster == null)
                return NotFound();
            var sw = Stopwatch.StartNew();
            var model = await cluster.GetModelAsync();
            sw.Stop();
            Request.GetOwinContext().Response.Headers.Add("x-actor-processingtime", new string[] { sw.ElapsedMilliseconds.ToString() });
           
            return Ok(new JRaw(model.Value));         
        }

        [ClusterRoute]
        [HttpPut]
        public async Task<IHttpActionResult> AddOrUpdateClusterInfo([FromClusterRoute]IMessageClusterActor cluster, MessageClusterResource model)
        {
            model.Name = cluster.GetClusterName();
            var sw = Stopwatch.StartNew();
            var jsonModel = await cluster.UpdateModelAsync(new JsonModel<MessageClusterResource>(model));
            var value = new JRaw(jsonModel.Value);
            sw.Stop();
            Request.GetOwinContext().Response.Headers.Add("x-actor-processingtime", new string[] { sw.ElapsedMilliseconds.ToString() });
            //  await cluster.StartMonitoringAsync();
            return Ok(new JRaw(value));

         //   return Ok(configuratio);
        }


        //POST {subscriptionId}/{resourceGroup}/providers/SInnovations.MessageProcessor/MessageCluster/{clusterName}/start"
        [ClusterRoute("start")]
        [HttpGet]
        [HttpPost]
        public async Task<IHttpActionResult> StartCluster([FromClusterRoute(true)]IMessageClusterActor cluster)
        {
            var status = await cluster.StartMonitoringAsync();
            return Ok(status);
        }
        
        //POST {subscriptionId}/{resourceGroup}/providers/SInnovations.MessageProcessor/MessageCluster/{clusterName}/stop"
        [ClusterRoute("stop")]
        [HttpGet]
        [HttpPost]
        public async Task<IHttpActionResult> StopCluster([FromClusterRoute(true)]IMessageClusterActor cluster)
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
