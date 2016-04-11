using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.Management.Api;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Owin;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Tracing;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Services
{
    //public class ManagementApiServiceFactory : IStatelessServiceFactory
    //{
    //    private readonly IUnityContainer container;
    //    public ManagementApiServiceFactory(IUnityContainer container)
    //    {
    //        this.container = container;
    //    }

    //    public IStatelessServiceInstance CreateInstance(string serviceTypeName, Uri serviceName, byte[] initializationData, Guid partitionId, long instanceId)
    //    {
        
    //        switch (serviceTypeName)
    //        {
    //            case "ManagementApiServiceType":
    //                return new ManagementApiService(this.container);
    //            default:
    //                throw new NotImplementedException(serviceTypeName);
    //        }
    //    }
    //}
    internal sealed class ManagementApiService : StatelessService
    {
        public const string ServiceType = "ManagementApiServiceType";
        private readonly IUnityContainer container;
        public ManagementApiService(IUnityContainer container,StatelessServiceContext context) : base(context)
        {
            this.container = container;
        }
        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
           // var configurationPackage = this.ServiceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config");
           // var connectionStringParameter = configurationPackage.Settings.Sections["UserDatabase"].Parameters["UserDatabaseConnectionString"];
            // TODO: If your service needs to handle user requests, return a list of ServiceReplicaListeners here.
            return new[]
            {
                new ServiceInstanceListener(initParams => new OwinCommunicationListener(new OwinHost(),initParams, "ServiceEndpoint",container,"webapp"))
            };
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        protected override Task RunAsync(CancellationToken cancelServiceInstance)
        {
            


            return base.RunAsync(cancelServiceInstance);
        }
    }
}
