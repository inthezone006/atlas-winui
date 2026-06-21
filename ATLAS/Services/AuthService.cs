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

        public static ATLAS.Models.User? CurrentUser { get; private set; }
        public static string? AuthToken { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;

        // FIX: Falls back immediately to cached storage data if the network handshake is still completing
        public static string? CurrentUserId => _firebaseClient?.User?.Uid ?? CurrentUser?.Id;

        public static event Action? OnLoginStateChanged;

        public static void Initialize()
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider(),
                    new GoogleProvider()
                }
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

                string fullDisplayName = userCredential.User.Info.DisplayName ?? "User";
                var nameParts = fullDisplayName.Split(' ', 2);
                string firstName = nameParts[0];
                string lastName = nameParts.Length > 1 ? nameParts[1] : "";

                // FIX: Map the unique Firebase Uid onto the local model instance descriptor
                var localUser = new ATLAS.Models.User
                {
                    Id = userCredential.User.Uid,
                    Username = userCredential.User.Info.Email,
                    FirstName = firstName,
                    LastName = lastName
                };

                Login(localUser, token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RegisterWithEmailAsync(string email, string password, string firstName, string lastName)
        {
            try
            {
                var userCredential = await _firebaseClient!.CreateUserWithEmailAndPasswordAsync(email, password);

                string fullName = $"{firstName} {lastName}".Trim();
                await _firebaseClient.User.ChangeDisplayNameAsync(fullName);

                string token = await userCredential.User.GetIdTokenAsync();

                // FIX: Map the unique Firebase Uid onto the local model instance descriptor
                var localUser = new ATLAS.Models.User
                {
                    Id = userCredential.User.Uid,
                    Username = email,
                    FirstName = firstName,
                    LastName = lastName
                };

                Login(localUser, token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SignInWithGoogleAsync()
        {
            try
            {
                if (_firebaseClient == null) return false;

                var userCredential = await _firebaseClient.SignInWithRedirectAsync(FirebaseProviderType.Google, uri =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = uri,
                        UseShellExecute = true
                    });
                    return Task.FromResult<string>(null!);
                });

                if (userCredential?.User != null)
                {
                    string token = await userCredential.User.GetIdTokenAsync();
                    string fullDisplayName = userCredential.User.Info.DisplayName ?? "Google User";
                    var nameParts = fullDisplayName.Split(' ', 2);

                    // FIX: Map the unique Firebase Uid onto the local model instance descriptor
                    var localUser = new ATLAS.Models.User
                    {
                        Id = userCredential.User.Uid,
                        Username = userCredential.User.Info.Email,
                        FirstName = nameParts[0],
                        LastName = nameParts.Length > 1 ? nameParts[1] : "",
                        GoogleId = userCredential.User.Uid
                    };

                    Login(localUser, token);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google authentication failure: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> LinkAccountWithGoogleAsync()
        {
            try
            {
                if (_firebaseClient?.User == null) return false;

                var userCredential = await _firebaseClient.User.LinkWithRedirectAsync(FirebaseProviderType.Google, uri =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = uri,
                        UseShellExecute = true
                    });
                    return Task.FromResult<string>(null!);
                });

                if (userCredential?.User != null && CurrentUser != null)
                {
                    CurrentUser.GoogleId = userCredential.User.Uid;
                    CurrentUser.Id = userCredential.User.Uid;
                    Login(CurrentUser, AuthToken!);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Google connection link failure: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> DeleteCurrentUserAccountAsync()
        {
            try
            {
                if (_firebaseClient?.User == null) return false;
                await _firebaseClient.User.DeleteAsync();
                Logout();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Account closure operation error: {ex.Message}");
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
            try
            {
                if (_firebaseClient?.User != null)
                {
                    _firebaseClient.SignOut();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logout Exception Controlled]: {ex.Message}");
            }
            CurrentUser = null;
            AuthToken = null;

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("AuthToken");
            localSettings.Values.Remove("CurrentUser");

            OnLoginStateChanged?.Invoke();
        }

        public static async Task<bool> UpdateUserNamesAsync(string firstName, string lastName)
        {
            try
            {
                if (_firebaseClient?.User == null || CurrentUser == null) return false;

                string fullName = $"{firstName} {lastName}".Trim();
                await _firebaseClient.User.ChangeDisplayNameAsync(fullName);

                CurrentUser.FirstName = firstName;
                CurrentUser.LastName = lastName;

                Login(CurrentUser, AuthToken!);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Update Names Error]: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> UpdateUserPasswordAsync(string newPassword)
        {
            try
            {
                if (_firebaseClient?.User == null) return false;
                await _firebaseClient.User.ChangePasswordAsync(newPassword);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Update Password Error]: {ex.Message}");
                return false;
            }
        }
    }
}