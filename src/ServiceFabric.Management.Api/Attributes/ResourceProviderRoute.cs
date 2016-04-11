using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Management.Api.Attributes
{
    public class ResourceProviderRouteAttribute : RouteFactoryAttribute
    {
        public ResourceProviderRouteAttribute(string provider,string route) 
            : base("{subscriptionId}/{resourceGroupName}/providers/"+ provider +"/"+ route)
        {

        }
    }
    public class ClusterRouteAttribute : ResourceProviderRouteAttribute
    {
        public ClusterRouteAttribute(string action= ""):base("SInnovations.MessageProcessor/MessageCluster", "{clusterName}" +"/"+ action.TrimStart('/'))
        {

        }
    }
}
