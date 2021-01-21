using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureSDK.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Text.Json;

namespace AzureSDK.Models.Tests
{
    [TestClass()]
    public class ResourceManagementRepoTests
    {
        [TestMethod()]
        public void GetLogAnalyticsSavedSearchTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetDeploymentErrorWorksForRg()
        {
            var rmc = new ResourceManagementRepo();
            var azureClient = new AzureDeploymentClient();

            //var deployments = azureClient.GetDeploymentAtResourceGroup("testingetag", cancellationToken:default).Result;
            //var mostRecent = deployments.OrderByDescending(x => x.Timestamp).FirstOrDefault();
            //var errs = rmc.getErrors("testingetag", "testingEtag").Result;
            var deployment = rmc.GetDeployment("testingetag", "testingEtag").Result;
            Assert.AreEqual("Conflict", deployment.Properties.Error.Details.FirstOrDefault().Code);

            
            //var t = deployment.Properties.FirstOrDefault(x => x.Error.Details != null);
            //var opInfo = rmc.GetDeploymentOperations("testingetag", "testingEtag", deployment.Id);

            //var correlation = rmc.GetDeploymentOperations("testingetag", "testingEtag", deployment.Properties.CorrelationId);

            //var deployments = rmc.GetDeployments().Result.OrderByDescending(x => x.Properties.Timestamp);
            //var mostRecent = deployments.FirstOrDefault();

            //var err = rmc.getErrors("testingetag", "testingetag", mostRecent.Id);
            //Assert.IsNotNull(err);
        }

        
        [TestMethod()]
        public void GetDeploymentErrorWorksForSubscription()
        {
            var rmc = new ResourceManagementRepo();
            var azureClient = new AzureDeploymentClient();

            //var deployments = azureClient.GetDeploymentAtResourceGroup("testingetag", cancellationToken:default).Result;
            //var mostRecent = deployments.OrderByDescending(x => x.Timestamp).FirstOrDefault();

            var deployment = rmc.GetAtSubscriptionScope("testetag");
            Assert.AreEqual("ResourceNotFound", deployment.Properties.Error.Details.FirstOrDefault().Code);

            //todo, make 'get error' private function            
        }

        [TestMethod()]
        public void TestGetPol()
        {
            var azureClient = new AzureDeploymentClient();

            var sub = azureClient.GetSubscription();
            Assert.IsNotNull(sub);

            var policy = azureClient.GetPolicy(sub.SubscriptionPolicies.QuotaId);
            var subResult = JsonSerializer.Serialize(policy);
            System.IO.File.WriteAllText(@"c:\temp\polInfo.json", subResult);
            Assert.IsNotNull(subResult);
            //var basename = "sroUxDwmNew";
        }

        [TestMethod()]
        public void TestGetSub()
        {
            var azureClient = new AzureDeploymentClient();

            var list = azureClient.GetSubscription();
            Assert.IsNotNull(list);
            var subResult = JsonSerializer.Serialize(list);
            System.IO.File.WriteAllText(@"c:\temp\subInfo.json", subResult);

            //var basename = "sroUxDwmNew";
        }

        [TestMethod()]
        public void TestGetWorkspaces()
        {
            var resourceManagementClient = new ResourceManagementRepo();
            List<String> computers = new List<string> { "sroDailyDeluxe1", "sroUxDwmNew", "sroUXwm02", "sroUxDm1104" };

            var list = resourceManagementClient.getOmsWorkspaces();
            Assert.IsNotNull(list);
            //var basename = "sroUxDwmNew";


        }

        [TestMethod()]
        public void TestInsertNewEvent()
        {
            var resourceManagementClient = new ResourceManagementRepo();
            List<String> computers = new List<string> { "foxTest", "sroUxDwmNew", "sroUXwm02", "sroUxDm1104" };

            foreach (string basename in computers)
            {
                var attemptedEvent = resourceManagementClient.sendCustomEvent(basename).Result;
                Assert.IsTrue(attemptedEvent);
            }
            //var basename = "sroUxDwmNew";


        }



        [TestMethod()]
        public void TestParseJson()
        {
            var h = ParseGroups();
            Assert.IsNotNull(h);
        }

        private string[] ParseGroups()
        {
            var groups = new List<String>();
            var azureLoginInfo = new AzureLoginFields();
            var endpoint = $"https://management.azure.com//subscriptions/{azureLoginInfo.SubscriptionId}/resourcegroups/DefaultResourceGroup-EUS/providers/microsoft.operationalinsights/workspaces/DefaultWorkspace-c90795fa-c037-499b-8287-139a1b075851-EUS/configurationscopes/MicrosoftDefaultScopeConfig-Updates?api-version=2015-11-01-preview";
            string token = "replace with tokenHolder";
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            //response had
            // properties
            // etag
            // other stuff
            
            //.properties 
            string properties = "{\r\n  \"Include\": [\r\n    \"Updates__MicrosoftDefaultComputerGroup\",\r\n    \"Updates__MicrosoftDefaultComputerGroup2\"\r\n  ]\r\n}";

            var props = JsonSerializer.Deserialize<GroupProperties>(properties);
            //var groups = JsonSerializer.Deserialize<string[]>(props);
            return groups.ToArray();
        }
    }
}