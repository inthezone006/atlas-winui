using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ATLAS.Services
{
    public class FirestoreTelemetryService
    {
        private static FirestoreTelemetryService? _instance;
        private static readonly object _lock = new object();
        private readonly HttpClient _httpClient;

        public static FirestoreTelemetryService Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FirestoreTelemetryService();
                    }
                    return _instance;
                }
            }
        }

        private FirestoreTelemetryService()
        {
            _httpClient = new HttpClient();
        }
        public async Task SaveScanTelemetryAsync(string analysisType, float resultScore, bool isScam, string scannedContent)
        {
            if (!AuthService.IsLoggedIn || string.IsNullOrEmpty(AuthService.CurrentUserId))
                return;

            try
            {
                string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/analyses";

                var payload = new
                {
                    fields = new
                    {
                        user_id = new { stringValue = AuthService.CurrentUserId },
                        analysis_type = new { stringValue = analysisType },
                        result_score = new { doubleValue = (double)resultScore },
                        is_scam = new { booleanValue = isScam },
                        created_at = new { stringValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        scanned_content = new { stringValue = scannedContent ?? string.Empty }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firestore Network Crash]: {ex.Message}");
            }
        }

        public async Task<(int TotalScans, int ScamCount, int SafeCount)> GetUserStatsAsync()
        {
            if (!AuthService.IsLoggedIn || string.IsNullOrEmpty(AuthService.CurrentUserId))
                return (0, 0, 0);

            try
            {
                string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents:runQuery";

                var queryPayload = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "analyses" } },
                        where = new
                        {
                            fieldFilter = new
                            {
                                field = new { fieldPath = "user_id" },
                                op = "EQUAL",
                                value = new { stringValue = AuthService.CurrentUserId }
                            }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(queryPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return (0, 0, 0);

                string responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                int total = 0;
                int scams = 0;
                int safe = 0;

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("document", out var documentElement) &&
                        documentElement.TryGetProperty("fields", out var fieldsElement))
                    {
                        total++;
                        if (fieldsElement.TryGetProperty("is_scam", out var scamProp) &&
                            scamProp.TryGetProperty("booleanValue", out var boolVal))
                        {
                            if (boolVal.GetBoolean()) scams++;
                            else safe++;
                        }
                    }
                }

                return (total, scams, safe);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving Firestore statistics: {ex.Message}");
                return (0, 0, 0);
            }
        }

        public async Task<List<Models.AnalysisHistoryItem>> GetUserHistoryAsync()
        {
            var historyList = new List<Models.AnalysisHistoryItem>();
            if (!AuthService.IsLoggedIn || string.IsNullOrEmpty(AuthService.CurrentUserId))
            {
                return historyList;
            }

            try
            {
                string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents:runQuery";

                var queryPayload = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "analyses" } },
                        where = new
                        {
                            fieldFilter = new
                            {
                                field = new { fieldPath = "user_id" },
                                op = "EQUAL",
                                value = new { stringValue = AuthService.CurrentUserId }
                            }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(queryPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                string responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return historyList;
                }

                using var doc = JsonDocument.Parse(responseJson);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("document", out var documentElement) &&
                        documentElement.TryGetProperty("fields", out var fieldsElement))
                    {
                        fieldsElement.TryGetProperty("analysis_type", out var typeProp);
                        fieldsElement.TryGetProperty("result_score", out var scoreProp);
                        fieldsElement.TryGetProperty("is_scam", out var scamProp);
                        fieldsElement.TryGetProperty("created_at", out var timeProp);
                        fieldsElement.TryGetProperty("scanned_content", out var contentProp);

                        string scannedContentValue = contentProp.TryGetProperty("stringValue", out var cVal) ? cVal.GetString() ?? "N/A" : "N/A";

                        string type = typeProp.TryGetProperty("stringValue", out var tVal) ? tVal.GetString() ?? "Unknown" : "Unknown";

                        double score = 0.0;
                        if (scoreProp.TryGetProperty("doubleValue", out var sDouble)) score = sDouble.GetDouble();
                        else if (scoreProp.TryGetProperty("integerValue", out var sInt) && double.TryParse(sInt.GetString(), out double parsedScore)) score = parsedScore;

                        bool isScam = scamProp.TryGetProperty("booleanValue", out var bVal) && bVal.GetBoolean();
                        string rawTime = timeProp.TryGetProperty("stringValue", out var dVal) ? dVal.GetString() ?? "" : "";

                        string formattedDisplayDate = rawTime;
                        DateTime sortingDateValue = DateTime.MinValue;

                        if (DateTimeOffset.TryParse(rawTime, out DateTimeOffset parsedOffset))
                        {
                            var localTime = parsedOffset.ToLocalTime();
                            formattedDisplayDate = localTime.ToString("g");
                            sortingDateValue = localTime.DateTime;
                        }

                        historyList.Add(new Models.AnalysisHistoryItem
                        {
                            AnalysisType = type,
                            Score = (float)score,
                            IsScam = isScam,
                            CreatedAt = formattedDisplayDate,
                            SortingDate = sortingDateValue,
                            ScannedContent = scannedContentValue
                        });
                    }
                }

                var sortedList = historyList.OrderByDescending(item => item.SortingDate).ToList();
                return sortedList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG TELEMETRY HISTORY CRASH]: {ex}");
            }
            return historyList;
        }
    }
}