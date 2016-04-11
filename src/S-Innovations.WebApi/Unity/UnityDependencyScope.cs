using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using SInnovations.WebApi.Services;

namespace SInnovations.WebApi.Unity
{
    internal class UnityDependencyScope : IDependencyScope
    {
      //  static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly IUnityContainer _container;
        private readonly bool disposeContainer;
        public UnityDependencyScope(IUnityContainer container) : this(container, true)
        {

        }
        internal UnityDependencyScope(IUnityContainer container, bool ignoreDispose)
        {
            this._container = container;
            disposeContainer = ignoreDispose;
        }

        public void Dispose()
        {
            if (disposeContainer)
                this._container.Dispose();
        }

        public IDependencyResolver GetDependencyResolver()
        {
            return new UnityDependencyResolver(_container);
        }


        public IDependencyScope GetChildScope()
        {
            return new UnityDependencyScope(_container.CreateChildContainer());
        }
    }
}
