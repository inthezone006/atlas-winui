using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            if (AuthService.IsLoggedIn)
            {
                await LoadUserStats();
                await LoadSubmissionHistory();
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
                    var stats = JsonSerializer.Deserialize<UserStats>(jsonResponse);
                    TotalAnalysesText.Text = stats.TotalAnalyses.ToString();
                    ScamsDetectedText.Text = stats.ScamsDetected.ToString();
                    SubmissionsText.Text = stats.CommunitySubmissions.ToString();
                }
            }
            catch (Exception) { /* Handle error */ }
        }

        private async Task LoadSubmissionHistory()
        {
            HistoryStatusText.Text = "Loading history...";
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/dashboard/log");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var history = JsonSerializer.Deserialize<List<ActivityLog>>(jsonResponse);
                    HistoryItemsRepeater.ItemsSource = history;
                    HistoryStatusText.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    HistoryStatusText.Text = "No submission history found.";
                }
                else
                {
                    HistoryStatusText.Text = "Failed to load submission history.";
                }
            }
            catch (Exception ex)
            {
                HistoryStatusText.Text = $"Error: {ex.Message}";
            }
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
                XamlRoot = this.XamlRoot
            };

            dialog.RequestedTheme = (this.Content as FrameworkElement).ActualTheme;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await HandleScamSubmission(submissionTextBox.Text);
            }
        }

        private async Task HandleScamSubmission(string scamText)
        {
            if (string.IsNullOrWhiteSpace(scamText)) return;

            var payload = new { text = scamText };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

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
    }
}