using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Net.Http.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media.Animation;

namespace ATLAS.Pages
{
    public sealed partial class TextAnalysisPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string backendUrl = "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/analyze";
        private string? lastAnalyzedText;
        public TextAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            lastAnalyzedText = InputTextBox.Text;
            var inputText = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(inputText)) return;

            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;

            try
            {
                var requestPayload = new Dictionary<string, string> { { "text", lastAnalyzedText } };
                var jsonPayload = JsonSerializer.Serialize(requestPayload, JsonContext.Default.DictionaryStringObject);
                var content = JsonContent.Create(requestPayload, jsonTypeInfo: JsonContext.Default.DictionaryStringString);
                var request = new HttpRequestMessage(HttpMethod.Post, backendUrl) { Content = content };

                if (AuthService.IsLoggedIn)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AnalysisResult>(jsonResponse, JsonContext.Default.AnalysisResult);
                    if (result != null)
                    {
                        DisplayResults(result);
                    }
                    else
                    {
                        ResultsBox.Visibility = Visibility.Visible;
                        StatusText.Text = "Error";
                        ScoreText.Text = "-";
                        ExplanationText.Text = "Received an invalid response from the server.";
                    }
                }
                else
                {
                    ResultsBox.Visibility = Visibility.Visible;
                    StatusText.Text = "Error";
                    ScoreText.Text = "-";
                    ExplanationText.Text = "Could not get a response from the server. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                ResultsBox.Visibility = Visibility.Visible;
                StatusText.Text = "Connection Error";
                ScoreText.Text = "-";
                ExplanationText.Text = $"Could not connect to the analysis service. Details: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private async void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.IsLoggedIn)
            {
                if (string.IsNullOrWhiteSpace(lastAnalyzedText))
                {
                    NotificationService.Show("No content to submit.", InfoBarSeverity.Warning);
                    return;
                }

                var payload = new Dictionary<string, string> { { "text", lastAnalyzedText } };
                var content = JsonContent.Create(payload, jsonTypeInfo: JsonContext.Default.DictionaryStringString);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/submit-scam");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                request.Content = content;

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseDoc = JsonDocument.Parse(responseBody);
                var message = responseDoc.RootElement.GetProperty(response.IsSuccessStatusCode ? "message" : "error").GetString();

                NotificationService.Show(message ?? "...", response.IsSuccessStatusCode ? InfoBarSeverity.Success : InfoBarSeverity.Error);
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Create an Account to Contribute",
                    Content = "Help improve ATLAS by creating a free account to submit new scam examples for review.",
                    PrimaryButtonText = "Sign Up",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    (Application.Current as App)?.RootFrame?.Navigate(typeof(SignUpPage));
                }
            }
        }

        private void DisplayResults(AnalysisResult? result)
        {
            if (result == null) return;

            if (result.IsScam == true)
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusText.Text = "This text appears to be a scam.";
                StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                StatusIcon.Glyph = "\uE73E";
                StatusText.Text = "This text appears to be safe.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }

            ScoreText.Text = $"{result.Score:F2}/10";
            ExplanationText.Text = result.Explanation;
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
    }
}