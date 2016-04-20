using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{
    public class ClusterProcessorNode : MessageClusterResourceBase
    {
        public override string Type { get; } = ProcessorNodeType;
        public ClusterVmssInfo Properties { get; set; }
    }
    public class ClusterQueueInfo : MessageClusterResourceBase
    {
        public override string Type { get; } = ClusterQueueType;

        public ClusterQueueInfoProperties Properties { get; set; }
    }
}
