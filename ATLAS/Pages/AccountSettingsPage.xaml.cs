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
            ErrorTextBlock.Text = "";
            LoadingRing.IsActive = true;

            bool success = await AuthService.SignInWithGoogleAsync();
            if (success)
            {
                Frame.Navigate(typeof(DashboardPage));
            }
            else
            {
                ErrorTextBlock.Text = "Google authentication aborted or timed out.";
            }
            LoadingRing.IsActive = false;
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
                    ErrorTextBlock.Text = "Failed to unlink account. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Error: {ex.Message}";
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
            ErrorTextBlock.Text = "";

            var oldUsername = OldUsernameTextBox.Text;
            var newUsername = NewUsernameTextBox.Text;

            if (string.IsNullOrWhiteSpace(oldUsername) || string.IsNullOrWhiteSpace(newUsername))
            {
                ErrorTextBlock.Text = "Both username fields are required.";
                return;
            }

            if (AuthService.IsLoggedIn && oldUsername != AuthService.CurrentUser?.Username)
            {
                ErrorTextBlock.Text = "The 'Current Username' you entered is incorrect.";
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
            ContentDialog confirmationDialog = new ContentDialog
            {
                Title = "Delete Account permanently?",
                Content = "This action is irreversible and wipes your scan metrics profile.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await confirmationDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                bool deleted = await AuthService.DeleteCurrentUserAccountAsync();
                if (deleted)
                {
                    // Escape back to registration view layout upon account purge
                    Frame.Navigate(typeof(SignUpPage));
                }
                else
                {
                    // Show error notice to re-authenticate if token expired
                }
            }
        }

        private async Task UpdateUserSettings(string url, object payload, string successMessage)
        {
            ErrorTextBlock.Text = "";
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
                    ErrorTextBlock.Text = responseDoc.RootElement.GetProperty("error").GetString();
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }
    }
}