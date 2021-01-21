﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var endpoint = "https://management.azure.com//subscriptions/c90795fa-c037-499b-8287-139a1b075851/resourcegroups/DefaultResourceGroup-EUS/providers/microsoft.operationalinsights/workspaces/DefaultWorkspace-c90795fa-c037-499b-8287-139a1b075851-EUS/configurationscopes/MicrosoftDefaultScopeConfig-Updates?api-version=2015-11-01-preview";
            string token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6ImtnMkxZczJUMENUaklmajRydDZKSXluZW4zOCIsImtpZCI6ImtnMkxZczJUMENUaklmajRydDZKSXluZW4zOCJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuYXp1cmUuY29tLyIsImlzcyI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0LzcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0Ny8iLCJpYXQiOjE2MDY4Mzk3NzksIm5iZiI6MTYwNjgzOTc3OSwiZXhwIjoxNjA2ODQzNjc5LCJfY2xhaW1fbmFtZXMiOnsiZ3JvdXBzIjoic3JjMSJ9LCJfY2xhaW1fc291cmNlcyI6eyJzcmMxIjp7ImVuZHBvaW50IjoiaHR0cHM6Ly9ncmFwaC53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvdXNlcnMvODYxODZjMDgtYWVkOC00MDFlLTkzMjctMjExOGZjNzkyZGE0L2dldE1lbWJlck9iamVjdHMifX0sImFjciI6IjEiLCJhaW8iOiJBVlFBcS84UkFBQUFVd1E0MzZJSUNZY250dGxkeTRkSG9qWHpuc25hSDhFOVZ1emVrbnhzeHFzRmFvVVBYTVJleHN1b1lObHV0NEh0L2lDTzB0OGlUQy9pR096QWFHMmMyUWRtbVNTMEFxc0kzYVcyWmZBa3V5OD0iLCJhbXIiOlsicnNhIiwibWZhIl0sImFwcGlkIjoiMDRiMDc3OTUtOGRkYi00NjFhLWJiZWUtMDJmOWUxYmY3YjQ2IiwiYXBwaWRhY3IiOiIwIiwiZGV2aWNlaWQiOiJlN2UwZTg2Ny00NTYxLTQ0ZjMtODg3ZC1iZTA2ZGJkMDhiN2IiLCJmYW1pbHlfbmFtZSI6IkNlcnUiLCJnaXZlbl9uYW1lIjoiTWljaGVsZSIsImlwYWRkciI6Ijg3LjE3LjIxNS43IiwibmFtZSI6Ik1pY2hlbGUgQ2VydSIsIm9pZCI6Ijg2MTg2YzA4LWFlZDgtNDAxZS05MzI3LTIxMThmYzc5MmRhNCIsIm9ucHJlbV9zaWQiOiJTLTEtNS0yMS0xMjQ1MjUwOTUtNzA4MjU5NjM3LTE1NDMxMTkwMjEtMTk4NDUwNiIsInB1aWQiOiIxMDAzMjAwMEMxQkMzMUIxIiwicmgiOiIwLkFSb0F2NGo1Y3ZHR3IwR1JxeTE4MEJIYlI1VjNzQVRialJwR3UtNEMtZUdfZTBZYUFCYy4iLCJzY3AiOiJ1c2VyX2ltcGVyc29uYXRpb24iLCJzdWIiOiI0SUhxZHBPMVdVYjdwTmpfcTZUcHQteTYtbi1lX1JMTXdGT3F6LTl3ZzRJIiwidGlkIjoiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3IiwidW5pcXVlX25hbWUiOiJtaWNlcnVAbWljcm9zb2Z0LmNvbSIsInVwbiI6Im1pY2VydUBtaWNyb3NvZnQuY29tIiwidXRpIjoicGRPY25YVWlaMGVSejVWZjc1TEJBUSIsInZlciI6IjEuMCIsIndpZHMiOlsiYjc5ZmJmNGQtM2VmOS00Njg5LTgxNDMtNzZiMTk0ZTg1NTA5Il0sInhtc190Y2R0IjoxMjg5MjQxNTQ3fQ.n4CF7m1oCDM76trNJtT3qoVZHNk0___yGv8LalPEBngiOf3qcoL1EVu3mQmYWl5-BJS4YdjHzpL64g6v5IIiOpYzQT5f8aybXD7rTWGwxnW1uX4mv-Xx0bwL_XqzjuuMNzD1AJMu75oV_6cATpwYa0i3fw7nRdjajmMWvEBdn7joiSZdmgphF1bKT2UwZRHX27DFch3UK-W6vbDcdZn0h00k0bvfNAlhjRxJtvvT60dmMnjK2Fi7zX55LYVss5Wi_tHziiXIhR4ioZ6nGvruPTLrU2PnCKMzlTMlqbpSyPuCeJv2Y1TFdUA0yYhOcjiAIPjcTVhovWelkaCKt8pL7g";
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