using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Models
{
    public class TopicInfo : MessageClusterResourceBase
    {
        public override string Type { get; } = TopicType;

        public TopicInfoProperties Properties { get; set; }

        public  class TopicInfoProperties
        {
            public int TopicScaleCount { get; set; } = 1;
            public ServiceBusInfo ServiceBus { get; set; }
        }
    }
    public class ClusterDispatcherInfo : MessageClusterResourceBase
    {
        public override string Type { get; } = DispatcherType;

        public ClusterDispatcherInfoProperties Properties { get; set; }

        public class ClusterDispatcherInfoProperties : TopicInfo.TopicInfoProperties
        {
           
            public Dictionary<string,string> CorrelationFilters { get; set; }

 
        }
    }
}
