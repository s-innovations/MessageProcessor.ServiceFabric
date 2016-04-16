using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{
    public class ClusterDispatcherInfo : MessageClusterResourceBase
    {
        public override string Type { get; } = DispatcherType;

        public ClusterDispatcherInfoProperties Properties { get; set; }

        public class ClusterDispatcherInfoProperties
        {
            public int TopicScaleCount { get; set; } = 1;
            public Dictionary<string,string> CorrelationFilters { get; set; }

            public ServiceBusInfo ServiceBus { get; set; }
        }
    }
}
