using System;
using System.Text;
using System.Threading.Tasks;
using ATLAS.Models;
using Windows.Storage;
using System.Text.Json;
using Firebase.Auth;
using Firebase.Auth.Providers;

namespace ATLAS.Services
{
    public static class AuthService
    {
        private static FirebaseAuthClient? _firebaseClient;

        // Force the app to use your custom User class everywhere to satisfy UI bindings
        public static ATLAS.Models.User? CurrentUser { get; private set; }
        public static string? AuthToken { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;
        public static string? CurrentUserId => _firebaseClient?.User?.Uid;

        public static event Action? OnLoginStateChanged;

        public static void Initialize()
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                Providers = new[] { new EmailProvider() }
            };

            _firebaseClient = new FirebaseAuthClient(config);
        }

        public static void TryLoadUserFromStorage()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue("AuthToken", out var tokenObj) &&
                localSettings.Values.TryGetValue("CurrentUser", out var userJsonObj))
            {
                AuthToken = tokenObj as string;
                var jsonStr = userJsonObj as string ?? "";

                // Fixed JSON parsing logic parameters
                CurrentUser = JsonSerializer.Deserialize<ATLAS.Models.User>(jsonStr, ATLAS.Models.JsonContext.Default.User);
                OnLoginStateChanged?.Invoke();
            }
        }

        public static async Task<bool> LoginWithEmailAsync(string email, string password)
        {
            try
            {
                var userCredential = await _firebaseClient!.SignInWithEmailAndPasswordAsync(email, password);
                string token = await userCredential.User.GetIdTokenAsync();

                var localUser = new ATLAS.Models.User
                {
                    Username = userCredential.User.Info.Email,
                    FirstName = userCredential.User.Info.DisplayName ?? "User",
                    LastName = ""
                };

                Login(localUser, token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RegisterWithEmailAsync(string email, string password)
        {
            try
            {
                var userCredential = await _firebaseClient!.CreateUserWithEmailAndPasswordAsync(email, password);
                string token = await userCredential.User.GetIdTokenAsync();

                var localUser = new ATLAS.Models.User { Username = email, FirstName = "User", LastName = "" };
                Login(localUser, token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Login(ATLAS.Models.User user, string token)
        {
            CurrentUser = user;
            AuthToken = token;

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["AuthToken"] = token;
            localSettings.Values["CurrentUser"] = JsonSerializer.Serialize(user, ATLAS.Models.JsonContext.Default.User);

            OnLoginStateChanged?.Invoke();
        }

        public static void Logout()
        {
            _firebaseClient?.SignOut();
            CurrentUser = null;
            AuthToken = null;

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("AuthToken");
            localSettings.Values.Remove("CurrentUser");

            OnLoginStateChanged?.Invoke();
        }
    }
}