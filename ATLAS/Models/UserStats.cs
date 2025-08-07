using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    class UserStats
    {
        [JsonPropertyName("total_analyses")]
        public int TotalAnalyses { get; set; }

        [JsonPropertyName("scams_detected")]
        public int ScamsDetected { get; set; }

        [JsonPropertyName("community_submissions")]
        public int CommunitySubmissions { get; set; }
    }
}
