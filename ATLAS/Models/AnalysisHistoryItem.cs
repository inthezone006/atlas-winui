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

        // FIX: Clean backend formatting helper to bypass XAML binding limitations
        public string ScoreDisplay => $"Score: {Score:F2}/10";

        // Bonus helper to render scan status nicely
        public string ScamStatusDisplay => IsScam ? "Status: Threat Detected" : "Status: Verified Clear";
    }
}