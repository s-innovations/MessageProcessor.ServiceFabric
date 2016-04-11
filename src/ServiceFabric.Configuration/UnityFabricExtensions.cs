﻿using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration
{

    /// <summary>
    /// The <see cref="IActorDeactivationInterception"/> interface for defining an OnDeactivateInterception
    /// </summary>
    public interface IActorDeactivationInterception
    {
        void Intercept();
    }

    public class OnActorDeactivateInterceptor : IActorDeactivationInterception
    {
        private readonly IUnityContainer container;
        public OnActorDeactivateInterceptor(IUnityContainer container)
        {
            this.container = container;
        }

        public void Intercept()
        {
            this.container.Dispose();
        }
    }
     
  
    public class ActorProxyTypeFactory
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ActorProxyTypeFactory"/> class.
        /// </summary>
        /// <param name="target"></param>
        public ActorProxyTypeFactory(Type target)
        {
            this.target = target;
        }

        /// <summary>
        /// Creates the proxy registered with specific interceptor.
        /// </summary>
        /// <returns></returns>
        public static T Create<T>(IActorDeactivationInterception deactivation, params object[] args)
        {
            return (T)new ActorProxyTypeFactory(typeof(T)).Create(new object[] { deactivation }.Concat(args).ToArray());
        }
        public static Type CreateType<T>()
        {
            return new ActorProxyTypeFactory(typeof(T)).CreateType();
        }
        /// <summary>
        /// Creates the proxy registered with specific interceptor.
        /// </summary>
        /// <returns></returns>
        public object Create(object[] args)
        {
            BuidAssembly();
            BuildType();
            InterceptAllMethods();

            Type proxy = this.typeBuilder.CreateType();

            return Activator.CreateInstance(proxy, args);
        }

        public Type CreateType()
        {
            BuidAssembly();
            BuildType();
            InterceptAllMethods();

            Type proxy = this.typeBuilder.CreateType();
            return proxy;
          //  return Activator.CreateInstance(proxy, args);
        }

        /// <summary>
        /// Builds a dynamic assembly with <see cref="AssemblyBuilderAccess.RunAndSave"/> mode.
        /// </summary>
        /// <returns></returns>
        public void BuidAssembly()
        {
            AssemblyName assemblyName = new AssemblyName("BasicProxy");
            AssemblyBuilder createdAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            // define module
            this.moduleBuilder = createdAssembly.DefineDynamicModule(assemblyName.Name);
        }

        public void BuildType()
        {
            if (!target.IsPublic)
            {
                throw new ArgumentException("Actors have to be public defined to proxy them");
            }
            

            this.typeBuilder =
                this.moduleBuilder.DefineType(target.FullName+"Proxy", TypeAttributes.Class | TypeAttributes.Public, target);
            this.fldInterceptor = this.typeBuilder.DefineField("interceptor", typeof(IActorDeactivationInterception), FieldAttributes.Private);

            foreach (var constructor in target.GetConstructors())
            {
              //  Type[] parameters = new Type[1];

                ParameterInfo[] parameterInfos = constructor.GetParameters();
                Type[] parameters = new Type[parameterInfos.Length + 1];

                parameters[0] = typeof(IActorDeactivationInterception);


                for (int index = 1; index <= parameterInfos.Length; index++)
                {
                    parameters[index] = parameterInfos[index-1].ParameterType;
                }

                ConstructorBuilder constructorBuilder =
                    typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameters);

                for (int argumentIndex = 0; argumentIndex < parameters.Length; argumentIndex++)
                    constructorBuilder.DefineParameter(
                        argumentIndex + 1,
                        ParameterAttributes.None,
                        $"arg{argumentIndex}");

                ILGenerator generator = constructorBuilder.GetILGenerator();

                generator.Emit(OpCodes.Ldarg_0);

                for (int index = 1; index < parameters.Length; index++)
                {
                    generator.Emit(OpCodes.Ldarg, index + 1);
                }

                generator.Emit(OpCodes.Call, constructor);

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Stfld, fldInterceptor);
                generator.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// Builds a type in the dynamic assembly, if already the type is not created.
        /// </summary>
        /// <returns></returns>
        public void InterceptAllMethods()
        {
         
            const MethodAttributes targetMethodAttributes =
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;

            var methodInfo = target.GetMethod("OnDeactivateAsync", BindingFlags.NonPublic | BindingFlags.Instance| BindingFlags.FlattenHierarchy);
            {
                if (methodInfo.IsVirtual)
                {
                    Type[] paramTypes = GetParameterTypes(methodInfo.GetParameters());

                    MethodBuilder methodBuilder =
                        typeBuilder.DefineMethod(methodInfo.Name, targetMethodAttributes, methodInfo.ReturnType, paramTypes);
                     
                    ILGenerator ilGenerator = methodBuilder.GetILGenerator();


                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, fldInterceptor);
                    ilGenerator.Emit(OpCodes.Call, typeof(IActorDeactivationInterception).GetMethod("Intercept"));

                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Call, methodInfo);
                    ilGenerator.Emit(OpCodes.Ret);

                    return;

                  
                }
            }
        }

      

        private Type[] GetParameterTypes(ParameterInfo[] parameterInfos)
        {
            Type[] parameters = new Type[parameterInfos.Length];

            int index = 0;

            foreach (var parameterInfo in parameterInfos)
            {
                parameters[index++] = parameterInfo.ParameterType;
            }
            return parameters;
        }

        private TypeBuilder typeBuilder;
        private ModuleBuilder moduleBuilder;
        private readonly Type target;
        private FieldInfo fldInterceptor;

    }

   
    public static class UnityFabricExtensions
    {
        public static IUnityContainer WithFabricContainer(this IUnityContainer container)
        {
            return container.WithFabricContainer(c => FabricRuntime.Create());
        }
        public static IUnityContainer WithFabricContainer(this IUnityContainer container, Func<IUnityContainer, FabricRuntime> factory)
        {
            container.RegisterType<FabricRuntime>(new ContainerControlledLifetimeManager(), new InjectionFactory(factory));
            return container;
        }


        public static IUnityContainer WithActor<TActor>(this IUnityContainer container, ActorServiceSettings settings=null) where TActor : ActorBase
        {
            if (!container.IsRegistered<IActorDeactivationInterception>())
            {
                container.RegisterType<IActorDeactivationInterception, OnActorDeactivateInterceptor>(new HierarchicalLifetimeManager());
            }

            container.RegisterType(typeof(TActor),ActorProxyTypeFactory.CreateType<TActor>(),new HierarchicalLifetimeManager());
            ActorRuntime.RegisterActorAsync<TActor>((context, actorType) => {
                try
                {
                    return new ActorService(context, 
                        actorTypeInfo: actorType,
                        actorFactory:() => container.CreateChildContainer().Resolve<TActor>(), 
                        settings:settings);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }).GetAwaiter().GetResult();




            return container;
        }
       
       
        public static IUnityContainer WithStatelessService<TStatelessService>(this IUnityContainer container, string serviceTypeName) where TStatelessService : StatelessService
        {
            //if (!container.IsRegistered<TFactory>())
            //{
            //    container.RegisterType<TFactory>(new ContainerControlledLifetimeManager());
            //}
            ServiceRuntime.RegisterServiceAsync(serviceTypeName, (context) => container.CreateChildContainer().RegisterInstance(context,new ContainerControlledLifetimeManager()).Resolve<TStatelessService>());
            //container.Resolve<FabricRuntime>().RegisterStatelessServiceFactory(serviceTypeName, container.Resolve<TFactory>());


            return container;
        }
        public static IUnityContainer WithStatefulFactory<TFactory>(this IUnityContainer container, string serviceTypeName) where TFactory : IStatefulServiceFactory
        {
            if (!container.IsRegistered<TFactory>())
            {
                container.RegisterType<TFactory>(new ContainerControlledLifetimeManager());
            }
            container.Resolve<FabricRuntime>().RegisterStatefulServiceFactory(serviceTypeName, container.Resolve<TFactory>());
            return container;
        }
        public static IUnityContainer WithService<TService>(this IUnityContainer container, string serviceTypeName) 
        {
            container.Resolve<FabricRuntime>().RegisterServiceType(serviceTypeName, typeof(TService));
            return container;
        }
    }
}
