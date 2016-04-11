using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SInnovations.Azure.MessageProcessor.Core;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services
{
    public interface IMessageProcessorClientFactory
    {
        Task<IMessageProcessorClient> CreateMessageProcessorAsync(string key);
    }
}
