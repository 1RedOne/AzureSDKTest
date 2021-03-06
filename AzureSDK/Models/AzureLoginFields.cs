﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureSDK.Models
{
    public class AzureLoginFields
    {
        public string ClientId;
        public string ClientSecret;
        public string TenantId;
        public string SubscriptionId;
        public string OmsWorkSpaceID;
        public string OmsSharedKey;
        public string[] ConnectionStrings;

        public AzureLoginFields(IConfiguration configuration)
        {
            configuration.GetSection("LoginInfo").Get<AzureLoginFields>();
            //return newField;
        }

    }
}
