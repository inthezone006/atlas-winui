using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATLAS.Models;
using Windows.Storage;
using System.Text.Json;

namespace ATLAS.Services
{
    public static class AuthService
    {
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
                CurrentUser = JsonSerializer.Deserialize<User>(userJsonObj as string ?? "");
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
            localSettings.Values["CurrentUser"] = JsonSerializer.Serialize(user);

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
