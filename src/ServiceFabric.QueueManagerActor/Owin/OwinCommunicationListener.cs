

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Owin
{
    using System;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin.Hosting;
    using Microsoft.Practices.Unity;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Tracing;
    using SInnovations.WebApi.Owin;
    using global::Owin;

    public class OwinCommunicationListener : ICommunicationListener
    {

        private readonly IOwinAppBuilder startup;
        private readonly ServiceContext serviceContext;
        private readonly string endpointName;
        private readonly string appRoot;

        private IDisposable webApp;
        private string publishAddress;
        private string listeningAddress;

        private readonly IUnityContainer container;

        public OwinCommunicationListener(IOwinAppBuilder startup, ServiceContext serviceContext,string endpointName, IUnityContainer container)
            : this(startup, serviceContext, endpointName,container, null)
        {
        }

        public OwinCommunicationListener(IOwinAppBuilder startup, ServiceContext serviceContext, string endpointName, IUnityContainer container,string appRoot)
        {
            if (startup == null)
            {
                throw new ArgumentNullException(nameof(startup));
            }

            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

           
            this.container = container;
            this.startup = startup;
            this.serviceContext = serviceContext;
            this.endpointName = endpointName;
            this.appRoot = appRoot;
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint(this.endpointName);
            int port = serviceEndpoint.Port;

            if (this.serviceContext is StatefulServiceContext)
            {
                StatefulServiceContext statefulServiceContext = this.serviceContext as StatefulServiceContext;

                this.listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}{2}/{3}/{4}",
                    port,
                    string.IsNullOrWhiteSpace(this.appRoot)
                        ? string.Empty
                        : this.appRoot.TrimEnd('/') + '/',
                    statefulServiceContext.PartitionId,
                    statefulServiceContext.ReplicaId,
                    Guid.NewGuid());
            }
            else if (this.serviceContext is StatelessServiceContext)
            {
                this.listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    string.IsNullOrWhiteSpace(this.appRoot)
                        ? string.Empty
                        : this.appRoot.TrimEnd('/') + '/');
            }
            else
            {
                throw new InvalidOperationException();
            }

            this.publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            try
            {
                ServiceFabricEventSource.Current.ServiceMessage(this.serviceContext, "Starting web server on " + this.listeningAddress);

                this.webApp = WebApp.Start(this.listeningAddress, appBuilder => { appBuilder.UseUnityContainer(this.container); this.startup.Configuration(appBuilder); });

                ServiceFabricEventSource.Current.ServiceMessage(this.serviceContext, "Listening on " + this.publishAddress);

                return Task.FromResult(this.publishAddress);
            }
            catch (Exception ex)
            {
                ServiceFabricEventSource.Current.ServiceMessage(this.serviceContext, "Web server failed to open. " + ex.ToString());

                this.StopWebServer();

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            ServiceFabricEventSource.Current.Message("Close");

            this.StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            ServiceFabricEventSource.Current.Message("Abort");

            this.StopWebServer();
        }

        private void StopWebServer()
        {
            if (this.webApp != null)
            {
                try
                {
                    this.webApp.Dispose();
                   
                }
                catch (ObjectDisposedException)
                {
                    // no-op
                }
            }
            if(this.container  != null)
            {
                try
                {
                    this.container.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    //no-op
                }
            }
        }
    }
}
