using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ATLAS.Pages
{
    public sealed partial class LoginPage : Page
    {

        private static readonly HttpClient client = new HttpClient();

        private readonly string backendUrl = "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/login";

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";
            LoadingRing.IsActive = true;
            LoginButton.IsEnabled = false;

            var username = UsernameTextBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorTextBlock.Text = "Username and password cannot be empty.";
                LoadingRing.IsActive = false;
                LoginButton.IsEnabled = true;
                return;
            }

            try
            {
                var loginData = new Dictionary<string, string>
                {
                    { "username", username },
                    { "password", password }
                };

                var content = JsonContent.Create(loginData, jsonTypeInfo: JsonContext.Default.DictionaryStringString);
                HttpResponseMessage response = await client.PostAsync(backendUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody, JsonContext.Default.LoginResponse);

                    if (loginResponse?.User != null && loginResponse?.Token != null)
                    {
                        AuthService.Login(loginResponse.User, loginResponse.Token);
                        (Application.Current as App)?.RootFrame?.Navigate(typeof(HomePage));
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var errorDoc = JsonDocument.Parse(errorBody);
                    var errorMessage = errorDoc.RootElement.GetProperty("error").GetString();
                    ErrorTextBlock.Text = errorMessage ?? "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"An error occurred: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
                LoginButton.IsEnabled = true;
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                LoginButton_Click(LoginButton, new RoutedEventArgs());
            }
        }

        private void SignUpLink_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App)?.RootFrame?.Navigate(typeof(SignUpPage), null, new DrillInNavigationTransitionInfo());
        }

        public class LoginResponse
        {
            [JsonPropertyName("user")]
            public User? User { get; set; }
            [JsonPropertyName("token")]
            public string? Token { get; set; }
        }
    }
}
