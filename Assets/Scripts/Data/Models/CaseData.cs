using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameData.Models
{
    [Serializable]
    public class CaseData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("suspectId")]
        public string SuspectId { get; set; }

        [JsonProperty("accusation")]
        public string Accusation { get; set; }

        [JsonProperty("evidenceIds")]
        public List<string> EvidenceIds { get; set; } = new List<string>();

        [JsonProperty("questionIds")]
        public List<string> QuestionIds { get; set; } = new List<string>();

        [JsonProperty("truth")]
        public Dictionary<string, object> Truth { get; set; } = new Dictionary<string, object>();
    }
}
