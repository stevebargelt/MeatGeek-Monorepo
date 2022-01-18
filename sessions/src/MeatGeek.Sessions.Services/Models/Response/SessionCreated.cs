using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Response
{
    public class SessionCreated
    {
        [JsonProperty("id")] 
        public string Id { get; set; }
    }
}