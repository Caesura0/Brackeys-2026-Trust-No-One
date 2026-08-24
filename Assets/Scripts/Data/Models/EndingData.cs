using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameData.Models
{
    [Serializable]
    public class EndingData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("conditions")]
        public Dictionary<string, int> Conditions { get; set; } = new Dictionary<string, int>();
    }
}
