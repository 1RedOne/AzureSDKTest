using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureSDKTests.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<ArmTemplateClass>(myJsonResponse); 
    public class WorkspacesTestingetagName
    {
        public string defaultValue { get; set; }
        public string type { get; set; }
    }

    public class Parameters
    {
        public WorkspacesTestingetagName workspaces_testingetag_name { get; set; }
    }

    public class Sku
    {
        public string name { get; set; }
    }

    public class WorkspaceCapping
    {
        public int dailyQuotaGb { get; set; }
    }

    public class Tag
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Properties
    {        
        public Sku sku { get; set; }
        public int retentionInDays { get; set; }
        public WorkspaceCapping workspaceCapping { get; set; }
        public string publicNetworkAccessForIngestion { get; set; }
        public string publicNetworkAccessForQuery { get; set; }
        public string Etag { get; set; }
        public string Category { get; set; }
        public string DisplayName { get; set; }
        public string Query { get; set; }
        public List<Tag> Tags { get; set; }
        public Template template { get; set; }
        public string FunctionAlias { get; set; }
        public int? Version { get; set; }
    }

    public class Resource
    {
        public string type { get; set; }
        public string apiVersion { get; set; }
        public string name { get; set; }
        public string location { get; set; }
        public Properties properties { get; set; }
        public List<string> dependsOn { get; set; }
    }

    public class ArmTemplateClass
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }
        public string contentVersion { get; set; }
        public Parameters parameters { get; set; }
        public Variables variables { get; set; }
        public List<Resource> resources { get; set; }
    }

    public class Template
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }
        public string contentVersion { get; set; }
        public List<Resource> resources { get; set; }
    }

    public class AnyOf
    {
        public string field { get; set; }
        public string notEquals { get; set; }
        public List<AllOf> allOf { get; set; }
    }

    public class AllOf
    {
        public string field { get; set; }
        public string equals { get; set; }
        public List<AnyOf> anyOf { get; set; }
    }

    public class If
    {
        public List<AllOf> allOf { get; set; }
        public List<AnyOf> anyOf { get; set; }
    }

    public class Then
    {
        public string effect { get; set; }
    }

    public class PolicyRule
    {
        public If @if { get; set; }
        public Then then { get; set; }
    }

    public class Variables
    {
        public string scopeConfigurationName { get; set; }
        public string configurationScopeKind { get; set; }
        public string configurationScopeInclude { get; set; }
        public string automationPolicyName { get; set; }
        public string automationPolicyDisplayName { get; set; }
        public string automationPolicyDescription { get; set; }
        public string workspacePolicyName { get; set; }
        public string workspacePolicyDisplayName { get; set; }
        public string workspacePolicyDescription { get; set; }
        public string solutionPolicyName { get; set; }
        public string solutionPolicyDisplayName { get; set; }
        public string solutionPolicyDescription { get; set; }
        public string workspacePolicyAssignment { get; set; }
        public string automationPolicyAssignment { get; set; }
        public string updatesSolutionPolicyAssignment { get; set; }
        public string changeTrackingSolutionPolicyAssignment { get; set; }
        public string VMInsightsSolutionPolicyAssignment { get; set; }
        public string prodProfileName { get; set; }
    }

    //NEW STUFF HERE 
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Profile
    {
        public string value { get; set; }
    }

    public class EnvironmentResourceGroup
    {
        public string value { get; set; }
    }

    public class EnvironmentResourceGroupLocation
    {
        public string value { get; set; }
    }

    public class Value
    {
        public string name { get; set; }
        public string location { get; set; }
        public string resourceGroup { get; set; }
        public string subscriptionid { get; set; }
        public string sku { get; set; }
        public string resourcegroup { get; set; }
    }

    public class Workspace
    {
        public Value value { get; set; }
    }

    public class McasIntegrationEnabled
    {
        public bool value { get; set; }
    }

    public class WdatpIntegrationEnabled
    {
        public bool value { get; set; }
    }

    public class AutomationAccount
    {
        public Value value { get; set; }
    }

    public class AutomationDeployName
    {
        public string value { get; set; }
    }

    public class DependenciesDeployName
    {
        public string value { get; set; }
    }

    public class PolicyAssignmentDeployName
    {
        public string value { get; set; }
    }

    public class AutomationPolicyAssignmentDeployName
    {
        public string value { get; set; }
    }

    
    public class DeploymentParameters
    {
        public Profile profile { get; set; }
        public EnvironmentResourceGroup environmentResourceGroup { get; set; }
        public EnvironmentResourceGroupLocation environmentResourceGroupLocation { get; set; }
        public Workspace workspace { get; set; }
        public McasIntegrationEnabled mcasIntegrationEnabled { get; set; }
        public WdatpIntegrationEnabled wdatpIntegrationEnabled { get; set; }
        public AutomationAccount automationAccount { get; set; }
        public AutomationDeployName automationDeployName { get; set; }
        public DependenciesDeployName dependenciesDeployName { get; set; }
        public PolicyAssignmentDeployName policyAssignmentDeployName { get; set; }
        public AutomationPolicyAssignmentDeployName automationPolicyAssignmentDeployName { get; set; }
    }

    public class Root
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }
        public string contentVersion { get; set; }
        public Parameters parameters { get; set; }
    }


}
