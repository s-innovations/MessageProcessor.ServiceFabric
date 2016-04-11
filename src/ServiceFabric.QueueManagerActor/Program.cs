using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;
using SInnovations.Azure.MessageProcessor.Core;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Tracing;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric
{
    //https://azure.microsoft.com/en-gb/documentation/articles/service-fabric-resource-balancer-service-description/

    public class DummyFactory : IMessageProcessorClientFactory
    {
        public Task<IMessageProcessorClient> CreateMessageProcessorAsync(string key)
        {
            return Task.FromResult<IMessageProcessorClient>(new DummyProcessor());
        }
    }
    public class DummyProcessor : IMessageProcessorClient
    {
        public void Dispose()
        {

        }

        public Task RestartProcessorAsync()
        {
            return Task.FromResult(0);
        }

        public void SignalRestartOnNextAllCompletedMessage()
        {

        }

        public Task StartProcessorAsync()
        {
            return Task.FromResult(0);
        }

        public Task StopProcessorAsync()
        {
            return Task.FromResult(0);
        }
    }
    public class test : IMessageClusterConfigurationStore
    {
        public async Task<MessageClusterResource> GetMessageClusterAsync(string clusterKey)
        {
            var parts = clusterKey.Split('/');

            var stream = typeof(ServiceFabricConstants).Assembly.GetManifestResourceStream("SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.sampleConfiguration.json");
            var cluster = JsonConvert.DeserializeObject<MessageClusterResource>(await new StreamReader(stream).ReadToEndAsync(), new JsonSerializerSettings {});
            
            return cluster;
        }

        public async Task<MessageClusterResourceBase> GetMessageClusterResourceAsync(string clusterKey)
        {
            var parts = clusterKey.Split('/');

            var stream = typeof(ServiceFabricConstants).Assembly.GetManifestResourceStream("SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.sampleConfiguration.json");
            var cluster = JsonConvert.DeserializeObject<MessageClusterResource>(await new StreamReader(stream).ReadToEndAsync(), new JsonSerializerSettings {});

            var name = parts.Last();
            return cluster.Resources.FirstOrDefault(n => n.Name == name);
        }

    }
    //http://help.appveyor.com/discussions/questions/1625-service-fabric

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                using (var container = new UnityContainer())
                {
                    container.RegisterType<IMessageProcessorClientFactory, DummyFactory>(new HierarchicalLifetimeManager());
                    container.RegisterType<IMessageClusterConfigurationStore, test>(new HierarchicalLifetimeManager());

                    container.WithFabricContainer();
                    container.WithActor<MessageClusterActor>();
                    container.WithActor<QueueListenerActor>();
                    container.WithStatelessService<ManagementApiService>(ManagementApiService.ServiceType);
                    container.WithActor<VmssManagerActor>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(120, 60)
                    });
                    container.WithActor<QueueManagerActor>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(120, 60)
                    });

                    ServiceFabricEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(ManagementApiService).Name);

                    Thread.Sleep(Timeout.Infinite);  // Prevents this host process from terminating to keep the service host process running.
                }
            }
            catch (Exception e)
            {
                ServiceFabricEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
