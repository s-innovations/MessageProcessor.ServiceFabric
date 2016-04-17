using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{

    [VariableReplacable]
    public class ClusterQueueInfoProperties
    {
        public ClusterQueueInfoProperties()
        {
            PlacementProperties = new Dictionary<string, string>();
            Capacities = new Dictionary<string, int>();
            ListenerDescription = new ListenerDescription();
        }
        public ClusterVmssInfo Vmss { get; set; }
        public ServiceBusInfo ServiceBus { get; set; }
        public QueueInfo QueueDescription { get; set; }
        public ListenerDescription ListenerDescription { get; set; }
        public Dictionary<string, string> PlacementProperties { get; set; }
        public Dictionary<string, int> Capacities { get; set; }

    }
}
