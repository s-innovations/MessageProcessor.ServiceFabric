using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM
{
    public static class ServiceFabricConstants
    {
        public const string ClusterGATemplate = "SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM.Templates.ServiceFabricGACluster.json";
        public const string ClusterTemplate = "SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM.Templates.Cluster.json";
        public const string VmssTemplate = "SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM.Templates.AddNodeType.json";

        public static class ActorServiceTypes
        {
            public const string QueueListenerActorService = "QueueListenerActorServiceType";
        }
    }
}
