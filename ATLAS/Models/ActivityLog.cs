using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ATLAS.Models
{
    public class ActivityLog
    {
        [JsonPropertyName("approved_on")]
        public string ApprovedOn { get; set; }

        [JsonPropertyName("approved_user")]
        public string ApprovedUser { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
