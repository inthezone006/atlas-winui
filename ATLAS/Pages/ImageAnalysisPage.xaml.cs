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
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ATLAS.Pages
{
    public sealed partial class ImageAnalysisPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private StorageFile? selectedImageFile;

        public ImageAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            var window = (Application.Current as App)?._window as MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                selectedImageFile = file;
                SelectedFileNameText.Text = file.Name;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void ExpandExtractedTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExtractedText.MaxLines == 3)
            {
                ExtractedText.MaxLines = 0;
                ExpandExtractedTextLabel.Text = "Show less";
                ExpandExtractedTextIcon.Glyph = "\uE70E";
            }
            else
            {
                ExtractedText.MaxLines = 3;
                ExpandExtractedTextLabel.Text = "Show more";
                ExpandExtractedTextIcon.Glyph = "\uE70D";
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedImageFile == null) return;

            ExtractedTextBox.Visibility = Visibility.Collapsed;
            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;
            SelectFileButton.IsEnabled = false;

            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = await selectedImageFile.OpenStreamForReadAsync();
                content.Add(new StreamContent(stream), "image", selectedImageFile.Name);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/image-analyze") { Content = content };
                if (AuthService.IsLoggedIn)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ImageAnalysisResponse>(jsonResponse);
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
                SelectFileButton.IsEnabled = true;
            }
        }

        private void DisplayResults(ImageAnalysisResponse? result)
        {
            if (result == null)
            {
                DisplayError("Failed to parse the analysis result.");
                return;
            }

            ExtractedText.Text = string.IsNullOrWhiteSpace(result.Text) ? "No text found in the image." : result.Text;
            ExtractedTextBox.Visibility = Visibility.Visible;

            if (result.Analysis != null)
            {
                if (result.Analysis.IsScam == true)
                {
                    StatusIcon.Glyph = "\uE7BA";
                    StatusText.Text = "The text in this image appears to be a scam.";
                    StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                }
                else
                {
                    StatusIcon.Glyph = "\uE73E";
                    StatusText.Text = "The text in this image appears to be safe.";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                }

                ScoreText.Text = $"{result.Analysis.Score:F2}/10";
                ExplanationText.Text = result.Analysis.Explanation;
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
            else
            {
                ResultsBox.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayError(string message)
        {
            ResultsBox.Visibility = Visibility.Visible;
            ExtractedTextBox.Visibility = Visibility.Collapsed;
            StatusText.Text = "Error";
            ScoreText.Text = "-";
            ExplanationText.Text = message;
        }
    }
}