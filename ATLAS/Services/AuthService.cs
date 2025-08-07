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
        public static User CurrentUser { get; private set; }
        public static string AuthToken { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static event Action OnLoginStateChanged;

        public static void TryLoadUserFromStorage()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue("AuthToken", out object token) &&
                localSettings.Values.TryGetValue("CurrentUser", out object userJson))
            {
                AuthToken = token as string;
                CurrentUser = JsonSerializer.Deserialize<User>(userJson as string);
            }
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
