using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights; 
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.OperationalInsights;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LogAnalytics.Client;
using Microsoft.Azure.Management.OperationalInsights.Models;

namespace AzureSDK.Models
{
    public class ResourceManagementRepo
    {
        private string subscribeTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "SubscriptionTemplate.json");
        private AzureLoginFields logininfo;
        private JsonSerializerSettings loopHandler = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        };

        public ResourceManagementRepo()
        {
            logininfo = GetLoginInfo();
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
            var t = this.GetAtSubscriptionScope("my123", waitForCompletion:true);

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
            var deploymentExtended  = rmc.Deployments.GetAtSubscriptionScope(name);
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
        public async Task<DeploymentOperationError> getErrors(string rgName, string deploymentName)
        {
            var client = getResourceManagementClient();
            var t = await client.DeploymentOperations.ListAtScopeAsync("resourceGroup", rgName);
            var errors = t.Where(x => x.Properties.ProvisioningState == "Failed");

            var deploymentOperations = client.DeploymentOperations.Get(rgName, deploymentName, null);
            var err = new DeploymentOperationError(deploymentOperations);
            return err;
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

        private LogAnalyticsClient GetLogAnalyticsClient()
        {
            LogAnalyticsClient logger = new LogAnalyticsClient(
                workspaceId: logininfo.OmsWorkSpaceID,
                sharedKey: logininfo.OmsSharedKey);

            return logger;
        }
    }
}
