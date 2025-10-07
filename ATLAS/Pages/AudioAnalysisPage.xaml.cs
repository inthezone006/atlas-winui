using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ATLAS.Pages
{
    public sealed partial class AudioAnalysisPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private StorageFile? selectedAudioFile;
        private string? lastAnalyzedText;

        public AudioAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            var window = (Application.Current as App)?._window as MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add(".wav");
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".m4a");

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                selectedAudioFile = file;
                SelectedFileNameText.Text = file.Name;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAudioFile == null) return;
            lastAnalyzedText = null;
            TranscriptBox.Visibility = Visibility.Collapsed;
            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;
            SelectFileButton.IsEnabled = false;

            try
            {
                var transcribeTask = TranscribeAudioAsync(selectedAudioFile);
                var analyzeTask = AnalyzeAudioAsync(selectedAudioFile);

                await Task.WhenAll(transcribeTask, analyzeTask);

                var transcript = await transcribeTask;
                var analysisResult = await analyzeTask;
                lastAnalyzedText = transcript;
                DisplayTranscript(transcript);
                if (analysisResult != null)
                {
                    DisplayAnalysis(analysisResult);
                }
                else
                {
                    DisplayError("Could not get an analysis from the server.");
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
            }
        }

        private static async Task<string> TranscribeAudioAsync(StorageFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = await file.OpenStreamForReadAsync();
            content.Add(new StreamContent(stream), "audio", file.Name);

            var response = await client.PostAsync(
                "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/transcribe", content);
            if (!response.IsSuccessStatusCode) return "Failed to transcribe audio.";

            var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return jsonDoc.RootElement.GetProperty("transcript").GetString() ?? "Transcript not found.";
        }

        private static async Task<AnalysisResult?> AnalyzeAudioAsync(StorageFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = await file.OpenStreamForReadAsync();
            content.Add(new StreamContent(stream), "audio", file.Name);

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/analyze-audio")
            { Content = content };
            if (AuthService.IsLoggedIn)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
            }

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AnalysisResult>(jsonResponse);
        }

        private void DisplayTranscript(string transcript)
        {
            TranscriptText.Text = transcript;
            TranscriptBox.Visibility = Visibility.Visible;
        }

        private void DisplayError(string message)
        {
            ResultsBox.Visibility = Visibility.Visible;
            StatusText.Text = "Error";
            ScoreText.Text = "-";
            ExplanationText.Text = message;
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

                var payload = new { text = lastAnalyzedText };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

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

        private void DisplayAnalysis(AnalysisResult? result)
        {
            if (result == null)
            {
                DisplayError("Could not get an analysis from the server.");
                return;
            }

            if (result.IsScam == true)
            {
                StatusIcon.Glyph = "\uE7BA"; // Warning icon
                StatusText.Text = "This audio appears to be a scam.";
                StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                StatusIcon.Glyph = "\uE73E"; // Checkmark icon
                StatusText.Text = "This audio appears to be safe.";
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

        private void ExpandTranscriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (TranscriptText.MaxLines == 3)
            {
                TranscriptText.MaxLines = 0;
                ExpandTranscriptLabel.Text = "Show less";
                ExpandTranscriptIcon.Glyph = "\uE70E";
            }
            else
            {
                // Collapse it
                TranscriptText.MaxLines = 3;
                ExpandTranscriptLabel.Text = "Show more";
                ExpandTranscriptIcon.Glyph = "\uE70D";
            }
        }
    }
}