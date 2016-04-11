using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services
{
    public interface IMessageClusterConfigurationStore
    {
        Task<MessageClusterResource> GetMessageClusterAsync(string clusterKey);
        Task<MessageClusterResourceBase> GetMessageClusterResourceAsync(string clusterKey);
    }
}
