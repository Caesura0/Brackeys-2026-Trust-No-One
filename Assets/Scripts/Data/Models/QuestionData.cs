using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameData.Models
{
    [Serializable]
    public class QuestionResponse
    {
        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("answer")]
        public string Answer { get; set; }
    }

    [Serializable]
    public class QuestionData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("responses")]
        public List<QuestionResponse> Responses { get; set; } = new List<QuestionResponse>();
    }
}
