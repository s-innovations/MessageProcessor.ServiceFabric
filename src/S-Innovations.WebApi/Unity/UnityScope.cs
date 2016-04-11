

namespace SInnovations.WebApi.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dependencies;
    using Microsoft.Practices.Unity;
    using SInnoDependencyResolver = SInnovations.WebApi.Services.IDependencyResolver;
    using SInnoDependencyScope = SInnovations.WebApi.Services.IDependencyScope;

    public class UnityScope : IDependencyScope
    {
        protected SInnoDependencyScope Container { get; private set; }
        protected SInnoDependencyResolver Resolver { get; private set; }

        public UnityScope(SInnoDependencyScope container)
        {
            Container = container;
            Resolver = container.GetDependencyResolver();
        }

        public object GetService(Type serviceType)
        {
            if (typeof(IHttpController).IsAssignableFrom(serviceType))
            {
                return Resolver.Resolve(serviceType);
            }
           
            try
            {
                return Resolver.Resolve(serviceType);
            }
            catch
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Resolver.ResolveAll(serviceType);
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
    
    

    
}
