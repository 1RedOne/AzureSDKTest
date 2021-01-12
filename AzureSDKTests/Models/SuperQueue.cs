using System;
using System.Collections.Generic;
using System.Text;
using Azure.Storage.Queues;

namespace AzureSDKTests.Models
{
    public class SuperQueue
    {
        public string MyName { get; set; }
        public List<RegionalQueueClient> queues { get; set; }        
        public SuperQueue(List<RegionDefinition> regionDefinitions)
        {
            this.queues = new List<RegionalQueueClient>();
            this.MyName = "Stephen";
            foreach (var region in regionDefinitions)
            {
                var thisRegion = new RegionalQueueClient(region.ConnectionString, region.Region);
                this.queues.Add(thisRegion);
            }            
        }
    }
}

