namespace AzureSDK.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Newtonsoft.Json;

    public class CustomOmsRecord
    {
        //
        // Summary:
        //     Gets or sets the list of dependencies.
        [JsonProperty(PropertyName = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        //
        // Summary:
        //     Gets or sets the list of dependencies.
        [JsonProperty(PropertyName = "vMName")]
        public string VMName { get; set; }

        //
        // Summary:
        //     Gets or sets the list of dependencies.
        [JsonProperty(PropertyName = "Computer")]
        public string Computer { get; set; }

        public CustomOmsRecord(string vMName)
        {
            this.Computer = vMName;
            this.VMName = $"{vMName}{DateTime.Now.ToShortTimeString().Replace(':', '-')}"; ;
            this.TimeStamp = DateTime.UtcNow;

        }
    }
}