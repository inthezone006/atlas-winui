using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Json;
using Microsoft.Web.WebView2.Core;
using System.Text.RegularExpressions;

namespace ATLAS.Pages
{
    public sealed partial class AccountSettingsPage : Page
    {
        private static readonly HttpClient client = new HttpClient();

        public AccountSettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!AuthService.IsLoggedIn || AuthService.CurrentUser == null)
            {
                (Window.Current.Content as Frame)?.Navigate(typeof(LoginPage));
            }
            else
            {
                FirstNameTextBox.Text = AuthService.CurrentUser.FirstName;
                LastNameTextBox.Text = AuthService.CurrentUser.LastName;
                UpdateLinkStatusUI();
            }
        }

        private void UpdateLinkStatusUI()
        {
            if (AuthService.IsLoggedIn && AuthService.CurrentUser != null)
            {
                if (string.IsNullOrEmpty(AuthService.CurrentUser.GoogleId))
                {
                    LinkStatusText.Text = "Your account is not linked to Google.";
                    LinkGoogleButton.Visibility = Visibility.Visible;
                    UnlinkGoogleButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LinkStatusText.Text = "Your account is linked to Google.";
                    LinkGoogleButton.Visibility = Visibility.Collapsed;
                    UnlinkGoogleButton.Visibility = Visibility.Visible;
                }
            }
        }

        private async void LinkGoogle_Click(object sender, RoutedEventArgs e)
        {
            var webView = new WebView2 { Height = 600, Width = 400 };
            await webView.EnsureCoreWebView2Async();

            var dialog = new ContentDialog
            {
                Title = "Link with Google",
                Content = webView,
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            webView.NavigationStarting += async (s, args) =>
            {
                string url = args.Uri.ToString();

                if (url.StartsWith("https://www.atlasprotection.app/account?linked=true"))
                {
                    args.Cancel = true;
                    dialog.Hide();

                    AuthService.Logout();
                    (Application.Current as App)?.RootFrame?.Navigate(typeof(HomePage), null, new SuppressNavigationTransitionInfo());
                    NotificationService.Show("Account linked! Please log in again to sync changes.", InfoBarSeverity.Success);
                }
            };

            string authUrl = $"https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/auth/google?login_token={AuthService.AuthToken}";
            webView.CoreWebView2.Navigate(authUrl);

            await dialog.ShowAsync();
        }

        private async void UnlinkGoogle_Click(object sender, RoutedEventArgs e)
        {
            LoadingRing.IsActive = true;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/unlink-google");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    AuthService.Logout();
                    (Application.Current as App)?.RootFrame?.Navigate(typeof(HomePage), null, new SuppressNavigationTransitionInfo());
                    NotificationService.Show("Google account unlinked. You have been signed out.", InfoBarSeverity.Success);
                }
                else
                {
                    StatusTextBlock.Text = "Failed to unlink account. Please try again.";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            var payload = new Dictionary<string, string>
            {
                { "first_name", FirstNameTextBox.Text },
                { "last_name", LastNameTextBox.Text }
            };
            await UpdateUserSettings(
                "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/name",
                payload,
                "Name updated successfully!");
        }

        private async void SaveUsername_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "";

            var oldUsername = OldUsernameTextBox.Text;
            var newUsername = NewUsernameTextBox.Text;

            if (string.IsNullOrWhiteSpace(oldUsername) || string.IsNullOrWhiteSpace(newUsername))
            {
                StatusTextBlock.Text = "Both username fields are required.";
                return;
            }

            if (AuthService.IsLoggedIn && oldUsername != AuthService.CurrentUser?.Username)
            {
                StatusTextBlock.Text = "The 'Current Username' you entered is incorrect.";
                return;
            }

            var payload = new Dictionary<string, string>
            {
                { "old_username", oldUsername },
                { "new_username", newUsername }
            };
            await UpdateUserSettings(
                "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/username",
                payload,
                "Username updated successfully!");
        }

        private async void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            var payload = new Dictionary<string, string>
            {
                { "old_password", OldPasswordBox.Password },
                { "new_password", NewPasswordBox.Password }
            };
            await UpdateUserSettings(
                "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/password",
                payload,
                "Password updated successfully!");
        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Are you absolutely sure?",
                Content = "This action is permanent. All your data will be deleted and this cannot be undone. Do you wish to proceed?",
                PrimaryButtonText = "Delete My Account",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            dialog.RequestedTheme = (this.Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                StatusTextBlock.Text = "";
                LoadingRing.IsActive = true;
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/delete");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        AuthService.Logout();
                        (Application.Current as App)?.RootFrame?.Navigate(typeof(HomePage), null, new SuppressNavigationTransitionInfo());
                        NotificationService.Show("Your account has been successfully deleted.", InfoBarSeverity.Success);
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseDoc = JsonDocument.Parse(responseBody);
                        StatusTextBlock.Text = responseDoc.RootElement.GetProperty("error").GetString();
                    }
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = $"An error occurred: {ex.Message}";
                }
                finally
                {
                    LoadingRing.IsActive = false;
                }
            }
        }

        private async Task UpdateUserSettings(string url, object payload, string successMessage)
        {
            StatusTextBlock.Text = "";
            LoadingRing.IsActive = true;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = JsonContent.Create(payload, jsonTypeInfo: JsonContext.Default.DictionaryStringString)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    AuthService.Logout();
                    (Application.Current as App)?.RootFrame?.Navigate(typeof(HomePage), null, new SuppressNavigationTransitionInfo());
                    NotificationService.Show($"{successMessage} You have been signed out for security.", InfoBarSeverity.Success);
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseDoc = JsonDocument.Parse(responseBody);
                    StatusTextBlock.Text = responseDoc.RootElement.GetProperty("error").GetString();
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }
    }
}