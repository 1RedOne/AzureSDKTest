using Microsoft.Azure.Management.OperationalInsights;
using Microsoft.Azure.Management.OperationalInsights.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using QueryRequest = Microsoft.Azure.Management.ResourceGraph.Models.QueryRequest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Rest;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Extensions.Configuration;

namespace AzureSDK.Models
{
    public class OmsClient
    {
        private AzureLoginFields logininfo;
        private string ResourceManagerEndpoint = "https://management.azure.com/";
        private string ASCApiVersion = "2017-08-01-preview";
        private AzureDeploymentClient azureClient;
        private ResourceManagementRepo rmc;
        private readonly HttpClient client;
        private TokenHolder token;

        public OmsClient(IConfiguration configuration)
        {
            logininfo = new AzureLoginFields(configuration);
            azureClient = new AzureDeploymentClient(configuration);
            rmc = new ResourceManagementRepo(configuration);
            client = new HttpClient();
            token = new TokenHolder(configuration);
        }

        public async Task<Workspace> GetNewDefaultOrExistingWorkspace(Assignment assignment, Workspace defaultWorkspace, string token,  bool isDriftEvent = false)
        {
            _ = assignment ?? throw new ArgumentNullException(nameof(assignment));
            _ = defaultWorkspace ?? throw new ArgumentNullException(nameof(defaultWorkspace));

            var existingWorkspaceId = await GetLaWorkspaceId(token, defaultWorkspace.ToResource());

            //This is the most basic case for Arc machines. This should work if the machine is new, further implementation is required
            //to allow for workspace verification of arc machines
            var existingVMWorkspaceId = "";

            // green field
            if (existingVMWorkspaceId == existingWorkspaceId || string.IsNullOrEmpty(existingVMWorkspaceId))
            {
                return defaultWorkspace;
            }

            try // see if ASC has custom workspace, if yes whether it is resolvable (within the same subscription)
            {
                var ascCustomWorkspaceResourceId = await GetASCCustomWorkspaceResourceId(token);
                if (!string.IsNullOrEmpty(ascCustomWorkspaceResourceId))
                {
                    var ascWorkspaceResource = await GetResourceByIdAsync(token, ascCustomWorkspaceResourceId);
                    var ascCustomWorkspaceId = await GetLaWorkspaceId(token, ascWorkspaceResource);

                    if (existingVMWorkspaceId == ascCustomWorkspaceId)
                    {
                        return new Workspace(ascWorkspaceResource.Name, ascWorkspaceResource.RegionName, ascWorkspaceResource.ResourceGroupName, ascWorkspaceResource.SubscriptionId);
                    }
                }

                // if ASC don't have custom workspace, or ASC workspace doesn't match existing VM workspace.
                // Try to resolve VM workspace if it is possible. If drift, then do not throw
                var existingVMWorkspaceResource = await GetResourceFromWorkspaceId(token, assignment, existingVMWorkspaceId, !isDriftEvent);

                //If workspace is deleted but MMA extension is still present on VM, we should use the new default workspace
                if (existingVMWorkspaceResource is null)
                {
                    return defaultWorkspace;
                }

                return new Workspace(
                    existingVMWorkspaceResource.Name,
                    existingVMWorkspaceResource.RegionName,
                    existingVMWorkspaceResource.ResourceGroupName,
                    existingVMWorkspaceResource.SubscriptionId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Custom workspace from VM or ASC that are not accessible from current subscription. Exception: {ex.Message}");
            }
        }

        public async Task<Workspace> GetDefaultWorkspace(Assignment assignment)
        {
            _ = assignment ?? throw new ArgumentNullException(nameof(assignment));

            var regionName = await this.GetTargetRegionName(assignment);
            var logAnalyticsLocation = this.GetLogAnalyticsServiceLocation(regionName, requireSameRegion: false);
            var subscriptionId = assignment.ResourceIdentity.Subscription.ToString();

            var defaultWorkspaceName = $"DefaultWorkspace-{subscriptionId}-{logAnalyticsLocation.LocationCode}";
            var defaultResourceGroup = $"DefaultResourceGroup-{logAnalyticsLocation.LocationCode}";

            return new Workspace(defaultWorkspaceName, logAnalyticsLocation.Location, defaultResourceGroup, subscriptionId);
        }

        private async Task<string> GetTargetRegionName(Assignment assignment)
           => (await this.GetTargetResource(assignment)).RegionName;

        public async Task<Resource> GetTargetResource(Assignment assignment)
        {
            _ = assignment ?? throw new ArgumentNullException(nameof(assignment));

            Resource targetResource;
            try
            {
                var token = this.GetAccessToken(assignment);
                
                targetResource = await rmc.GetTargetResource(token, assignment);
            }
            catch (Exception exception)
            {
                throw exception;
            }            

            return targetResource;
        }

        private string GetAccessToken(Assignment assignment)
        {
            return token.token;
        }

        public async Task<Resource> GetResourceFromWorkspaceId(string token, Assignment assignment, string workspaceId, bool throwIfNotExist = true)
        {
            
            var strQuery = $"Resources | where type has 'Microsoft.OperationalInsights/workspaces' | where properties.customerId == '{workspaceId}' | project id";
            var availableSubscriptions = GetAvailableSubscriptions(token);
            var resourceGraphClientResponse = CallResourceGraphClient(token, strQuery, availableSubscriptions);
            if (resourceGraphClientResponse == null)
            {
                throw new Exception($"GetResourceFromWorkspaceId not able to resolve the workspaceArmId for {workspaceId} becuase is getting null response from CallResourceGraphClient");
            }
            try
            {
                string workspaceArmId = resourceGraphClientResponse.rows[0][0];
                //If it is a drift event do not throw
                return await this.GetResourceByIdAsync(token, workspaceArmId, throwIfNotExist);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<string> GetASCCustomWorkspaceResourceId(string token)
        {

            var subscriptionId = logininfo.SubscriptionId; // assignment.ResourceIdentity.Subscription.ToString();

            // if autoprovision is off, we do not care about the custom setting}
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ResourceManagerEndpoint}/subscriptions/{subscriptionId}/providers/Microsoft.Security/autoProvisioningSettings/default?api-version={ASCApiVersion}");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var clientResponse = await this.client.SendAsync(request);

            if (clientResponse.StatusCode == HttpStatusCode.OK)
            {
                var clientResponseStream = await clientResponse.Content.ReadAsStreamAsync();
                var ascResponse = await System.Text.Json.JsonSerializer.DeserializeAsync<ASCResponse>(clientResponseStream);

                if (ascResponse?.properties?.autoProvision == "On")
                {
                    request = new HttpRequestMessage(HttpMethod.Get, $"{ResourceManagerEndpoint}/subscriptions/{subscriptionId}/providers/Microsoft.Security/workspaceSettings/default?api-version={ASCApiVersion}");
                    request.Headers.Add("Authorization", $"Bearer {token}");

                    clientResponse = await this.client.SendAsync(request);

                    if (clientResponse.StatusCode == HttpStatusCode.OK)
                    {
                        clientResponseStream = await clientResponse.Content.ReadAsStreamAsync();

                        var ascWorkspaceSetting = await System.Text.Json.JsonSerializer.DeserializeAsync<ASCResponse>(clientResponseStream);
                        
                        return ascWorkspaceSetting?.properties?.workspaceId;
                    }
                }
            }
            else
            {
            }

            return null;
        }

        public async Task<string> GetLaWorkspaceId(string token, Resource workspace)
        {
            var client = getOmsClient();
            try
            {
                var result = await client.Workspaces.GetAsync(workspace.ResourceGroupName, workspace.Name);
                return result?.CustomerId;
            }
            catch
            {
                return null;
            }
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

        public Resource ParseResourceId(string resourceId)
        {
            var resource = ResourceId.FromString(resourceId);

            return new Resource
            {
                Name = resource.Name,
                ResourceGroupName = resource.ResourceGroupName,
                SubscriptionId = resource.SubscriptionId
            };
        }

        public async Task<Resource> GetResourceByIdAsync(string token, string resourceId, bool throwIfNotExist = true)
        {
            try
            {
                
                var parseResource = this.ParseResourceId(resourceId);
                return await GetResourceById(resourceId);
                
            }
            
            catch (Exception ex)
            {
                // roslyn analysis isn't able to distinguish the filters, but this is only reached
                // when (throwIfNotExist == false && ex.Response.StatusCode == HttpStatusCode.NotFound)
                return null;
            }

            
        }

        public ResourceGraphClientResponse CallResourceGraphClient(string token, string strQuery, List<string> subscriptions)
        {
            
            try
            {
                QueryRequest request = new QueryRequest();
                request.Query = strQuery;
                request.Subscriptions = subscriptions;
                ServiceClientCredentials serviceClientCreds = new TokenCredentials(token);
                ResourceGraphClient argClient = new ResourceGraphClient(serviceClientCreds);
                Microsoft.Azure.Management.ResourceGraph.Models.QueryResponse response = argClient.Resources(request);
                var resourceGraphClientResponse = Newtonsoft.Json.Linq.JObject.Parse(response.Data.ToString()).ToObject<ResourceGraphClientResponse>();
                return resourceGraphClientResponse;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private List<string> GetAvailableSubscriptions(string token)
        {            
            var subList = azureClient.GetAvailableSubscriptions();

            return subList;
        }

        private async Task<Resource> GetResourceById(string resourceId)
        {            
            return await rmc.GetResourceByIdAsync(null, resourceId);
        }


        private AzureLoginFields GetLoginInfo()
        {
            string fileName = Path.Combine(Directory.GetCurrentDirectory(), "Models", "LoginInfo.json");
            string jsonString = System.IO.File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<AzureLoginFields>(jsonString);
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

    }

    public class ASCResponse
    {
        public Properties properties { get; set; }

        public class Properties
        {
            public string workspaceId { get; set; }
            public string autoProvision { get; set; }
        }
    }

    public class ResourceGraphClientResponse
    {
        public class Column
        {
            public string name { get; set; }
            public string type { get; set; }
        }
        public List<Column> columns { get; set; }
        public List<List<string>> rows { get; set; }
    }
}
