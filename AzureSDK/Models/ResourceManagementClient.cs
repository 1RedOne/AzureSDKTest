using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    }
}
