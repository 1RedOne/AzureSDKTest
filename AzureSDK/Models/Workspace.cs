
namespace AzureSDK.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class Workspace : IEquatable<Workspace>
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        [JsonProperty(PropertyName = "subscriptionid")]
        public string SubscriptionId { get; set; }

        [JsonConstructor]
        public Workspace(string name, string location, string resourceGroup, string subscriptionId)
        {
            this.Name = name;
            this.Location = location;
            this.ResourceGroup = resourceGroup;
            this.SubscriptionId = subscriptionId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Workspace);
        }

        public bool Equals(Workspace other)
        {
            return other != null &&
                   this.Name == other.Name &&
                   this.Location == other.Location &&
                   this.ResourceGroup == other.ResourceGroup &&
                   this.SubscriptionId == other.SubscriptionId;
        }

        public override int GetHashCode() => HashCode.Combine(Name, Location, ResourceGroup, SubscriptionId);

        public static bool operator ==(Workspace left, Workspace right) => EqualityComparer<Workspace>.Default.Equals(left, right);

        public static bool operator !=(Workspace left, Workspace right) => !(left == right);


        public Resource ToResource()
        {
            return new Resource
            {
                Name = this.Name,
                RegionName = this.Location,
                ResourceGroupName = this.ResourceGroup,
                SubscriptionId = this.SubscriptionId
            };
        }

        public override string ToString() => $"Name: {Name}. Location: {Location}. ResourceGroup: {ResourceGroup}. SubscriptionId: {SubscriptionId}.";
    }

    public class Resource
    {
        public string Name { get; set; }

        public string RegionName { get; set; }

        public string ResourceGroupName { get; set; }

        public string SubscriptionId { get; set; }

    }
}
