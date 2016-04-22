using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.WebApi.Logging;
using SInnovations.WebApi.Owin;

namespace SInnovations.WebApi.Bindings
{
   
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class FromClusterRouteAttribute : ParameterBindingAttribute
    {
        //  private static ILog Logger = LogProvider.GetCurrentClassLogger();
        private bool _validateExistance = false;


        public FromClusterRouteAttribute(bool validateExistance=false)
        {
            this._validateExistance = validateExistance;
        }
       
        public class ClusterActorBinder : IModelBinder
        {
            public string ModelName { get; set; }

            public bool ValidateExistance { get; set; }
            public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
            {
         
                actionContext.Bind(bindingContext);
                var ctx = actionContext.Request.GetOwinContext();
       
                var subscriptionid = bindingContext.ValueProvider.GetValue("subscriptionId").RawValue;
                var clusterName = bindingContext.ValueProvider.GetValue("clusterName").RawValue;
                var resourceGroupName = bindingContext.ValueProvider.GetValue("resourceGroupName").RawValue;

                var clusterKey = $"{subscriptionid}/{resourceGroupName}/{clusterName}";
                if (ValidateExistance) {
                    var store = ctx.ResolveDependency<IMessageClusterConfigurationStore>();
                    if (!store.ClusterExistsAsync(clusterKey).GetAwaiter().GetResult())
                    {
                        actionContext.Response = actionContext.Request.CreateErrorResponse(
                             HttpStatusCode.NotFound,new HttpError("Message Cluster not found for "+clusterKey));
                        return false;
                      
                    }

                }

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

            return parameter.BindWithModelBinding(new ClusterActorBinder {  ValidateExistance = _validateExistance});

        }
    }
}
