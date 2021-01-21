using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureSDK.Models
{
    public class Assignment
    {

        [JsonProperty(PropertyName = "resourceIdentity")]
        public ResourceIdentity ResourceIdentity { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "target")]
        public string Target { get; set; }

        [JsonProperty(PropertyName = "profile")]
        public string Profile { get; set; }

        [JsonProperty(PropertyName = "preferences")]
        public string Preferences { get; set; }

        [JsonProperty(PropertyName = "accountId")]
        public string AccountId { get; set; }

        [JsonProperty(PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }

        

        public Assignment DeepCopy()
        {
            return new Assignment
            {
                ResourceIdentity = this.ResourceIdentity,
                Id = this.Id,
                Name = this.Name,
                Location = this.Location,
                Target = this.Target,
                Profile = this.Profile,
                Preferences = this.Preferences,
                AccountId = this.AccountId,
                ProvisioningState = this.ProvisioningState
            };
        }

    }

    public class ResourceIdentity
    {
        // Note: Cosmos SDK support doesn't using [JsonObject(NamingStrategyType = typeof(LowerCaseNamingStrategy))] at the class level.
        // Using this would cause LINQ queries to fail. Using the CosmosSerializationOptions when creating the Client also turned
        // out to be problematic since it causes dictionary keys to also be camel cased, which breaks case preserving. So, we
        // need to use JsonProperty on each property to control serialization.

        [JsonProperty(PropertyName = "subscription")]
        public Guid Subscription { get; set; }

        [JsonProperty(PropertyName = "resourceGroup")]
        [Required(AllowEmptyStrings = false)]
        public string ResourceGroup { get; set; }

        [JsonProperty(PropertyName = "name")]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "providerNamespace")]
        public string ProviderNamespace { get; set; }

        [JsonProperty(PropertyName = "resourceType")]
        public string ResourceType { get; set; }

        [JsonProperty(PropertyName = "extensionResourceType")]
        public string ParentResourceType { get; set; }

        [JsonProperty(PropertyName = "extensionResourceName")]
        public string ParentResourceName { get; set; }

        [JsonProperty(PropertyName = "extensionProviderNamespace")]
        public string ParentProviderNamespace { get; set; }

        public override string ToString()
        {
            const string rootStringTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/{2}";
            const string providerStringTemplate = "/providers/{0}";
            var builder = new StringBuilder(1024);

            if (!string.IsNullOrEmpty(this.ParentProviderNamespace)
                && !string.IsNullOrEmpty(this.ParentResourceType)
                && !string.IsNullOrEmpty(this.ParentResourceName))
            {
                builder.AppendFormat(rootStringTemplate, this.Subscription, this.ResourceGroup, this.ParentProviderNamespace);
                AppendTypeAndName(builder, this.ParentResourceType, this.ParentResourceName);

                builder.AppendFormat(providerStringTemplate, this.ProviderNamespace);
                AppendTypeAndName(builder, this.ResourceType, this.Name);

                return builder.ToString();
            }

            builder.AppendFormat(rootStringTemplate, this.Subscription, this.ResourceGroup, this.ProviderNamespace);
            AppendTypeAndName(builder, this.ResourceType, this.Name);

            return builder.ToString();
        }

        public string GetId()
        {
            return this.ToString().ToLower().Replace("/", "__");
        }

        public string GetFullyQualifiedResourceType()
        {
            return this.ProviderNamespace + "/" + this.ResourceType;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public bool Equals(ResourceIdentity other)
        {
            return !(other is null)
                && this.Subscription == other.Subscription
                && this.ResourceGroup == other.ResourceGroup
                && this.Name == other.Name
                && this.ProviderNamespace == other.ProviderNamespace
                && this.ResourceType == other.ResourceType
                && this.ParentResourceType == other.ParentResourceType
                && this.ParentResourceName == other.ParentResourceName
                && this.ParentProviderNamespace == other.ParentProviderNamespace;
        }

        public override bool Equals(object other)
        {
            return other is ResourceIdentity otherResourceIdentity && this.Equals(otherResourceIdentity);
        }

        public static bool operator ==(ResourceIdentity first, ResourceIdentity second)
        {
            return first is null ? second is null : first.Equals(second);
        }

        public static bool operator !=(ResourceIdentity first, ResourceIdentity second)
        {
            return !(first == second);
        }

        private static void AppendTypeAndName(StringBuilder builder, string resourceType, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceType) || string.IsNullOrEmpty(resourceName))
            {
                return;
            }

            var typeSegments = resourceType.Split('/');
            var nameSegments = resourceName.Split('/');

            for (var i = 0; i < typeSegments.Length; i++)
            {
                builder.AppendFormat("/{0}", typeSegments[i]);

                if (i < nameSegments.Length)
                {
                    builder.AppendFormat("/{0}", nameSegments[i]);
                }
            }
        }
    }
}
