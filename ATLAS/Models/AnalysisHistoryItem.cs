using System;
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

        [JsonPropertyName("result_score")]
        public float Score { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        // FIX: Provide a concrete DateTime structure for robust timeline sorting loops
        public DateTime SortingDate { get; set; }

        public string ScoreDisplay => $"Score: {Score:F2}/10";
        public string ScamStatusDisplay => IsScam ? "Status: Threat Detected" : "Status: Verified Clear";
    }
}