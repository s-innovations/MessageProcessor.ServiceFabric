﻿using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Practices.Unity;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;
using SInnovations.Azure.MessageProcessor.Core;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Tracing;
using System.Collections.Generic;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Stores;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Security.Cryptography.Pkcs;
using SInnovations.ServiceFabric.Unity;

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
    
    
    //http://help.appveyor.com/discussions/questions/1625-service-fabric

    internal static class Program
    {
        private static TelemetryClient CreateTelemetryClientFromInstrumentationkey(string instrumentationKey = "")
        {
            var telemetryClient = new TelemetryClient();

            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                telemetryClient.InstrumentationKey = instrumentationKey;
            }

            telemetryClient.TrackTrace("Initializing from Service Fabric", SeverityLevel.Information);
            telemetryClient.Flush();
            return telemetryClient;
        }
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
          


            var settings = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("config").Settings;
            var appInsight = settings.Sections["AppSettings"]?.Parameters.FirstOrDefault(p => p.Name == "AppInsights")?.Value;              //  Log.Logger = new LoggerConfiguration()
            TelemetryConfiguration.Active.InstrumentationKey = appInsight;
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = false;
            
            LogProvider.SetCurrentLogProvider(ServiceFabricEventSource.Current);

            var host = new TelemetryClient();
            host.TrackTrace("Host Starting");
            try
            {

                var encrypted64 = "MIICLgYJKoZIhvcNAQcDoIICHzCCAhsCAQAxggFFMIIBQQIBADApMBUxEzARBgNVBAMTCmNpdGVzdGNlcnQCEG7nJgAd1pGtROAqXTINhgowDQYJKoZIhvcNAQEBBQAEggEARESvyeELwgWe08JSmoBKOIdFC3Tgw9wPpG6/BLFVtDI0kYgAZr74ftz/QSj7cyt9mTeJGt1WO8MHR/Q3QV/dkxyW/7NPU7dIrM8xliHwtCHQO5gZVnjARbpwPdYEi8NayqB3iRd+h8Sa4B3GudwX5xzZr5d2GmJ6FDL2F5UHYTF6zm7HUps28UybGgzgmRymE+9qoChnEA/DATPGq4w019pcIdkgZP8kodacqCVe9Bl4Izlgl44NTgkW9YSgGH550SMkK60Z/Z9TMzHcBRKyePUyYkwNeZ1uoMMilA7Zm5jK93iqD1ThTAdOodpgG4gy7CfIReqtN+T8yRa5ZmYUTzCBzAYJKoZIhvcNAQcBMBQGCCqGSIb3DQMHBAj3/gDO9Af1J4CBqOFSI4FKmu0gulZaaBp+uRwoLAAaNN0R981CItIkhuhFs4BsqK+YyjB7zZDwQnM8dI6R4V62Ys/ToiOVXYlvMP0OkfDmiHOqlWeIhTThSXc9/3uxZW/A1c32FJZ3DGD7SbUdJHcTIbTsXRl88SGY8mMPgdAwlDhMvphqXjMbf6YQZYVGJrn8xRHDk5FMRjIeOHBV2VYUHGGz+pStzjaQ+hETr3fihyZUig==";

                var envelope = new EnvelopedCms();
                envelope.Decode(Convert.FromBase64String(encrypted64));
                envelope.Decrypt();

            }
            catch (Exception ex)
            {
                host.TrackException(ex);  
            }


            try
            {
                using (var container = new UnityContainer().AsFabricContainer())
                {

                    if (!string.IsNullOrEmpty(appInsight))
                    {
                        container.RegisterInstance(CreateTelemetryClientFromInstrumentationkey(appInsight));
                    }
                    container.RegisterInstance(FabricRuntime.GetActivationContext().GetConfigurationPackageObject("config").GetClusterConfiguraiton());
                    container.RegisterType<CloudStorageAccount>("ApplicationStorage", new ContainerControlledLifetimeManager(), new InjectionFactory(ApplicationCloudStorageAccountFactory));
                    container.RegisterType<IMessageProcessorClientFactory, DummyFactory>(new HierarchicalLifetimeManager());
                    container.RegisterType<IMessageClusterConfigurationStore, BlobStorageClusterStore>(new HierarchicalLifetimeManager(), new InjectionFactory(BlobContainerFactory));
                    container.WithActor<MessageClusterActor>();
                    container.WithStatelessService<ManagementApiService>(ManagementApiService.ServiceType);
                
                    container.WithActor<VmssManagerActor>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(120, 60)
                    });
                    container.WithActor<QueueManagerActor>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(120, 60)
                    });
                    container.WithActor<DispatcherManagerActor>(new ActorServiceSettings()
                    {
                        ActorGarbageCollectionSettings = new ActorGarbageCollectionSettings(120, 60)
                    });
                    container.WithActor<TopicManagerActor>(new ActorServiceSettings()
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

        private static CloudStorageAccount ApplicationCloudStorageAccountFactory(IUnityContainer arg)
        {
            var configuration = arg.Resolve<ServiceFabricClusterConfiguration>();

            var client = new ArmClient(configuration.GetAccessToken().GetAwaiter().GetResult());
            var keys = client.ListKeysAsync<JObject>($"/subscriptions/{configuration.SubscriptionId}/resourceGroups/{configuration.ResourceGroupName}/providers/Microsoft.Storage/storageAccounts/{configuration.StorageName}", "2016-01-01").GetAwaiter().GetResult();

            var account = new CloudStorageAccount(new StorageCredentials(configuration.StorageName, keys.SelectToken("keys[0].value").ToString()),true);
            account.CreateCloudBlobClient().GetContainerReference("clusters").CreateIfNotExists();
            return account;
        }

        private static IMessageClusterConfigurationStore BlobContainerFactory(IUnityContainer arg)
        {

            var account = arg.Resolve<CloudStorageAccount>("ApplicationStorage");
            var container = account.CreateCloudBlobClient().GetContainerReference("clusters");

            return new BlobStorageClusterStore(container);


        }
    }
   
}
