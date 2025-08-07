using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    public class AnalysisResult
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("is_scam")]
        public bool IsScam { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }
    }
}
