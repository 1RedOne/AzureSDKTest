

namespace AzureSDK.Models.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using AzureSDK.Models;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net.Http;
    using System.Text.Json;
    using Azure.Storage.Queues;
    using Azure.Storage.Queues.Models;
    using Microsoft.Extensions.Configuration;
    using AzureSDKTests.Models;
    using System.Linq;
    using Microsoft.Azure.AutoManaged.Infrastructure.Arm;
    using Microsoft.Extensions.Logging;

    [TestClass()]
    public class SuperQueueClientTest
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            return config;
        }

        //private bool getSavedSearch()
        //{
        //    var myClient = new HttpClient();
        //    var logger = new Logger<ResourceManagerClient>(new LoggerFactory());
        //    var rem = new ResourceManagerClient(myClient, logger);
        //    var token = new TokenServi
        //    rem.GetSavedSearchFromWorkspace()
        //}

        [TestMethod()]
        public void GetLogAnalyticsSavedSearchTest()
        {
            var config = InitConfiguration();
            var test = config.GetSection("SuperQueue");
            var name = test["Myname"];
            var regionDefinitions = config.GetSection("SuperQueue:QueueClients").Get<List<RegionDefinition>>();
            var goodHam = regionDefinitions.FirstOrDefault();
            //var terminalSections = this.Configuration.GetSection("Terminals").GetChildren();
            //foreach (var item in terminalSections)
            //{
            //    terminals.Add(new Terminal
            //    {
            //        // perform type mapping here 
            //    });
            //}

            SuperQueue mySuperQueue = new SuperQueue(regionDefinitions);
            
            
            
            var connectionString = GetConnectionString();
            QueueClient queue = new QueueClient(connectionString, "mystoragequeue");
            

            queue.CreateIfNotExists();
            queue.SendMessage("myCoolMessage");
            Assert.IsNotNull(queue);
        }

        private string GetConnectionString()
        {
            return "updateToWorkWithTokenHolder";        
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
    }
}