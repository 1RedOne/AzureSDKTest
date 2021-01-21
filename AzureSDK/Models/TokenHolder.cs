using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureSDK.Models
{
        public class TokenHolder
        {
            public string token { get; set; }
            public TokenHolder(IConfiguration configuration)
            {
                this.token = configuration["resourceManagement:token"]; 
            }
        }
}
