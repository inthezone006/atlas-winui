using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    public class AnalysisHistoryItem
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("analysis_type")]
        public string? AnalysisType { get; set; }

        [JsonPropertyName("is_scam")]
        public bool IsScam { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }
}