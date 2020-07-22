namespace AzureSDK.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Newtonsoft.Json;

    public class BasicDependency
    {
        //POCO Class to represent an ARM Deployments possible BasicDependency object
        // Summary:
        //     Gets or sets the ID of the dependency.
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        //
        // Summary:
        //     Gets or sets the dependency resource type.
        [JsonProperty(PropertyName = "resourceType")]
        public string ResourceType { get; set; }

        //
        // Summary:
        //     Gets or sets the dependency resource name.
        [JsonProperty(PropertyName = "resourceName")]
        public string ResourceName { get; set; }

        public BasicDependency(string id = null, string resourceType = null, string resourceName = null)
        {
            this.Id = id;
            this.ResourceType = resourceType;
            this.ResourceName = resourceName;
        }
    }
}