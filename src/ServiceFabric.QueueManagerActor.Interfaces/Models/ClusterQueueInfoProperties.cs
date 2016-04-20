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

            ListenerDescription = new ListenerDescription();
        }
       // public ClusterVmssInfo Vmss { get; set; }
        public ServiceBusInfo ServiceBus { get; set; }
        public QueueInfo QueueDescription { get; set; }
        public ListenerDescription ListenerDescription { get; set; }


    }
}
