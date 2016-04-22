using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors
{
    /// <summary>
    /// This interface represents the actions a client app can perform on an actor.
    /// It MUST derive from IActor and all methods MUST return a Task.
    /// </summary>
    public interface IMessageClusterActor : IActor
    {  
        Task<string> StartMonitoringAsync();
        Task<string> StopMonitoringAsync();
        Task<JsonModel<MessageClusterResource>> UpdateModelAsync(JsonModel<MessageClusterResource> model);
        Task<JsonModel<MessageClusterResource>> GetModelAsync();
    }
    public interface IMessageClusterActorProxy : IMessageClusterActor, IActorProxy
    {

    }
}
