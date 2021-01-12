using Microsoft.Azure.Management.Automanage;
using Microsoft.Azure.Management.Automanage.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureSDK.Models
{
    public class AutoManagementRepo
    {
        private AzureLoginFields logininfo = new AzureLoginFields();
        private string subscribeTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "SubscriptionTemplate.json");
        private string resourceGroupTemplate = Path.Combine(Directory.GetCurrentDirectory(), "Models", "resourceGroupTemplate.json");
        private string resourceGroupTemplateParams = Path.Combine(Directory.GetCurrentDirectory(), "Models", "resourceGroupTemplateParams.json");

        private IAutomanageClient GetAutomanageClient()
        {
            logininfo = GetLoginInfo();
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(logininfo.ClientId, logininfo.ClientSecret, logininfo.TenantId,
                    AzureEnvironment.AzureGlobalCloud);
            var newClient = new AutomanageClient(credentials);
            newClient.SubscriptionId = logininfo.SubscriptionId;
            return newClient;
        }

        public IEnumerable<Operation> GetOperations()
        {
            var client = GetAutomanageClient();
            return client.Operations.List();
        }

        public ConfigurationProfileAssignment CreateDeployment()
        {            
            var client = GetAutomanageClient();
            
            var properties = new ConfigurationProfileAssignmentProperties(configurationProfile: "Azure virtual machine best practices - Production",
                targetId: null,
                accountId: null,
                configurationProfilePreferenceId: null,
                provisioningStatus: null,
                null);
            var ass = new ConfigurationProfileAssignment("someNewId", "StephenTest", "East US2", "Azure virtual machine best practices - Production", properties);
            //var configParams = new ConfigurationProfilePreference("someId", "StephensTest", "East US", null, null, null);
            var t = client.ConfigurationProfileAssignments.BeginCreateOrUpdate("default", ass, "DeluxeTest", "TestMeAmvmv");
            return t;
        }

        private AzureLoginFields GetLoginInfo()
        {
            string fileName = Path.Combine(Directory.GetCurrentDirectory(), "Models", "LoginInfo.json");
            string jsonString = System.IO.File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<AzureLoginFields>(jsonString);
        }
    }
}
