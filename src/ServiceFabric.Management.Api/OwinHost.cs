using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Owin;
using System.IO;
using Microsoft.ServiceFabric.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using System.Fabric;
using System.Web.Http;
using SInnovations.WebApi.Filters;
using System.Net.Http.Formatting;
using Newtonsoft.Json.Serialization;
using SInnovations.WebApi.Formatters;
using SInnovations.WebApi.Owin;
using Thinktecture.IdentityModel.Owin;
using System.Security.Claims;
using Microsoft.Practices.Unity;
namespace ServiceFabric.Management.Api
{


    public class OwinHost : IOwinAppBuilder
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var sfConfig = appBuilder.GetUnityContainer().Resolve<ServiceContext>().CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var target = sfConfig.Settings.Sections["AppSettings"].Parameters["BasicAuth"].Value;

            appBuilder.UseBasicAuthentication(new BasicAuthenticationOptions("messagecluster", (username,password)=> {
                if (string.Equals(target, $"{username}:{password}"))
                {
                    return Task.FromResult<IEnumerable<Claim>>(new[] { new Claim("sub", "username") });
                }
                return null;
            }));

            HttpConfiguration config = new HttpConfiguration();
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter("Basic"));
            config.MapHttpAttributeRoutes();
            config.Filters.Add(new ValidateModelAttribute());

            var jsonFormatter = new JsonMediaTypeFormatter();
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Services.Replace(typeof(IContentNegotiator), new JsonContentNegotiator(jsonFormatter));
            config.MessageHandlers.Insert(0, new KatanaDependencyResolver());

            appBuilder.UseWebApi(config);
            
        }

        
    }

}
