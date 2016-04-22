//using System;
//using System.Collections.Generic;
//using System.Fabric;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.ServiceFabric.Actors;
//using Microsoft.ServiceFabric.Actors.Runtime;
//using Microsoft.ServiceFabric.Services.Communication.Runtime;
//using Microsoft.ServiceFabric.Services.Runtime;
//using SInnovations.Azure.MessageProcessor.Core;
//using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
//using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
//using SInnovations.Azure.MessageProcessor.ServiceFabric.Tracing;

//namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Actors
//{

//    public class QueueListenerService : StatelessService
//    {
//        public const string ServiceType = "QueueListenerServiceType";
//        private IMessageProcessorClientFactory _messageProcessorFactory;
//        private IMessageProcessorClient _messageProcessor;

//        public QueueListenerService(IMessageProcessorClientFactory messageProcessorFactory, StatelessServiceContext serviceContext) : base(serviceContext)
//        {
//            _messageProcessorFactory = messageProcessorFactory;
//        }

//        protected async override Task RunAsync(CancellationToken cancellationToken)
//        {
           
//            _messageProcessor = await _messageProcessorFactory.CreateMessageProcessorAsync("");
//            await _messageProcessor.StartProcessorAsync();

//            await base.RunAsync(cancellationToken);
//        }
//        protected override Task OnOpenAsync(CancellationToken cancellationToken)
//        {
//            return base.OnOpenAsync(cancellationToken);
//        }
//        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
//        {
//            return base.CreateServiceInstanceListeners();
//        }
//        protected override void OnAbort()
//        {
//            base.OnAbort();
//        }
//        protected override async Task OnCloseAsync(CancellationToken cancellationToken)
//        {
//            await _messageProcessor.StopProcessorAsync();
//            _messageProcessor.Dispose();
//            _messageProcessor = null;
//            await base.OnCloseAsync(cancellationToken);
//        }
//    }

//    [StatePersistence(StatePersistence.None)]
//    public class QueueListenerActor : Actor, IQueueListenerActor
//    {
//        private IMessageProcessorClientFactory _messageProcessorFactory;
//        private IMessageProcessorClient _messageProcessor;

//        public QueueListenerActor(IMessageProcessorClientFactory messageProcessorFactory)
//        {
//            _messageProcessorFactory = messageProcessorFactory;
        
//        }

//        public async Task EnsureListeningAsync()
//        {
//            ServiceFabricEventSource.Current.ActorMessage(this, "EnsureListening Start");
//            if (_messageProcessor == null)
//            {
//                ServiceFabricEventSource.Current.ActorMessage(this, "Creating MessageProcessor");
//                _messageProcessor = await _messageProcessorFactory.CreateMessageProcessorAsync(this.Id.GetStringId());
//                await _messageProcessor.StartProcessorAsync();
//            }

//            ServiceFabricEventSource.Current.ActorMessage(this, "EnsureListening Done : {0}", ServiceUri);
//        }

//        protected async override Task OnActivateAsync()
//        {

//            await _messageProcessor.StartProcessorAsync();

//        }

//        protected async override Task OnDeactivateAsync()
//        {

//            await _messageProcessor.StopProcessorAsync();
           
//        }
//    }
//}
