using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureSDK.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static AzureSDK.Models.ResourceManagementRepo;
using System.IO;
using Newtonsoft.Json;
using AzureSDKTests.Models;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureSDK.Models.Tests
{
    [TestClass()]
    public class ConflictResolutionPrototype
    {
        private int totalVMperRegion = 50;
        private string etagTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "etagTemplate.json");
        private string fullSubTemplPath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "fullWorkspaceTemplate.json");
        private string fromAzureTemplat = Path.Combine(Directory.GetCurrentDirectory(), "Models", "FromPortal", "template.json");
        private string fromAzureParam = Path.Combine(Directory.GetCurrentDirectory(), "Models", "FromPortal", "parameters.json");
        private IConfigurationRoot configuration;
        private ResourceManagementRepo rmc;
        private AzureDeploymentClient azure;
        private List<RequestPrototype> enrolledDevices = new List<RequestPrototype>();
        private OmsClient omsClient;
        private ServiceProvider serviceProvider;
        private TokenHolder tokenHolder;
        public class RequestPrototype
        {
            public string VMName { get; set; }
            public string Region { get; set; }
            public int RetryAttempts { get; set; }
            public double Elapsed { get; set; }
            public RequestPrototype(string vmName, string region)
            {
                this.VMName = vmName;
                this.Region = region;
            }
        }

        [TestInitialize]
        public void Setup()
        {
            configuration = TestHelpers.GetIConfigurationRoot(Directory.GetCurrentDirectory());

            var services = new ServiceCollection();

            // Simple configuration object injection (no IOptions<T>)
            services.AddSingleton(configuration);
            serviceProvider = services.BuildServiceProvider();
            rmc = serviceProvider.GetRequiredService<ResourceManagementRepo>();
            azure = serviceProvider.GetRequiredService<AzureDeploymentClient>();
            tokenHolder = serviceProvider.GetRequiredService<TokenHolder>();
            omsClient = serviceProvider.GetRequiredService<OmsClient>();
        }

        [TestMethod()]
        public void ValidateCanGetError()
        {
            //var rmc = new ResourceManagementRepo();
            var deploymentName = "etagTestVM0051";
            var deployment = rmc.GetDeployment("testingetag", deploymentName).Result;
            Assert.AreEqual("Conflict", deployment.Properties.Error.Details.FirstOrDefault().Code);
        }

        [TestMethod()]
        public void Scratch()
        {
            //var azure = new AzureDeploymentClient();
            //var rmc = new ResourceManagementRepo();
            //var token = new TokenHolder();
            var baseTemplate = GetArmTemplate();
            var resource = GetAResource();
            
            var savedSearchAlias = "etagSearch";
            var newVm = "fox02";
            var query = rmc.GetSavedSearchFromWorkspace(tokenHolder.token, resource, savedSearchAlias).Result;
            //add machine to query
            Assert.IsNotNull(query.etag);
            var newQuery = QueryHelper.AddVmToComputerGroupQuery(query.properties.Query, newVm, isIdentifierVmuuid: false);

            var newTemplate = CreateNewTemplateFromQuery(baseTemplate, newQuery, query.etag);
            Assert.IsNotNull(newTemplate);
            var jobj = JObject.FromObject(newTemplate);

            var deployment = azure.NewResourceGroupDeployment(jobj.ToString(), "scratchTest01", "testingetag").Result;
            Assert.IsNotNull(deployment);

        }

        [TestMethod()]
        public void CanGetDefaultWorkspace()
        {            
            Assignment ass = new Assignment();            
            var defaultWorkspace = omsClient.GetDefaultWorkspace(ass);
            var k = omsClient.GetNewDefaultOrExistingWorkspace(ass, tokenHolder.token).Result;
        }

        [TestMethod()]
        public void ScratchFullTemp()
        {            
            var baseTemplate = GetArmTemplate(templateType.fromAzure);
            var resource = GetAResource();         
            var savedSearchAlias = "etagSearch";
            var newVm = "fox0234";
            var query = rmc.GetSavedSearchFromWorkspace(tokenHolder.token, resource, savedSearchAlias).Result;
            //add machine to query
            Assert.IsNotNull(query.etag);
            var newQuery = QueryHelper.AddVmToComputerGroupQuery(query.properties.Query, newVm, isIdentifierVmuuid: false);

            var newTemplate = CreateNewTemplateFromQuery(baseTemplate, newQuery, query.etag);
            Assert.IsNotNull(newTemplate);
            var jobj = JObject.FromObject(newTemplate);
            var parms = JObject.Parse(File.ReadAllText(fromAzureParam));
           
            var deployment = azure.NewSubscriptionDeployment(jobj.ToString(), parms.ToString(), "etag03").Result;
            Assert.IsNotNull(deployment);

        }

        [TestMethod()]
        public async Task CanEnrollNewMachinesWithout409()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var baseTemplate = GetArmTemplate();
            var resource = GetAResource();            
            var savedSearchAlias = "etagSearch";            

            System.Diagnostics.Debug.WriteLine("Beginning Bulk Enrollment");
            
            
            Thread EastUs = new Thread(OnBoardVmsForRegionA){
                Name = "EastUs"
            };

            Thread Europe = new Thread(OnBoardVmsForRegionB){ 
                Name = "Europe"
            };

            EastUs.Start();
            Europe.Start();
            
            EastUs.Join();
            Europe.Join();
            
            //Done, check to see that the query has chnged
            var query = rmc.GetSavedSearchFromWorkspace(tokenHolder.token, resource, savedSearchAlias).Result;
            Assert.IsNotNull(query);
            //Assert.IsTrue(enrolledDevices.Count >= 90);

            string json = JsonConvert.SerializeObject(enrolledDevices.ToArray());

            stopwatch.Stop();
            //write string to file
            System.IO.File.WriteAllText(@"C:\Temp\etag\enrolled_100.json", json);
            System.Diagnostics.Debug.WriteLine($"Bulk Enrollment Completed in {stopwatch.Elapsed.TotalSeconds}");
        }

        private void OnBoardVmsForRegionA()
        {
            List<RequestPrototype> queue = Enumerable.Range(1, totalVMperRegion).
                Select(x => new RequestPrototype($"VMUS{x}", "EastUS")).ToList();

            System.Diagnostics.Debug.WriteLine("[EASTUS Region Online]");
            System.Diagnostics.Debug.WriteLine($"[EASTUS] Onboarding {queue.Count} machines");
            //locking would serialize, so we can get the same in prototype with a ForEach loop
            foreach (RequestPrototype request in queue)
            {
                System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - Onboarding...");
                var requestResult = tryToUpdateQuery(request, 10);
                
                enrolledDevices.Add(requestResult);
            }
            System.Diagnostics.Debug.WriteLine("[EASTUS Region Online]");
            //return ("complete");
        }

        private void OnBoardVmsForRegionB()
        {
            List<RequestPrototype> queue = Enumerable.Range(1, totalVMperRegion).
                Select(x => new RequestPrototype($"VMEU{x}", "Europe")).ToList();

            System.Diagnostics.Debug.WriteLine("[Europe Region Online]");
            System.Diagnostics.Debug.WriteLine($"[Europe] Onboarding {queue.Count} machines");
            //locking would serialize, so we can get the same in prototype with a ForEach loop
            foreach (RequestPrototype request in queue)
            {
                System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - Onboarding...");
                var requestResult = tryToUpdateQuery(request, 10);

                enrolledDevices.Add(requestResult);
            }
            System.Diagnostics.Debug.WriteLine("[Europe Region Completed]");
            //return ("complete");
        }

        private void OnBoardVmsForRegionC()
        {
            List<RequestPrototype> queue = Enumerable.Range(1, totalVMperRegion).
                Select(x => new RequestPrototype($"VMEU{x}", "Japan0")).ToList();
            //locking would serialize, so we can get the same in prototype with a ForEach loop
            foreach (RequestPrototype request in queue)
            {
                System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - Onboarding...");
                var requestResult = tryToUpdateQuery(request, 10);

                enrolledDevices.Add(requestResult);
            }

            //return ("complete");
        }

        public async Task<string> OnBoardVmsForRegion(List<RequestPrototype> queue)
        {
            //locking would serialize, so we can get the same in prototype with a ForEach loop
            foreach (RequestPrototype request in queue)
            {
                Console.WriteLine($"[{request.Region}]-[{request.VMName}] - Onboarding...");
                tryToUpdateQuery(request, 10);
            }

            return ("complete");
        }
        
        private RequestPrototype tryToUpdateQuery(RequestPrototype request, int times)
        {
            Stopwatch stopwatch = new Stopwatch();

            // Begin timing.
            stopwatch.Start();
            Random random = new Random();
            int sleepPeriod = random.Next(1, 5) * 1000;
            Thread.Sleep(sleepPeriod);
            int i = 1;
            var newVm = request.VMName;            
            var savedSearchAlias = "etagSearch";
            var resource = GetAResource();
            var baseTemplate = GetArmTemplate();
            var deploymentName = $"etagTest{newVm}";
            SavedSearch query = new SavedSearch();
            while (i < times)
            {
                //get Query 
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - getting query, [{i}] attempt");
                    query = rmc.GetSavedSearchFromWorkspace(tokenHolder.token, resource, savedSearchAlias).Result;
                    //add machine to query

                    if (null == query)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - exception was 401 error, likely need new token!");

                        throw new Exception("Need to get a new Bearer Token!");
                    }

                    var newQuery = QueryHelper.AddVmToComputerGroupQuery(query.properties.Query, newVm, isIdentifierVmuuid: false);
                    //update query 

                    var newTemplate = CreateNewTemplateFromQuery(baseTemplate, newQuery, query.etag);
                    var jobj = JObject.FromObject(newTemplate);

                    System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - conducting deployment, [{i}] attempts");
                    
                    
                    var deployment = azure.NewResourceGroupDeployment(jobj.ToString(), deploymentName, "testingetag").Result;
                    stopwatch.Stop();
                    request.Elapsed= stopwatch.Elapsed.TotalSeconds;
                    request.RetryAttempts = i;
                    //it worked if we got here so we return true                    
                    System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - added in [{i}] attempts ⌚[{request.Elapsed}]");
                    return request;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - exception occurred!");
                    if (null == query)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - exception was 401 error, likely need new token!");

                        throw new Exception("Need to get a new Bearer Token!");
                    }
                    var deployment = rmc.GetDeployment("testingetag", deploymentName).Result;
                    //Assert.AreEqual("Conflict", deployment.Properties.Error?.Details.FirstOrDefault().Code);
                    
                    
                    if ("Conflict" == deployment.Properties.Error.Details.FirstOrDefault().Code){
                        System.Diagnostics.Debug.WriteLine($"[{request.Region}]-[{request.VMName}] - exception was 409 Conflict, retrying...!");
                        i++;
                        continue;
                    }
                    else
                    {
                        throw ex;
                    }

                }

            }
            stopwatch.Stop();
            request.Elapsed = stopwatch.Elapsed.TotalSeconds;
            request.RetryAttempts = -1;
            return request;
        }

        private WorkspaceResource GetAResource()
        {
            return new WorkspaceResource(name: "testingetag", sub: "cdd53a71-7d81-493d-bce6-224fec7223a9", resourceGroupName: "testingetag");
        }

        private ArmTemplateClass GetArmTemplate(templateType templateType = templateType.etagSample)
        {
            string jsonString;
            if (templateType == templateType.subscription)
            {
                jsonString = System.IO.File.ReadAllText(fullSubTemplPath);
            }
            else if (templateType == templateType.fromAzure)
            {
                jsonString = System.IO.File.ReadAllText(fromAzureTemplat);//updte
            }
            else
            {
                jsonString = System.IO.File.ReadAllText(etagTemplatePath);
            }
            return JsonConvert.DeserializeObject<ArmTemplateClass>(jsonString);
        }

        private ArmTemplateClass CreateNewTemplateFromQuery(ArmTemplateClass template, string newQuery, string etag)
        {
            var search = template.resources.Where(x => x.type == "Microsoft.OperationalInsights/workspaces/savedSearches").FirstOrDefault();

            if (search == null)
            {
                search = template.resources.Where(m => m.type == "Microsoft.Resources/deployments")
                            .Where(m => m.name == "[parameters('dependenciesDeployName')]")
                            .FirstOrDefault().properties.template.resources
                            .Where(k => k.type == "Microsoft.OperationalInsights/workspaces/savedSearches").FirstOrDefault();
                
            }
            search.properties.Etag = etag;
            search.properties.Query = newQuery;

            return template;
        }

        public enum templateType
        {
            fromAzure,subscription,etagSample
        }
    }
}
