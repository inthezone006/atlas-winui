using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
            }
            else
            {
                LogStatusText.Text = "You must be logged in to view the dashboard.";
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
                else
                {
                    LogStatusText.Text = "Failed to load user statistics.";
                }
            }
            catch (Exception ex)
            {
                LogStatusText.Text = $"Error: {ex.Message}";
            }
        }
    }
}