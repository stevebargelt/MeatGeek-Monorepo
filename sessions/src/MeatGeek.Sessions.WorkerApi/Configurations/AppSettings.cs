using Microsoft.Azure.WebJobs.Extensions.OpenApi.Configurations.AppSettings.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Configuration;

namespace MeatGeek.Sessions.WorkerApi.Configurations
{
    public class AppSettings : OpenApiAppSettingsBase
    {
        public AppSettings()
            : base()
        {
            this.OpenApi = this.Config.Get<OpenApiSettings>("OpenApi");
        }

        public virtual OpenApiSettings OpenApi { get; set; }
    }

    public class OpenApiSettings
    {
        public virtual string ApiKey { get; set; }
    }
}
