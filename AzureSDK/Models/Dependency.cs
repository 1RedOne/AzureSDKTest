namespace AzureSDK.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Newtonsoft.Json;

    public class Dependency
    {
        //
        // Summary:
        //     Gets or sets the list of dependencies.
        [JsonProperty(PropertyName = "dependsOn")]
        public IList<BasicDependency> DependsOn { get; set; }

        //
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

        //POCO Class to represent an ARM Deployments possible Dependency object

        //
        // Summary:
        //     Initializes a new instance of the Dependency class.
        public Dependency(string Id, string ResourceType, string ResourceName)
        {
            this.Id = Id;
            this.ResourceName = ResourceName;
            this.ResourceType = ResourceType;
        }

        //
        // Summary:
        //     Initializes a new instance of the Dependency class.
        //
        // Parameters:
        //   dependsOn:
        //     The list of dependencies.
        //
        //   id:
        //     The ID of the dependency.
        //
        //   resourceType:
        //     The dependency resource type.
        //
        //   resourceName:
        //     The dependency resource name.
        public Dependency(IList<BasicDependency> dependsOn = null, string id = null, string resourceType = null, string resourceName = null)
        {
            this.DependsOn = dependsOn;
            this.Id = id;
            this.ResourceType = resourceType;
            this.ResourceName = resourceName;
        }
    }
}