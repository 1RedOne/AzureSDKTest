namespace AzureSDK.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    
    using AzureSDK.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;
    using Microsoft.Azure.Management.ResourceManager.Models;
    using Microsoft.Extensions.Logging;
    
    using Newtonsoft.Json;    

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private string etagTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "etagTemplate.json");
        private AzureLoginFields logininfo = new AzureLoginFields();
        private JsonSerializerSettings loopHandler = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        };

        private string subscribeTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "SubscriptionTemplate.json");
        private string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "Template.json");
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            logininfo = GetLoginInfo();
        }

        [HttpPost]
        public async Task<IActionResult> ArmResourceGroupDeployment(string ok)
        {
            string template = System.IO.File.ReadAllText(etagTemplatePath);
            var azureClientRepo = new AzureDeploymentClient();
            //testnew method

            var rmc = new ResourceManagementRepo();

            var err = rmc.getErrors("testingetag", "testingetag");

            var ret = new ArmDeployment(err.Result[0]);
            return View("Details", ret);
            var deploymentExtendedInner = await azureClientRepo.NewResourceGroupDeployment(template,"testingetag");

            //var rg = azure.ResourceGroups
            //             .Define("srofoxtestrg07161")
            //             .WithRegion("West US");
            ////var rg = await resourcesManagementClient.ResourceGroups.CreateOrUpdateAsync("srofoxtestrg0716", new ResourceGroup("West US"));

            //var t = await azure.Deployments.Define("someTest1234")
            //        .WithNewResourceGroup(rg)
            //        .WithTemplate(template).WithParameters("{}")
            //        .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
            //        .CreateAsync(CancellationToken.None);

            var deployment = deploymentExtendedInner;
            //returm 
            //var h = new ArmDeployment(
            //        deployment.CorrelationId ?? "notProvided",
            //        deployment.Timestamp ?? DateTime.Now,
            //        deployment.ProvisioningState ?? "otherState",
            //        deployment.ResourceGroupName,
            //        deployment.Name,
            //        deployment
            //        );
            //ViewBag.Template =
            //            Newtonsoft.Json.JsonConvert.SerializeObject(deploymentExtendedInner,
            //            Formatting.Indented, loopHandler);
            ViewBag.Deployed = true;
            //TODO: redirect to details endpoint 
            return View("Details", deployment);
        }

        [HttpGet]
        public IActionResult AutomanageClientTest()
        {
            var client = new AutoManagementRepo();
            var ops = client.GetOperations();
            ViewBag.Operations = ops;
            return View(ops);
        }

        [HttpPost]
        public IActionResult AutomanageClientTestCreate()
        {
            var client = new AutoManagementRepo();
            var ops = client.CreateDeployment();
            return View(ops);
        }

        public async Task<IDeployment> AzureDeployment(ICreatable<IResourceGroup> rg, object template, string deploymentName = "someTest1234", string parameters = "{}")
        {
            if (null == rg)
            {
                throw new System.ArgumentNullException("ResourceGroup");
            }
            if (null == template)
            {
                throw new System.ArgumentNullException("Template");
            }
            if (null == deploymentName)
            {
                throw new System.ArgumentNullException("DemploymentName");
            }
            var azure = GetAzureClient();

            return await azure.Deployments.Define(deploymentName).WithNewResourceGroup(rg)
                   .WithTemplate(template).WithParameters(parameters)
                   .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                   .CreateAsync(CancellationToken.None);

        }

        [HttpGet]
        public async Task<IActionResult> AzureFail()
        {
            ViewBag.Error = "No Error";
            var azure = GetAzureClient();
            var rg = azure.ResourceGroups
                         .Define("srofoxtestrg07161")
                         .WithRegion("West US");
            rg = null;
            try
            {
                var r = AzureDeployment(rg, null);
            }
            catch (System.ArgumentNullException e)
            {
                ViewBag.Error = "Some shit broke";
            }
            catch (Exception e)
            {
                ViewBag.Error = "Some shit broke";
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AzureGet(DeploymentExtended? deploymentExtended)
        {

            List<ArmDeployment> deployments = new List<ArmDeployment>();
            if (deploymentExtended.Name != null)
            {

                deployments.Add(new ArmDeployment(
                    deploymentExtended.Id,
                    deploymentExtended.Properties.Timestamp,
                    deploymentExtended.Properties.ProvisioningState,
                    null,
                    deploymentExtended.Name,
                    deploymentExtended)
                    );
                ViewBag.Deployments = deployments;
                ViewBag.DeploymentSourceObject = deploymentExtended;
                return View();
            }
            var azure = GetAzureClient();
            var deployment = azure.Deployments.GetByName("someTest1234");

            deployments.Add(new ArmDeployment(deployment.CorrelationId, deployment.Timestamp, deployment.ProvisioningState, deployment.ResourceGroupName, deployment.Name, deployment));
            ViewBag.Deployments = deployments;
            ViewBag.DeploymentSourceObject = deployment;
            return View();
        }

        [HttpGet]
        public IActionResult DeploymentTest()
        {
            string template = System.IO.File.ReadAllText(templatePath);
            string subscriptionTemplate = System.IO.File.ReadAllText(subscribeTemplatePath);
            ViewBag.Template = template;
            ViewBag.SubscriptionTemplate = subscriptionTemplate;
            ViewBag.Deployed = false;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(string name, DeploymentExtended? deploymentExtended)
        {
            if (deploymentExtended.Name != null)
            {
                var armDeployment = new ArmDeployment(
                    deploymentExtended.Id,
                    deploymentExtended.Properties.Timestamp,
                    deploymentExtended.Properties.ProvisioningState,
                    null,
                    deploymentExtended.Name,
                    deploymentExtended);
                ViewBag.DeploymentSourceObject = deploymentExtended;
                return View(armDeployment);
            }

            var azure = GetAzureClient();
            var deployment = azure.Deployments.GetByName(name);
            var output = new ArmDeployment(deployment.Inner.Id, deployment.Timestamp, deployment.ProvisioningState, deployment.ResourceGroupName, deployment.Name, deployment);
            ViewBag.DeploymentSourceObject = deployment;
            return View(output);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> FluentAzureSubscription(string ok = "123")
        {
            var azureClientRepo = new AzureDeploymentClient();
            //testnew method
            var deploymentExtendedInner = await azureClientRepo.NewSubscriptionDeployment();
            var armDeployment = new ArmDeployment(
                    deploymentExtendedInner.Id,
                    deploymentExtendedInner.Properties.Timestamp,
                    deploymentExtendedInner.Properties.ProvisioningState,
                    null,
                    deploymentExtendedInner.Name,
                    deploymentExtendedInner);
            ViewBag.DeploymentSourceObject = deploymentExtendedInner;
            ViewBag.DeploymentSourceObjectType = deploymentExtendedInner.GetType().Name;
            ViewBag.DeploymentType = "Azure Flient Subscription Deployment";
            return View("Details", armDeployment);
        }

        public IActionResult Index()
        {
            logininfo = GetLoginInfo();
            ViewBag.loginJson = JsonConvert.SerializeObject(logininfo);
            return View(logininfo);
        }

        public async Task<IActionResult> ListDeployments()
        {
            var client = GetAzureClient();
            var list = await client.Deployments.ListAsync();
            return View(list);
        }
        //[HttpGet]
        //public async Task<IActionResult> ListDeployments()
        //{
        //    var azure = GetAzureClient();
        //    var list = azure.Deployments.List();
        //    var rmcrepo = new ResourceManagementRepo();
        //    var k = rmcrepo.GetDeployments().Result;
        //    var azureClientRepo = new AzureDeploymentClient();
        //    List<ArmDeployment> deployments = new List<ArmDeployment>();
        //    foreach (var deployment in list)
        //    {
        //        deployments.Add(new ArmDeployment(deployment.Inner.Id, deployment.Timestamp, deployment.ProvisioningState, deployment.ResourceGroupName, deployment.Name, deployment));
        //    }
        //    var realList = azureClientRepo.GetDeployments(CancellationToken.None).Result;
        //    var deployCount = azureClientRepo.GetDeploymentCount(CancellationToken.None).Result;
        //    ViewBag.DeployCount = deployCount;
        //    return View(deployments);
        //}
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResourceManagementAzureSubscription(string ok = "123")
        {

            string subscriptionTemplate = System.IO.File.ReadAllText(subscribeTemplatePath);

            var repo = new ResourceManagementRepo();



            var deploymentExtended = await repo.CreateSubscriptionDeployment();
            var armDeployment = new ArmDeployment(
                    deploymentExtended.Id,
                    deploymentExtended.Properties.Timestamp,
                    deploymentExtended.Properties.ProvisioningState,
                    null,
                    deploymentExtended.Name,
                    deploymentExtended);
            ViewBag.DeploymentType = "Resource Management Client Deployment";
            ViewBag.DeploymentSourceObject = deploymentExtended;
            ViewBag.DeploymentSourceObjectType = deploymentExtended.GetType().Name;
            return View("Details", armDeployment);
        }

        public async Task<IActionResult> SubscriptionDetails()
        {
            var repo = new ResourceManagementRepo();
            var deploymentExtended = repo.GetAtSubscriptionScope("my123", waitForCompletion: true);
            var armDeployment = new ArmDeployment(
                    deploymentExtended.Id,
                    deploymentExtended.Properties.Timestamp,
                    deploymentExtended.Properties.ProvisioningState,
                    null,
                    deploymentExtended.Name,
                    deploymentExtended);
            ViewBag.DeploymentSourceObject = deploymentExtended;
            return View("Details", armDeployment);

        }

        private IAzure GetAzureClient()
        {
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
        private async Task<ArmDeployment> NewSubscriptionDeployment()
        {
            var azure = GetAzureClient();
            var template = System.IO.File.ReadAllText(subscribeTemplatePath);

            

            Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentInner deploymentParam = new Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentInner()
            {
                Location = "westus",
                Properties = new Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentProperties()
                {
                    Mode = Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental,
                    Template = template,
                    Parameters = "{}"
                }
            };

            Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentExtendedInner deployment = await azure.Deployments.Inner.CreateOrUpdateAtSubscriptionScopeAsync("dpA", deploymentParam);            
            
            var h = new ArmDeployment(
                    deployment.Id,
                    deployment.Properties.Timestamp,
                    deployment.Properties.ProvisioningState,
                    null,
                    deployment.Name,
                    deployment
                    );

            return h;
        }
    }
}