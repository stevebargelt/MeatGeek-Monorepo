using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Data
{

    public class Body
    {
        public string id { get; set; }
        public int ttl { get; set; }
        public string smokerId { get; set; }
        public string sessionId { get; set; }
        public string type { get; set; }
        public bool augerOn { get; set; }
        public bool blowerOn { get; set; }
        public bool igniterOn { get; set; }
        public StatusTemps temps { get; set; }
        public bool fireHealthy { get; set; }
        public string mode { get; set; }
        public int setPoint { get; set; }
        public DateTime modeTime { get; set; }
        public DateTime currentTime { get; set; }
    }

    public class Properties
    {
        public string correlationId { get; set; }
        public string sequenceNumber { get; set; }
        public string SessionId { get; set; }
    }

    public class SessionStatusDocument
    {
        public string id { get; set; }
        public string smokerId { get; set; }
        public Properties Properties { get; set; }
        public SystemProperties SystemProperties { get; set; }

        [JsonProperty("iothub-name")]
        public string iothubname { get; set; }
        public Body Body { get; set; }
        public string _rid { get; set; }
        public string _self { get; set; }
        public string _etag { get; set; }
        public string _attachments { get; set; }
        public int _ts { get; set; }
    }

    public class SystemProperties
    {
        [JsonProperty("iothub-connection-device-id")]
        public string iothubconnectiondeviceid { get; set; }
        [JsonProperty("iothub-connection-module-id")]
        public string iothubconnectionmoduleid { get; set; }
        [JsonProperty("iothub-connection-auth-method")]
        public string iothubconnectionauthmethod { get; set; }
        [JsonProperty("iothub-connection-auth-generation-id")]
        public string iothubconnectionauthgenerationid { get; set; }
        [JsonProperty("iothub-content-type")]
        public string iothubcontenttype { get; set; }
        [JsonProperty("iothub-content-encoding")]
        public string iothubcontentencoding { get; set; }
        [JsonProperty("iothub-enqueuedtime")]
        public DateTime iothubenqueuedtime { get; set; }
        [JsonProperty("iothub-message-source")]
        public string iothubmessagesource { get; set; }
    }

}