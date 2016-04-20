using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors
{
    public interface IVmssManagerActor : IActor
    {
        Task<bool> CreateIfNotExistsAsync();
        Task<bool> RemoveIfNotRemovedAsync();
        Task ReportQueueMessageCountAsync(string queueActorId,long count, int additinalNodesAvaible);
       // Task<bool> SetCapacityAsync(int capacity);
       // Task<int> GetCapacityAsync();
    }
}
