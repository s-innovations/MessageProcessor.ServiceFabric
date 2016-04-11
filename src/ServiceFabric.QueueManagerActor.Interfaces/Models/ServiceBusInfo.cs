using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{
    public class ServiceBusInfo
    {
        public string ServicebusNamespaceId { get; set; }
        public string AuthRuleResourceId { get; set; }
    }
}
