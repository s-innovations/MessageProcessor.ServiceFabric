using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{
    public class MessageClusterResource : MessageClusterResourceBase
    {
        public override string Type { get; } = MessageClusterType;
        public string ApiVersion { get; set; }
        public string Location { get; set; }

       // public IDictionary<string, JToken> Variables { get; set; }
        public MessageClusterResourceProperties Properties { get; set; }

        public MessageClusterResourceBase[] Resources { get; set; }

        public string ProvisioningState { get; set; }
    }
}
