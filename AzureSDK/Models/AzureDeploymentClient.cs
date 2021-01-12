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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private AzureLoginFields logininfo = new AzureLoginFields();
        private string subscribeTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "SubscriptionTemplate.json");
        private string resourceGroupTemplate = Path.Combine(Directory.GetCurrentDirectory(), "Models", "resourceGroupTemplate.json");
        private string resourceGroupTemplateParams = Path.Combine(Directory.GetCurrentDirectory(), "Models", "resourceGroupTemplateParams.json");

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

        public async Task<ArmDeployment> NewResourceGroupDeployment(string template, string rgName = "sroTemplateTest")
        {
            var azure = GetAzureClient();

            bool needToCreateRg;
            try { 
                azure.ResourceGroups.GetByName(rgName);
                needToCreateRg = false; //dont forget 
            }
            catch
            {
                needToCreateRg = true;
            }
            if (needToCreateRg)
            {
                var rg = azure.ResourceGroups
                             .Define(rgName)
                             .WithRegion("West US");

                try
                {
                    //testing to exceptions
                    log.Info("about to create deployment!");
                    var deploymentCreated2 = await azure.Deployments.Define("testingetag")
                    .WithExistingResourceGroup(rg.Name)
                    .WithTemplate(template)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync(CancellationToken.None);
                    return new ArmDeployment(null, null, null, null, null, deploymentCreated2);
                    
                }
                catch(Exception ex)
                {
                    log.Error("ERROR CREATING deployment!", ex);
                }
                //valudate               
                var deploymentCreated = await azure.Deployments.Define("hassantest2")
                    .WithNewResourceGroup(rg)
                    .WithTemplate(template)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync(CancellationToken.None);

                var val = new ArmDeployment(null, null, null, null, null, deploymentCreated);
                return val;

            }
            else
            {
                try
                {
                    var result = await azure.Deployments.Define("testingEtag")
                        .WithExistingResourceGroup(rgName)
                        .WithTemplate(template)
                        .WithParameters("{}")
                        .WithMode(DeploymentMode.Incremental)
                        .CreateAsync(CancellationToken.None);

                    var val2 = new ArmDeployment(null, null, null, null, null, result);
                    return val2;
                }
                catch(Exception ex)
                {
                    var t = ex.Data;
                    var deploymentoperation = azure.Deployments.GetByName("testingEtag");
                    var hhe = azure.Deployments.Inner.GetAsync("testingetag", "testingetag");
                    //var val = new ArmDeployment(null, null, null, null, null, result);
                    //  return val;
                    throw ex;
                }                
            }
            
            
        }

        public async Task<DeploymentExtendedInner> NewSubscriptionDeployment()
        {
            var azure = GetAzureClient();
            var template = System.IO.File.ReadAllText(subscribeTemplatePath);
            
            DeploymentInner deploymentParam = new DeploymentInner()
            {
                Location = "westus",
                Properties = new DeploymentProperties()
                {
                    Mode = DeploymentMode.Incremental,
                    Template = Newtonsoft.Json.JsonConvert.DeserializeObject(template),                    
                    Parameters = Newtonsoft.Json.JsonConvert.DeserializeObject("{}")
                }
            };

            var deployment = azure.Deployments.Inner.BeginCreateOrUpdateAtSubscriptionScopeAsync("testetag", deploymentParam).Result;

            //var test = azure.Deployments.GetById(deployment.Id);
            
            var test2 = await azure.Deployments.Inner.GetAtSubscriptionScopeWithHttpMessagesAsync(deployment.Name);
            var azureFluentSubscriptionDeployment = azure.Deployments.Inner.GetAtSubscriptionScopeAsync(deployment.Name).Result;
            var k = azure.Deployments.Inner.GetAtSubscriptionScopeAsync(deployment.Name);
            return azureFluentSubscriptionDeployment;                

            
        }

        public async Task<List<IDeployment>> GetDeployments(CancellationToken cancellationToken)
        {            
         
            try
            {                
                var azureAuthenticationClient = GetAzureClient();

                var deployments =
                    await azureAuthenticationClient.Deployments.ListAsync(true, cancellationToken);

                return deployments.ToList();
            }
            catch (Exception ex)
            {                
                throw ex;
            }
        }

        public async Task<List<IDeployment>> GetDeploymentAtResourceGroup(string rgname, CancellationToken cancellationToken)
        {

            try
            {
                var azureAuthenticationClient = GetAzureClient();

                var deployments =
                    await azureAuthenticationClient.Deployments.ListByResourceGroupAsync(rgname, cancellationToken:default);

                return deployments.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<int> GetDeploymentCount(CancellationToken cancellationToken)
        {
            try
            {
                var deployments = await this.GetDeployments(cancellationToken);

                return deployments.Count();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ISubscription GetSubscription(CancellationToken cancellationToken=default)
        {

            try
            {
                var azureAuthenticationClient = GetAzureClient();

                var subscription =
                    azureAuthenticationClient.GetCurrentSubscription();
                
                return subscription;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IPolicyDefinition GetPolicy(string policyName, CancellationToken cancellationToken = default)
        {

            try
            {
                var azureAuthenticationClient = GetAzureClient();

                var policies =
                    azureAuthenticationClient.PolicyDefinitions.List();
                var ee2 = policies.FirstOrDefault(x => x.Name == policyName);
                return policies.FirstOrDefault(x=>x.Name==policyName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

       
    }
}
