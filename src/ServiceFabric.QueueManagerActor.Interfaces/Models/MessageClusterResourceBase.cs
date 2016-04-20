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
        public const string ClusterQueueType = "S-Innovations.MessageProcessor/queue";
        public const string MessageClusterType = "S-Innovations.MessageProcessor/MessageCluster";
        public const string DispatcherType = "S-Innovations.MessageProcessor/dispatcher";
        public const string ProcessorNodeType = "S-Innovations.MessageProcessor/processorNode";

        public string Name { get; set; }
        
        public abstract string Type { get;}

        
    }
}
