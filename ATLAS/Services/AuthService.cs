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
        public static string? CurrentUserId => _firebaseClient?.User?.Uid;

        public static event Action? OnLoginStateChanged;

        public static void Initialize()
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                // Added GoogleProvider alongside EmailProvider to authorize social credential handshakes
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

                // Unpack the DisplayName back into its native First and Last name fragments for the dashboard UI
                string fullDisplayName = userCredential.User.Info.DisplayName ?? "User";
                var nameParts = fullDisplayName.Split(' ', 2);
                string firstName = nameParts[0];
                string lastName = nameParts.Length > 1 ? nameParts[1] : "";

                var localUser = new ATLAS.Models.User
                {
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

        // FIX: Added explicit firstName and lastName parameter controls to replace generic placeholder definitions
        public static async Task<bool> RegisterWithEmailAsync(string email, string password, string firstName, string lastName)
        {
            try
            {
                var userCredential = await _firebaseClient!.CreateUserWithEmailAndPasswordAsync(email, password);

                // FIX: Applies the unified name fields straight to the Firebase profile sync context
                string fullName = $"{firstName} {lastName}".Trim();
                await _firebaseClient.User.ChangeDisplayNameAsync(fullName);

                string token = await userCredential.User.GetIdTokenAsync();

                var localUser = new ATLAS.Models.User
                {
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

        // NEW: Standard Standalone Google Identity Provider Login Routine
        public static async Task<bool> SignInWithGoogleAsync()
        {
            try
            {
                if (_firebaseClient == null) return false;

                // Launches an external browser loop back handler to acquire authentication authorization securely
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

                    var localUser = new ATLAS.Models.User
                    {
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

        // NEW: Connects/Links an already logged-in Email/Password account onto a Google SSO credential block
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
                    // Refresh storage mapping parameters
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

        // NEW: Permanently purges user profiles from the cloud authentication server database
        public static async Task<bool> DeleteCurrentUserAccountAsync()
        {
            try
            {
                if (_firebaseClient?.User == null) return false;

                // Calls the library's native destruction routine safely
                await _firebaseClient.User.DeleteAsync();

                // Instantly purges standard system variables and memory registry tokens from the device cache
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

                // 1. Combine inputs to update the core Firebase record profile DisplayName
                string fullName = $"{firstName} {lastName}".Trim();
                await _firebaseClient.User.ChangeDisplayNameAsync(fullName);

                // 2. Synchronize your custom local session memory tracker properties
                CurrentUser.FirstName = firstName;
                CurrentUser.LastName = lastName;

                // 3. Persist the updated configuration cache to the system layout registry
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

                // Executes native client-side password credential rotation
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