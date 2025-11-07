using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    public class MeResponse
    {
        [JsonPropertyName("user")]
        public User? User { get; set; }
    }
}