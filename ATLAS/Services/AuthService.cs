using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATLAS.Models;
using Windows.Storage;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ATLAS.Services
{
    public static class AuthService
    {
        private static readonly HttpClient client = new HttpClient();
        public static User? CurrentUser { get; private set; }
        public static string? AuthToken { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static event Action? OnLoginStateChanged;

        public static void TryLoadUserFromStorage()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue("AuthToken", out var tokenObj) &&
                localSettings.Values.TryGetValue("CurrentUser", out var userJsonObj))
            {
                var token = tokenObj as string;
                if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
                {
                    Logout();
                    return;
                }

                AuthToken = token;
                CurrentUser = JsonSerializer.Deserialize<User>(userJsonObj as string ?? "", JsonContext.Default.User);
            }
        }

        public static async Task<bool> LoginWithTokenAsync(string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var meResponse = JsonSerializer.Deserialize<MeResponse>(jsonResponse, JsonContext.Default.MeResponse);

                if (meResponse?.User != null)
                {
                    Login(meResponse.User, token);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTokenExpired(string token)
        {
            try
            {
                var payload = token.Split('.')[1];
                var jsonBytes = Convert.FromBase64String(AddPadding(payload));
                var json = Encoding.UTF8.GetString(jsonBytes);

                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("exp", out var expClaim) && expClaim.TryGetInt64(out var expSeconds))
                    {
                        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                        return expirationTime < DateTimeOffset.UtcNow;
                    }
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        private static string AddPadding(string base64)
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');
            return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        }


        public static void Login(User user, string token)
        {
            CurrentUser = user;
            AuthToken = token;
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["AuthToken"] = token;
            localSettings.Values["CurrentUser"] = JsonSerializer.Serialize(user, JsonContext.Default.User);

            OnLoginStateChanged?.Invoke();
        }

        public static void Logout()
        {
            CurrentUser = null;
            AuthToken = null;
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("AuthToken");
            localSettings.Values.Remove("CurrentUser");
            OnLoginStateChanged?.Invoke();
        }
    }
}
