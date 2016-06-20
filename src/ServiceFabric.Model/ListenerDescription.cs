namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Models
{
    public class ListenerDescription
    {
        public int PartitionCount { get; set; } = 1;
        public string IdleTimeout { get; set; } = "PT60M";

        public string ApplicationTypeName { get; set; }
        public string ApplicationTypeVersion { get; set; }
        public string ServiceTypeName {get;set;}
        public string ProcessorNode { get; set; }
        public bool UsePrimaryNode { get; set; }

        public bool AlwaysOn { get; set; }
    }
}