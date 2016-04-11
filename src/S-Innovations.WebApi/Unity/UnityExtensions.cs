using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using SInnovations.WebApi.Services;

namespace SInnovations.WebApi.Unity
{
    public static class UnityExtensions
    {
        public static void RegisterDependencyResolver(this IUnityContainer container)
        {
            container.RegisterType<IDependencyScope>(new HierarchicalLifetimeManager(), new InjectionFactory((ctx) => new UnityDependencyScope(ctx, false)));
            container.RegisterType<IDependencyResolver>(new HierarchicalLifetimeManager(), new InjectionFactory(ctx => ctx.Resolve<IDependencyScope>().GetDependencyResolver()));

        }
        public static IUnityContainer UseAscendDependencyResolver(this IUnityContainer container)
        {
            container.RegisterDependencyResolver();
            return container;
        }
    }
}
