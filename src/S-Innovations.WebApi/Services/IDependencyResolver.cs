

namespace SInnovations.WebApi.Services
{
    using System;
    using System.Collections.Generic;
    public interface IDependencyScope : IDisposable
    {
        IDependencyResolver GetDependencyResolver();
        IDependencyScope GetChildScope();
    }
    public interface IDependencyResolver
    {
       
        T Resolve<T>(string name = null);

        object Resolve(Type type);
        IEnumerable<object> ResolveAll(Type type);
        IDependencyScope GetParentScope();
    }
}
