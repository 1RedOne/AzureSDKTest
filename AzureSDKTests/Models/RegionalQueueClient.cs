using System;
using System.Collections.Generic;
using System.Text;
using Azure.Storage.Queues;

namespace AzureSDKTests.Models
{
    public class RegionalQueueClient 
    {
        public string region { get; set; }
        private QueueClient queue;

        public RegionalQueueClient(string clientInfo, string region)
        {
            QueueClient queue = new QueueClient(clientInfo, "region");
            this.queue = queue;
            this.region = region;
        }
    }

    public class RegionDefinition
    {
        public string Region { get; set; }
        public string ConnectionString { get; set; }
    }
}
