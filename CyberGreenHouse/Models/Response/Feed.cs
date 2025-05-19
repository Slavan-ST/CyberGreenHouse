using System;
using Newtonsoft.Json;

namespace CyberGreenHouse.Models.Response
{
    public class Feed
    {
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        public int EntryId { get; set; }
        public string Field1 { get; set; } = "undefined";
        public string Field2 { get; set; } = "undefined";
        public string Field3 { get; set; } = "undefined";
        public string Field4 { get; set; } = "undefined";
        public string Field5 { get; set; } = "undefined";
        public string Field6 { get; set; } = "undefined";
        public string Field7 { get; set; } = "undefined";
        public string Field8 { get; set; } = "undefined";
    }
}