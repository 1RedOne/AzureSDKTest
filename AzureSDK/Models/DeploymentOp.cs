namespace AzureSDK.Models
{
    public class DeploymentOp
    {
        public string ProvisioningState { get; set; }
        public string StatusMessage { get; set; }
        public string Id { get; set; }
        public string ResourceName { get; set; }
        public string ResourceType { get; set; }
    }
}