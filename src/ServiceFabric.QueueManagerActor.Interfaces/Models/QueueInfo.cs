using System;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{

    [VariableReplacable]
    public class QueueInfo
    {
        public string AutoDeleteOnIdle { get; set; }
        public string DefaultMessageTimeToLive { get; set; }
        public string DuplicateDetectionHistoryTimeWindow { get; set; }
        public bool EnableBatchedOperations { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public bool EnableExpress { get; set; }
        public bool EnablePartitioning { get; set; }
        public string ForwardDeadLetteredMessagesTo { get; set; }
        public string ForwardTo { get; set; }
    }
}