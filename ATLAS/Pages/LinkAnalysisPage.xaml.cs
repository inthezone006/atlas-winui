using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ATLAS.Pages
{
    public sealed partial class LinkAnalysisPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string backendUrl = "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/analyze-link";

        public LinkAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var urlToAnalyze = UrlTextBox.Text;
            if (string.IsNullOrWhiteSpace(urlToAnalyze)) return;

            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;

            try
            {
                var requestPayload = new { url = urlToAnalyze };
                var jsonPayload = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, backendUrl) { Content = content };
                if (AuthService.IsLoggedIn)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LinkAnalysisResult>(jsonResponse);
                    DisplayResults(result);
                }
                else
                {
                    DisplayError("Could not get a response from the server.");
                }
            }
            catch (Exception ex)
            {
                DisplayError($"An error occurred: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void DisplayResults(LinkAnalysisResult? result)
        {
            if (result == null)
            {
                DisplayError("Failed to parse the analysis result.");
                return;
            }

            if (result.IsScam)
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusText.Text = "This URL appears to be malicious.";
                StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                StatusIcon.Glyph = "\uE73E";
                StatusText.Text = "This URL appears to be safe.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }

            ExplanationText.Text = result.Explanation;

            if (result.Details != null)
            {
                HarmlessCountText.Text = result.Details.GetValueOrDefault("harmless", 0).ToString();
                SuspiciousCountText.Text = result.Details.GetValueOrDefault("suspicious", 0).ToString();
                MaliciousCountText.Text = result.Details.GetValueOrDefault("malicious", 0).ToString();
            }

            ResultsBox.Visibility = Visibility.Visible;
        }

        private void DisplayError(string message)
        {
            ResultsBox.Visibility = Visibility.Visible;
            StatusText.Text = "Analysis Failed";
            StatusIcon.Glyph = "\uE783";
            StatusText.Foreground = new SolidColorBrush(Colors.Red);
            ExplanationText.Text = message;
        }
    }
}