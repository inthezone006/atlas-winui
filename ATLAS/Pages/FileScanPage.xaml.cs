using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ATLAS.Pages
{
    public sealed partial class FileScanPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private StorageFile? selectedFile;

        public FileScanPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            var window = (Application.Current as App)?._window as MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add("*");

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                selectedFile = file;
                SelectedFileNameText.Text = file.Name;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFile == null) return;

            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;
            SelectFileButton.IsEnabled = false;

            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = await selectedFile.OpenStreamForReadAsync();
                content.Add(new StreamContent(stream), "file", selectedFile.Name);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/scan-file") { Content = content };
                if (AuthService.IsLoggedIn)
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
                SelectFileButton.IsEnabled = true;
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
                StatusText.Text = "This file appears to be malicious.";
                StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                StatusIcon.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                StatusIcon.Glyph = "\uE73E";
                StatusText.Text = "This file appears to be safe.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
                StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
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
    }
}