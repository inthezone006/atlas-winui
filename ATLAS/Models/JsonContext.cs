using ATLAS.Models;
using ATLAS.Pages;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static ATLAS.Pages.LoginPage;

namespace ATLAS.Models
{
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
    )]
    [JsonSerializable(typeof(User))]
    [JsonSerializable(typeof(AnalysisHistoryItem))]
    [JsonSerializable(typeof(AnalysisResult))]
    [JsonSerializable(typeof(ImageAnalysisResponse))]
    [JsonSerializable(typeof(LinkAnalysisResult))]
    [JsonSerializable(typeof(UserStats))]
    [JsonSerializable(typeof(LoginResponse))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(List<AnalysisHistoryItem>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(TranscriptionResponse))]
    [JsonSerializable(typeof(MeResponse))]

    internal partial class JsonContext : JsonSerializerContext
    {
    }
}