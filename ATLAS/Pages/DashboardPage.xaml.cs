using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace ATLAS.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private static readonly HttpClient client = new HttpClient();

        public DashboardPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (AuthService.IsLoggedIn && AuthService.CurrentUser != null)
            {
                WelcomeTextBlock.Text = $"{GetTimeOfDayGreeting()}, {AuthService.CurrentUser.FirstName}!";

                await LoadUserStats();
            }
            else
            {
                WelcomeTextBlock.Visibility = Visibility.Collapsed;
            }
        }
        private string GetTimeOfDayGreeting()
        {
            int currentHour = DateTime.Now.Hour;

            if (currentHour >= 0 && currentHour < 12)
            {
                return "Good morning";
            }
            else if (currentHour >= 12 && currentHour < 18)
            {
                return "Good afternoon";
            }
            else
            {
                return "Good evening";
            }
        }

        private async Task LoadUserStats()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/stats");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var stats = JsonSerializer.Deserialize<UserStats>(jsonResponse, JsonContext.Default.UserStats);
                    if (stats != null)
                    {
                        TotalAnalysesText.Text = stats.TotalAnalyses.ToString();
                        ScamsDetectedText.Text = stats.ScamsDetected.ToString();
                        SubmissionsText.Text = stats.CommunitySubmissions.ToString();
                    }
                }
            }
            catch (Exception) { }
        }

        private async void SubmitScamButton_Click(object sender, RoutedEventArgs e)
        {
            var submissionTextBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 200,
                PlaceholderText = "Paste the full text of a scam email, text, or message below. This will be reviewed by our team and used to improve ATLAS for everyone."
            };

            var dialog = new ContentDialog
            {
                Title = "Submit a Scam Example",
                Content = submissionTextBox,
                PrimaryButtonText = "Submit for Review",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.RequestedTheme = (this.Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await HandleScamSubmission(submissionTextBox.Text);
            }
        }

        private async Task HandleScamSubmission(string scamText)
        {
            if (string.IsNullOrWhiteSpace(scamText)) return;

            var payload = new Dictionary<string, string> { { "text", scamText } };
            var content = JsonContent.Create(payload, jsonTypeInfo: JsonContext.Default.DictionaryStringString);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/submit-scam");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
            request.Content = content;

            var response = await client.SendAsync(request);
            var confirmationDialog = new ContentDialog
            {
                Title = response.IsSuccessStatusCode ? "Submission Successful" : "Submission Failed",
                Content = response.IsSuccessStatusCode ? "Thank you for your contribution!" : "There was an error submitting your request.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await confirmationDialog.ShowAsync();
        }

        private void StatCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filter)
            {
                (Application.Current as App)?.RootFrame?.Navigate(
                    typeof(HistoryPage),
                    filter,
                    new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }
    }
}