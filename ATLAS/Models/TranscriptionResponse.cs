using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    public class TranscriptionResponse
    {
        [JsonPropertyName("transcript")]
        public string? Transcript { get; set; }
    }
}