using System;
using Newtonsoft.Json;

namespace GameData.Models
{
    [Serializable]
    public class EvidenceData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("reliability")]
        public string Reliability { get; set; }
    }
}
