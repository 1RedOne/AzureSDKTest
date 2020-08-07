using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using DeploymentExtendedInner = Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentExtendedInner;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AzureSDK.Models
{
    public class AzureDeploymentClient
    {
        private AzureLoginFields logininfo = new AzureLoginFields();
        private string subscribeTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "SubscriptionTemplate.json");
        private string rgTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "Template.json");
        private string hassanTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "hassanTestTemplate.json");
        private string hassanParams = Path.Combine(Directory.GetCurrentDirectory(), "Models", "templateparams.json");

        private IAzure GetAzureClient()
        {
            logininfo = GetLoginInfo();
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
                    AzureEnvironment.AzureGlobalCloud);

            return Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(credentials)
                .WithDefaultSubscription();
        }

        private AzureLoginFields GetLoginInfo()
        {
            string fileName = Path.Combine(Directory.GetCurrentDirectory(), "Models", "LoginInfo.json");
            string jsonString = System.IO.File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<AzureLoginFields>(jsonString);
        }

        public async Task<IDeployment> NewResourceGroupDeployment()
        {
            var rgName = "sroTemplateTest2";
            //var template = System.IO.File.ReadAllText(rgTemplatePath);
            
            var template = System.IO.File.ReadAllText(hassanTemplatePath, Encoding.UTF8);
            var parms = System.IO.File.ReadAllText(hassanParams, Encoding.UTF8);
            var jobj = JObject.Parse(File.ReadAllText(hassanParams));
            var azure = GetAzureClient();
            var objectFromJson = JsonConvert.SerializeObject(jobj);
            
            var rg = azure.ResourceGroups
                         .Define(rgName)
                         .WithRegion("West US");
            
            
            //var rg = await resourcesManagementClient.ResourceGroups.CreateOrUpdateAsync("srofoxtestrg0716", new ResourceGroup("West US"));
            bool needToCreateRg;
            try { 
                azure.ResourceGroups.GetByName(rgName);
                needToCreateRg = true; //dont forget 
            }
            catch
            {
                needToCreateRg = true;
            }
            if (needToCreateRg)
            {
                //valudate               
                var deploymentCreated = await azure.Deployments.Define("hassantest2")
                    .WithNewResourceGroup(rg)
                    .WithTemplate(template).WithParameters(jobj)
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync(CancellationToken.None);
                return deploymentCreated;

            }
            else
            {
                return await azure.Deployments.Define("someTest12345")
                    .WithExistingResourceGroup(rg.Name)
                    .WithTemplate(template).WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync(CancellationToken.None);
            }
            
            
        }

        public async Task<DeploymentExtendedInner> NewSubscriptionDeployment()
        {
            var azure = GetAzureClient();
            var template = System.IO.File.ReadAllText(subscribeTemplatePath);
            

            Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentInner deploymentParam = new Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentInner()
            {
                Location = "westus",
                Properties = new Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentProperties()
                {
                    Mode = Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental,
                    Template = Newtonsoft.Json.JsonConvert.DeserializeObject(template),                    
                    Parameters = Newtonsoft.Json.JsonConvert.DeserializeObject("{}")
                }
            };

            var deployment = azure.Deployments.Inner.BeginCreateOrUpdateAtSubscriptionScopeAsync("blameMichele", deploymentParam).Result;

            //var test = azure.Deployments.GetById(deployment.Id);
            
            var test2 = await azure.Deployments.Inner.GetAtSubscriptionScopeWithHttpMessagesAsync(deployment.Name);
            var azureFluentSubscriptionDeployment = azure.Deployments.Inner.GetAtSubscriptionScopeAsync(deployment.Name).Result;
            return azureFluentSubscriptionDeployment;                

            
        }
    }
}
