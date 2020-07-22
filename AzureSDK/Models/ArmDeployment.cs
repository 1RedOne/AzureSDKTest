namespace AzureSDK.Models
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class ArmDeployment
    {
        // a POCO class to return the bare basics of an Azure Fluent IDeployments operation

        //
        // Returns:
        //     the correlation ID of the deployment
        public string DeploymentName { get; }

        //
        // Returns:
        //     the correlation ID of the deployment
        public string CorrelationId { get; }

        //
        // Returns:
        //     the state of the provisioning process of the resources being deployed
        public string ProvisioningState { get; }

        //
        // Returns:
        //     the name of this deployment's resource group
        public string ResourceGroupName { get; }

        //
        // Returns:
        //     the timestamp of the template deployment
        public DateTime? Timestamp { get; }

        public Object Template { get; }

        public List<DeploymentOp> DeploymentOps { get; set; }

        public ArmDeployment(string CorrelationId, DateTime? TimeStamp, string ProvisioningState, string ResourceGroupName, string DeploymentName, IDeployment deployment)
        {
            if (null == DeploymentOps) { this.DeploymentOps = new List<DeploymentOp>(); }
            this.CorrelationId = CorrelationId;
            this.ProvisioningState = ProvisioningState;
            this.Timestamp = TimeStamp;
            this.ResourceGroupName = ResourceGroupName;
            this.DeploymentName = DeploymentName;
            var allDeployments = deployment.DeploymentOperations.List();
            this.Template = deployment.Template;
            foreach (var h in allDeployments)
            {
                var thisDeployment = new DeploymentOp()
                {
                    ProvisioningState = h.ProvisioningState,
                    StatusMessage = h.StatusCode,
                    Id = h.TargetResource?.Id,
                    ResourceName = h.TargetResource?.ResourceName,
                    ResourceType = h.TargetResource?.ResourceType
                };
                this.DeploymentOps.Add(thisDeployment);
            }
        }

        public ArmDeployment(string CorrelationId, DateTime? TimeStamp, string ProvisioningState, string ResourceGroupName, string DeploymentName, Microsoft.Azure.Management.ResourceManager.Models.DeploymentExtended deployment)
        {
            if (null == DeploymentOps) { this.DeploymentOps = new List<DeploymentOp>(); }
            this.CorrelationId = CorrelationId;
            this.ProvisioningState = ProvisioningState;
            this.Timestamp = TimeStamp;
            this.ResourceGroupName = ResourceGroupName;
            this.DeploymentName = DeploymentName;
            var allDeployments = deployment.Properties.OutputResources;
            this.Template = deployment.Properties.TemplateHash;
            foreach (var h in allDeployments)
            {
                var thisDeployment = new DeploymentOp()
                {
                    ProvisioningState = null,
                    StatusMessage = null,
                    Id = h.Id,
                    ResourceName = null,
                    ResourceType = null
                };
                this.DeploymentOps.Add(thisDeployment);
            }
        }
    }

   
}