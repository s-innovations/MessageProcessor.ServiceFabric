using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Models
{
    public class MessageProcessorOptions
    {
        public int ConcurrentMessagesProcesses { get; set; }
        public string ConnectionString { get; set; }
        public string ListenerConnectionString { get; set; }
        public string DispatcherName { get; set; }
        public string QueuePath { get; set; }
        public int TopicScaleCount { get; set; }
    }
}
