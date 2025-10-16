using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
                var requestPayload = new Dictionary<string, string> { { "url", urlToAnalyze } };
                var jsonPayload = JsonSerializer.Serialize(requestPayload, JsonContext.Default.DictionaryStringObject);
                var content = JsonContent.Create(requestPayload, jsonTypeInfo: JsonContext.Default.DictionaryStringString);
                var request = new HttpRequestMessage(HttpMethod.Post, backendUrl) { Content = content };
                if (AuthService.IsLoggedIn && !string.IsNullOrEmpty(AuthService.AuthToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LinkAnalysisResult>(jsonResponse, JsonContext.Default.LinkAnalysisResult);
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

            var storyboard = new Storyboard();

            var fadeAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(400)
            };
            Storyboard.SetTarget(fadeAnimation, ResultsBox);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
            storyboard.Children.Add(fadeAnimation);

            ResultsBox.RenderTransform = new TranslateTransform();
            var slideAnimation = new DoubleAnimation
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideAnimation, (TranslateTransform)ResultsBox.RenderTransform);
            Storyboard.SetTargetProperty(slideAnimation, "Y");
            storyboard.Children.Add(slideAnimation);

            ResultsBox.Visibility = Visibility.Visible;
            storyboard.Begin();
        }

        private void DisplayError(string message)
        {
            ResultsBox.Visibility = Visibility.Visible;
            StatusText.Text = "Analysis Failed";
            StatusIcon.Glyph = "\uE783";
            StatusText.Foreground = new SolidColorBrush(Colors.Red);
            StatusIcon.Foreground = new SolidColorBrush(Colors.Red);
            ExplanationText.Text = message;
        }

        private void UrlTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AnalyzeButton_Click(sender, new RoutedEventArgs());
            }
        }
    }
}