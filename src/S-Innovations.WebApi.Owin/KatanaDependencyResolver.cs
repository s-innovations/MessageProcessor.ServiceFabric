

namespace SInnovations.WebApi.Owin
{
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Hosting;
    using SInnovations.WebApi.Unity;

    public class KatanaDependencyResolver : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var owin = request.GetOwinContext();
            var scope = request.GetOwinEnvironment().GetDependencyScope();
            // var scope = owin.Get<IUnityContainer>(Constants.OwinContextProperties.UnityRequestContainer);
            if (scope != null)
            {
                request.Properties[HttpPropertyKeys.DependencyScope] = new UnityScope(scope);
            }
            else
            {
                Trace.TraceWarning("{0} not regiested (no dependency injeciton on controllers), make sure unityconatinermiddleware is running");

            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
