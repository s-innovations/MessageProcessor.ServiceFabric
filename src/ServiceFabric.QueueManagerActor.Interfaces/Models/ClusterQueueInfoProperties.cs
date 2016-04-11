using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{
    public class ClusterQueueInfoProperties
    {
        public ClusterQueueInfoProperties()
        {
            PlacementProperties = new Dictionary<string, string>();
        }
        public ClusterVmssInfo Vmss { get; set; }
        public ServiceBusInfo ServiceBus { get; set; }
        public Dictionary<string, string> PlacementProperties { get; set; }
    }
}
