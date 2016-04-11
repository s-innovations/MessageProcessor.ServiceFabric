using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Practices.Unity;
using Owin;
using SInnovations.WebApi.Services;
using SInnovations.WebApi.Unity;

namespace SInnovations.WebApi.Owin
{
    public static class OwinExtensions
    {
        private const string UnityRequestContainer = "sinno:unity:container";
        public static IDependencyScope GetDependencyScope(this IDictionary<string, object> env)
        {
            return env[UnityRequestContainer] as IDependencyScope;
        }
        public static void SetDependencyScope(this IDictionary<string, object> env, IDependencyScope scope)
        {
            env[UnityRequestContainer] = scope;
        }
        public static IUnityContainer UseUnityContainer(this IAppBuilder app,IUnityContainer container = null)
        {

            if (app.Properties.ContainsKey(UnityRequestContainer))
                return app.GetUnityContainer();

   
            app.Properties.Add(UnityRequestContainer, container ?? (container = new UnityContainer()));
            container.RegisterDependencyResolver();

            app.Use(typeof(IoCMiddleware), container.Resolve<IDependencyScope>());

            return container;
        }
        public static IUnityContainer GetUnityContainer(this IAppBuilder app)
        {
            return app.Properties[UnityRequestContainer] as IUnityContainer;
        }
        public static T ResolveDependency<T>(this IOwinContext ctx)
        {
            var scope = ctx.Environment.GetDependencyScope();// ctx.Get<IUnityContainer>(Constants.OwinContextProperties.UnityRequestContainer);
            return scope.GetDependencyResolver().Resolve<T>();

        }
        public static T ResolveDependency<T>(this IDictionary<string, object> env)
        {
            var scope = env.GetDependencyScope();// ctx.Get<IUnityContainer>(Constants.OwinContextProperties.UnityRequestContainer);
            return scope.GetDependencyResolver().Resolve<T>();
           
        }
    }

    public class IoCMiddleware
    {
        readonly private Func<IDictionary<string, object>, Task> _next;
        readonly private IDependencyScope _scope;

        public IoCMiddleware(Func<IDictionary<string, object>, Task> next, IDependencyScope scope)
        {
            _next = next;
            _scope = scope;

        }

        public async Task Invoke(IDictionary<string, object> env)
        {
          
            using (var scope = _scope.GetChildScope())
            {
             
                env.SetDependencyScope(scope);
 

                await _next(env);
            }
        }
    }
}
