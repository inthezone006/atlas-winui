using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            if (!AuthService.IsLoggedIn)
            {
                // Redirect if not logged in
                (Application.Current as App)?.RootFrame.Navigate(typeof(LoginPage));
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
            await UpdateUserSettings("https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/name", payload);
        }

        private async void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            var payload = new { old_password = OldPasswordBox.Password, new_password = NewPasswordBox.Password };
            await UpdateUserSettings("https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/password", payload);
        }

        private async Task UpdateUserSettings(string url, object payload)
        {
            StatusTextBlock.Text = "Saving...";
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseDoc = JsonDocument.Parse(responseBody);
                var message = responseDoc.RootElement.GetProperty(response.IsSuccessStatusCode ? "message" : "error").GetString();

                StatusTextBlock.Text = message;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
            }
        }
    }
}