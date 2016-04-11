using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{
    [JsonConverter(typeof(MessageClusterResourceBaseConverter))]
    public abstract class MessageClusterResourceBase
    { 
        public const string ClusterQueueType = "SInnovations.MessageProcessor/queue";
        public const string MessageClusterType = "SInnovations.MessageProcessor/MessageCluster";

        public string Name { get; set; }
        
        public abstract string Type { get;}
    }
}
