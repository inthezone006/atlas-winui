using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    public class LinkAnalysisResult
    {
        [JsonPropertyName("is_scam")]
        public bool IsScam { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("details")]
        public Dictionary<string, int>? Details { get; set; }
    }
}