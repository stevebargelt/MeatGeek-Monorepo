using System;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace MeatGeek.Sessions.WorkerApi.Configurations
{
    public class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
    {
        public override OpenApiInfo Info { get; set; } = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "MeatGeek IoT API",
            Description = "An back-end API for reading and managing MeatGeek SEssions",
            TermsOfService = new Uri("https://github.com/stevebargelt"),
            Contact = new OpenApiContact()
            {
                Name = "Harebrained Apps",
                Email = "azfunc-openapi@harebrained-apps.com",
                Url = new Uri("https://github.com/stevebargelt"),
            },
            License = new OpenApiLicense()
            {
                Name = "MIT",
                Url = new Uri("http://opensource.org/licenses/MIT"),
            }
        };
    }
}
