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
            }
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            var payload = new { first_name = FirstNameTextBox.Text, last_name = LastNameTextBox.Text };
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

            var payload = new { old_username = oldUsername, new_username = newUsername };
            await UpdateUserSettings(
                "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/username",
                payload,
                "Username updated successfully!");
        }

        private async void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            var payload = new { old_password = OldPasswordBox.Password, new_password = NewPasswordBox.Password };
            await UpdateUserSettings(
                "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/password",
                payload,
                "Password updated successfully!");
        }

        private async Task UpdateUserSettings(string url, object payload, string successMessage)
        {
            StatusTextBlock.Text = "";
            LoadingRing.IsActive = true;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
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