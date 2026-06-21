using System;
using System.Collections.Generic;
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

        public async Task SaveScanTelemetryAsync(string analysisType, float resultScore, bool isScam)
        {
            if (!AuthService.IsLoggedIn || (string.IsNullOrEmpty(AuthService.CurrentUserId) && AuthService.CurrentUser == null))
                return;

            try
            {
                // FIX: Fall back to local user caching safely if background network initialization is delayed
                string targetUserId = !string.IsNullOrEmpty(AuthService.CurrentUserId)
                    ? AuthService.CurrentUserId
                    : (AuthService.CurrentUser?.Id ?? AuthService.CurrentUser?.Username ?? "");

                if (string.IsNullOrEmpty(targetUserId)) return;

                string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/analyses";

                var payload = new
                {
                    fields = new
                    {
                        user_id = new { stringValue = targetUserId },
                        analysis_type = new { stringValue = analysisType },
                        result_score = new { doubleValue = (double)resultScore },
                        is_scam = new { booleanValue = isScam },
                        created_at = new { stringValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[Firestore REST Upload Error]: {response.StatusCode} - {errorDetails}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Firestore REST]: Analysis document logged successfully!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firestore Network Crash]: {ex.Message}");
            }
        }

        public async Task<(int TotalScans, int ScamCount, int SafeCount)> GetUserStatsAsync()
        {
            if (!AuthService.IsLoggedIn || (string.IsNullOrEmpty(AuthService.CurrentUserId) && AuthService.CurrentUser == null))
                return (0, 0, 0);

            try
            {
                // FIX: Unify the User ID selector token target to ensure matching payload variables aren't null
                string targetUserId = !string.IsNullOrEmpty(AuthService.CurrentUserId)
                    ? AuthService.CurrentUserId
                    : (AuthService.CurrentUser?.Username ?? "");

                if (string.IsNullOrEmpty(targetUserId)) return (0, 0, 0);

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
                                value = new { stringValue = AuthService.CurrentUserId } // FIX: Points to populated string reference variable token
                            }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(queryPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[Firestore REST Query Error]: {errorDetails}");
                    return (0, 0, 0);
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                int total = 0;
                int scams = 0;
                int safe = 0;

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("document", out var documentElement))
                    {
                        if (documentElement.TryGetProperty("fields", out var fieldsElement))
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
            if (!AuthService.IsLoggedIn || (string.IsNullOrEmpty(AuthService.CurrentUserId) && AuthService.CurrentUser == null))
                return historyList;

            try
            {
                // FIX: Unify the User ID selector token target for history lookups
                string targetUserId = !string.IsNullOrEmpty(AuthService.CurrentUserId)
                    ? AuthService.CurrentUserId
                    : (AuthService.CurrentUser?.Username ?? "");

                if (string.IsNullOrEmpty(targetUserId)) return historyList;

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
                                value = new { stringValue = AuthService.CurrentUserId } // FIX: Points to populated string reference variable token
                            }
                        },
                        order = new[]
                        {
                            new { field = new { fieldPath = "created_at" }, direction = "DESCENDING" }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(queryPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return historyList;

                string responseJson = await response.Content.ReadAsStringAsync();
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

                        string type = typeProp.TryGetProperty("stringValue", out var tVal) ? tVal.GetString() ?? "Unknown" : "Unknown";
                        double score = scoreProp.TryGetProperty("doubleValue", out var sVal) ? sVal.GetDouble() : 0.0;
                        bool isScam = scamProp.TryGetProperty("booleanValue", out var bVal) && bVal.GetBoolean();
                        string rawTime = timeProp.TryGetProperty("stringValue", out var dVal) ? dVal.GetString() ?? "" : "";

                        DateTime.TryParse(rawTime, out DateTime parsedTime);

                        historyList.Add(new Models.AnalysisHistoryItem
                        {
                            AnalysisType = type,
                            Score = (float)score,
                            IsScam = isScam,
                            CreatedAt = parsedTime.ToLocalTime().ToString("g")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"History query processing fault: {ex.Message}");
            }
            return historyList;
        }
    }
}