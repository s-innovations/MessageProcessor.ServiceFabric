using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using SInnovations.WebApi.Services;

namespace SInnovations.WebApi.Unity
{
    internal class UnityDependencyResolver : IDependencyResolver
    {

        readonly IUnityContainer ctx;
        public UnityDependencyResolver(IUnityContainer ctx)
        {
            this.ctx = ctx;
        }

        public T Resolve<T>(string name)
        {
            if (name != null)
            {
                return ctx.Resolve<T>(name);
            }

            return ctx.Resolve<T>();
        }

        public IDependencyScope GetParentScope()
        {
            //var root = ctx;
            //while (root.Parent != null)
            //{
            //    Logger.InfoFormat("has parent {0}", root.GetHashCode());
            //    root = root.Parent;
            //}
            return new UnityDependencyScope(ctx.Parent);
        }


        public object Resolve(Type type)
        {
            return ctx.Resolve(type);
        }

        public IEnumerable<object> ResolveAll(Type type)
        {
            return ctx.ResolveAll(type);
        }
    }
}
