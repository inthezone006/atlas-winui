using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    public class ImageAnalysisResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("analysis")]
        public AnalysisResult? Analysis { get; set; }
    }
}