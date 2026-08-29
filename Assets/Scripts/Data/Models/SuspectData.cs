using System;
using Newtonsoft.Json;

namespace GameData.Models
{
    [Serializable]
    public class SuspectData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("age")]
        public int Age { get; set; }

        [JsonProperty("occupation")]
        public string Occupation { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("portraitId")]
        public string PortraitId { get; set; }
    }
}
