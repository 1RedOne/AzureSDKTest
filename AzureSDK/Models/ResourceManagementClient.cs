using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.OperationalInsights;
using ResourceManager = Microsoft.Azure.Management.ResourceManager.Fluent.ResourceManager;
using LogAnalytics.Client;
using Microsoft.Azure.Management.OperationalInsights.Models;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AzureSDK.Models
{
    public class ResourceManagementRepo
    {
        private HttpClient client;
        private ILogger<ResourceManagementClient> logger = new Logger<ResourceManagementClient>(new LoggerFactory()); 
        private string subscribeTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "SubscriptionTemplate.json");
        private AzureLoginFields logininfo;
        private JsonSerializerSettings loopHandler = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        };

        public ResourceManagementRepo(IConfiguration configuration)
        {
            logininfo = new AzureLoginFields(configuration);
            this.client = new HttpClient();
        }

        private AzureLoginFields GetLoginInfo()
        {
            string fileName = Path.Combine(Directory.GetCurrentDirectory(), "Models", "LoginInfo.json");
            string jsonString = System.IO.File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<AzureLoginFields>(jsonString);
        }

        public async Task<IEnumerable<Microsoft.Azure.Management.OperationalInsights.Models.Workspace>> GetLogAnalyticsSavedSearch(string token, string searchName)
        {
            var credentials = Microsoft.Azure.Management.ResourceManager.Fluent.SdkContext.AzureCredentialsFactory.FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
                   Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.AzureGlobalCloud);
            //var subscriptionId = assignment.ResourceIdentity.Subscription.ToString();
            var client = new OperationalInsightsManagementClient(credentials)
            {
                SubscriptionId = logininfo.SubscriptionId
            };
            var w = await client.Workspaces.ListByResourceGroupAsync("myTestWorkspaceGroup");
            return w;
            //var r = client.SavedSearches.ListByWorkspace()
            //client.SavedSearches.GetAsync(assignment.ResourceIdentity.ResourceGroup, )

        }

        public async Task<Resource> GetResourceByIdAsync(string token, string resourceId, bool throwIfNotExist = true)
        {
            try
            {
                var resourceManagerAuthenticationClient = GetFluentResourceManagerClient();
                var resource = await resourceManagerAuthenticationClient.GenericResources.GetByIdAsync(resourceId);

                return new Resource
                {
                    Name = resource.Name,
                    RegionName = resource.RegionName,
                    ResourceGroupName = resource.ResourceGroupName,
                    SubscriptionId = logininfo.SubscriptionId
                };
            }
            catch
            {

                // roslyn analysis isn't able to distinguish the filters, but this is only reached
                // when (throwIfNotExist == false && ex.Response.StatusCode == HttpStatusCode.NotFound)
                return null;
            }

        }


        public async Task<bool> sendCustomEvent(string property)
        {
            var client = GetLogAnalyticsClient();
            try
            {
                await client.SendLogEntry(new CustomOmsRecord(property)
                , "AutoManagedVM")
                .ConfigureAwait(true);

            }
            catch
            {
                return false;
            }
            return true;
        }

        public async Task<DeploymentExtended> CreateSubscriptionDeployment()
        {
            string subscriptionParamPath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "SubscriptionTemplateParams.json");
            var credentials = Microsoft.Azure.Management.ResourceManager.Fluent.SdkContext.AzureCredentialsFactory.FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
                    Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.AzureGlobalCloud);

            var template = File.ReadAllText(subscribeTemplatePath);
            var r = new ResourceManagementClient(credentials);
            var jobj = JObject.Parse(File.ReadAllText(subscriptionParamPath));
            var deployment = new Deployment
            {
                Location = "eastus",
                Properties = new DeploymentProperties
                {
                    Mode = DeploymentMode.Incremental,
                    Template = template,//subscriptionTemplate,
                    Parameters = jobj
                }
            };
            r.SubscriptionId = logininfo.SubscriptionId;
            var h = await r.Deployments.BeginCreateOrUpdateAtSubscriptionScopeAsync("thankMichele", deployment);

            //return h;

            //r.Deployments.BeginCreateOrUpdateAtSubscriptionScopeAsync()

            //try to get results
            var t = this.GetAtSubscriptionScope("my123", waitForCompletion: true);

            return t;
        }

        public async Task<DeploymentExtended> CreateSubscriptionDeployment(string deploymentName, JObject template)
        {            
            var credentials = Microsoft.Azure.Management.ResourceManager.Fluent.SdkContext.AzureCredentialsFactory.FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
                    Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.AzureGlobalCloud);
            
            var r = new ResourceManagementClient(credentials);            
            var deployment = new Deployment
            {
                Location = "eastus",
                Properties = new DeploymentProperties
                {
                    Mode = DeploymentMode.Incremental,
                    Template = template,//subscriptionTemplate,
                    Parameters = "{}"
                }
            };
            r.SubscriptionId = logininfo.SubscriptionId;
            var h = await r.Deployments.BeginCreateOrUpdateAtSubscriptionScopeAsync(deploymentName, deployment);
         
            //try to get results
            var t = this.GetAtSubscriptionScope(deploymentName, waitForCompletion: true);

            return t;
        }

        public async Task<List<DeploymentExtended>> GetDeployments()
        {
            var client = getResourceManagementClient();
            var t = await client.Deployments.ListAtSubscriptionScopeAsync();
            return t.ToList();
        }

        public DeploymentExtended GetAtSubscriptionScope(string name, bool waitForCompletion = false)
        {


            var rmc = getResourceManagementClient();
            var deploymentExtended = rmc.Deployments.GetAtSubscriptionScope(name);
            if (waitForCompletion)
            {
                while (deploymentExtended.Properties.ProvisioningState == "Running")
                {
                    deploymentExtended = rmc.Deployments.GetAtSubscriptionScope(name);
                    Thread.Sleep(500);
                }
            }

            //var ops = r.DeploymentOperations.GetAtSubscriptionScope(name, deploymentExtended.Properties.CorrelationId);
            return deploymentExtended;
        }

        //private DeploymentOperation GetDeploymentOperationInfo(DeploymentExtended deployment)
        //{

        //}
        public async Task<List<DeploymentOperationError>> getErrors(string rgName, string deploymentName)
        {
            var client = getResourceManagementClient();
            var t = await client.DeploymentOperations.ListAtScopeAsync("resourceGroup", rgName);
            var errors = t.Where(x => x.Properties.ProvisioningState == "Failed");
            return errors.Select(x => new DeploymentOperationError(x)).ToList();
            //var deploymentOperations = client.DeploymentOperations.Get(rgName, deploymentName, null);
            //var err = new DeploymentOperationError(deploymentOperations);
            //return err;
        }

        public async Task<DeploymentOperationError> getErrors(string rgName, string deploymentName, string operation)
        {
            var client = getResourceManagementClient();
            var d = client.Deployments.Get("testingetag", "testingetag");
            var deploymentOperations = client.DeploymentOperations.Get(rgName, deploymentName, operation);
            var err = new DeploymentOperationError(deploymentOperations);
            return err;
        }

        public async Task<DeploymentExtended> GetDeployment(string rgName, string deploymentName)
        {
            var client = getResourceManagementClient();
            var d = client.Deployments.Get(rgName, deploymentName);
            return d;
        }

        public async Task<DeploymentOperation> GetDeploymentOperations(string rgName, string deploymentName, string operation)
        {
            var client = getResourceManagementClient();
            var k = client.DeploymentOperations.Get(rgName, deploymentName, operation);
            //var d = client.Deployments.Get(rgName, deploymentName);
            return k;
        }

        public async Task<SavedSearch> GetSavedSearchFromWorkspace(string token, WorkspaceResource workspace, string savedSearchAlias)
        {
            try
            {
                
                string ResourceManagerEndpoint = "https://management.azure.com/";
                var endpoint = $"{ResourceManagerEndpoint}/subscriptions/{workspace.SubscriptionId}/resourcegroups/{workspace.ResourceGroupName}/providers/Microsoft.OperationalInsights/workspaces/{workspace.Name}/savedSearches?api-version=2020-08-01";
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("Authorization", $"Bearer {token}");

                var clientResponse = await this.client.SendAsync(request);
                if (clientResponse.IsSuccessStatusCode)
                {
                    var clientResponseStream = await clientResponse.Content.ReadAsStreamAsync();
                    var solutionResponse = await System.Text.Json.JsonSerializer.DeserializeAsync<SavedSearchesResult>(clientResponseStream,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    var savedSearch = solutionResponse.Value.Where(o => o.properties.FunctionAlias == savedSearchAlias).ToList();
                    if (savedSearch != null)
                    {
                        return savedSearch[0];
                    }
                    return null;
                }
                else
                {
                    this.logger?.LogTrace("Not found the saved search {savedSearchAlias} configured on workspace {workspaceName}", savedSearchAlias, workspace.Name);
                    return null;
                }
            }
            catch (Exception ex)
            {
                this.logger?.LogTrace(ex, "Error when trying to find the saved search {savedSearchAlias} on the workspace {workspaceName}", savedSearchAlias, workspace.Name);
                return null;
            }
        }

        public List<Workspace> getOmsWorkspaces()
        {
            var client = getOmsClient();
            var list = client.Workspaces.List();
            return (List<Workspace>)list;

        }

        private OperationalInsightsManagementClient getOmsClient()
        {
            var credentials = Microsoft.Azure.Management.ResourceManager.Fluent.SdkContext.AzureCredentialsFactory.FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
                   Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.AzureGlobalCloud);
            //var subscriptionId = assignment.ResourceIdentity.Subscription.ToString();
            var client = new OperationalInsightsManagementClient(credentials)
            {
                SubscriptionId = logininfo.SubscriptionId
            };
            return client;
        }

        private IResourceManagementClient getResourceManagementClient()
        {

            var credentials = Microsoft.Azure.Management.ResourceManager.Fluent.SdkContext.AzureCredentialsFactory.FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
                Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.AzureGlobalCloud);

            var resourceClient = new ResourceManagementClient(credentials)
            {
                SubscriptionId = logininfo.SubscriptionId
            };
            return resourceClient;
        }

        private Microsoft.Azure.Management.ResourceManager.Fluent.IResourceManager GetFluentResourceManagerClient()
        {
            var credentials = Microsoft.Azure.Management.ResourceManager.Fluent.SdkContext.AzureCredentialsFactory.FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
               Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.AzureGlobalCloud);

            var resourceClient =  ResourceManager.Configure().Authenticate(credentials).WithSubscription(logininfo.SubscriptionId);
            return resourceClient;
        }

        private LogAnalyticsClient GetLogAnalyticsClient()
        {
            LogAnalyticsClient logger = new LogAnalyticsClient(
                workspaceId: logininfo.OmsWorkSpaceID,
                sharedKey: logininfo.OmsSharedKey);

            return logger;
        }

        public class WorkspaceResource {
            public string Name { get; set; }
            public string SubscriptionId { get; set; }
            public string ResourceGroupName { get; set; }
            public WorkspaceResource(string name, string sub, string resourceGroupName)
            {
                this.Name = name;
                this.SubscriptionId = sub;
                this.ResourceGroupName = resourceGroupName;
            }
        }

        public class SavedSearchesResult
        {
            [Newtonsoft.Json.JsonProperty(PropertyName = "__metadata")]
            public SearchMetadata Metadata { get; set; }
            [Newtonsoft.Json.JsonProperty(PropertyName = "value")]
            public IList<SavedSearch> Value { get; set; }
        }

        public class Properties
        {
            public string Category { get; set; }
            public string DisplayName { get; set; }
            public string Query { get; set; }
            public List<Tag> Tags { get; set; }
            public string FunctionAlias { get; set; }
            public int Version { get; set; }
        }

        public class SavedSearch
        {
            public string id { get; set; }
            public string etag { get; set; }
            public Properties properties { get; set; }
            public string name { get; set; }
            public string type { get; set; }
        }
    }
}
