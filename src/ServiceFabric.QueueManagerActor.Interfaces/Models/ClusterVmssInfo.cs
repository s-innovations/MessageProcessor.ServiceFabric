using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models
{
    public class ClusterVmssInfo
    {
        public string Name { get; set; }
        public string Tier { get; set; }
        public string VmImagePublisher { get; set; }
        public string VmImageOffer { get; set; }
        public string VmImageSku { get; set; }
        public string VmImageVersion { get; set; }
        public string Location { get; set; } = "West Europe";

        public int MinCapacity { get; set; } = 0;
        public int MaxCapacity { get; set; } = 40;
        public string ScaleDownCooldown { get; set; } = "PT10M";
        public string ScaleUpCooldown { get; set; } = "PT10M";
        public int MessagesPerInstance { get; set; } = 0;

    }


}
