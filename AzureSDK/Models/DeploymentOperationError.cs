using Microsoft.Azure.Management.ResourceManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureSDK.Models
{
    public class DeploymentOperationError
    {
        public DeploymentOperationError(string targetResource, string statusCode, object statusMessage)
        {
            this.TargetResource = targetResource;
            this.StatusCode = statusCode;
            this.StatusMessage = statusMessage;
        }

        public DeploymentOperationError(DeploymentOperation deploymentOperation)
        {
            this.TargetResource = deploymentOperation.Properties.TargetResource.Id;
            this.StatusCode = deploymentOperation.Properties.StatusCode;
            this.StatusMessage = deploymentOperation.Properties.StatusMessage;
        }
        public string TargetResource { get; }

        public string StatusCode { get; }

        public object StatusMessage { get; }
    }
}
