using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.WebApi.Logging;
using SInnovations.WebApi.Owin;

namespace SInnovations.WebApi.Bindings
{
   
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class FromClusterRouteAttribute : ParameterBindingAttribute
    {
      //  private static ILog Logger = LogProvider.GetCurrentClassLogger();

        
        public FromClusterRouteAttribute()
        {

        }
       
        public class ClusterActorBinder : IModelBinder
        {
            public string ModelName { get; set; }

            public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
            {
         
                actionContext.Bind(bindingContext);
                var ctx = actionContext.Request.GetOwinContext();
            //    var serviceContext = ctx.ResolveDependency<ServiceContext>();
                var subscriptionid = bindingContext.ValueProvider.GetValue("subscriptionId").RawValue;
                var clusterName = bindingContext.ValueProvider.GetValue("clusterName").RawValue;
                var resourceGroupName = bindingContext.ValueProvider.GetValue("resourceGroupName").RawValue;

                var clusterKey = $"{subscriptionid}/{resourceGroupName}/{clusterName}";

                bindingContext.Model = ActorProxy.Create<IMessageClusterActor>(new ActorId(clusterKey));
                return true;

            }
        }

        public override HttpParameterBinding GetBinding(HttpParameterDescriptor parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            return parameter.BindWithModelBinding(new ClusterActorBinder());

        }
    }
}
