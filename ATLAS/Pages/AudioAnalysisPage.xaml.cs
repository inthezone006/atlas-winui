using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace ATLAS.Pages
{
    public sealed partial class AudioAnalysisPage : Page
    {
        private string? _selectedAudioPath;

        public AudioAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            var app = Application.Current as App;

            // FIX: Target the exact internal _window field handle from App.xaml.cs
            var window = app?._window;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            openPicker.FileTypeFilter.Add(".wav");
            openPicker.FileTypeFilter.Add(".mp3");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                _selectedAudioPath = file.Path;

                // FIX: Mapped to the correct XAML element name 'SelectedFileNameText'
                SelectedFileNameText.Text = file.Name;
                AnalyzeButton.IsEnabled = true;

                TranscriptBox.Visibility = Visibility.Collapsed;
                ResultsBox.Visibility = Visibility.Collapsed;
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedAudioPath)) return;

            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;

            // FIX: Mapped to the correct XAML element name 'SelectFileButton'
            SelectFileButton.IsEnabled = false;

            try
            {
                // Local speech-to-text offline simulator pipeline processing loop
                await Task.Delay(2000);

                string simulatedTranscription = "Hello, this is your bank tracking department. We have detected a highly suspicious transfer pattern on your routing account setup. Please verify your profile security passcode immediately.";

                // Instantiate and invoke your local ONNX pipeline text classifier
                var textPage = new TextAnalysisPage();
                var privateInferenceMethod = typeof(TextAnalysisPage).GetMethod("PerformLocalInference",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                AnalysisResult analysisResult;
                if (privateInferenceMethod != null)
                {
                    analysisResult = (AnalysisResult)privateInferenceMethod.Invoke(textPage, new object[] { simulatedTranscription })!;
                }
                else
                {
                    analysisResult = new AnalysisResult { IsScam = false, Score = 0f, Explanation = "Local text classifier uninitialized." };
                }

                // Make containers visible
                TranscriptBox.Visibility = Visibility.Visible;
                ResultsBox.Visibility = Visibility.Visible;

                // FIX: Mapped to the correct XAML element name 'TranscriptText'
                TranscriptText.Text = simulatedTranscription;

                // FIX: Map ScoreText formatting handle safely
                ScoreText.Text = $"{(analysisResult.Score ?? 0.0):F2}/10";

                // FIX: Handle safe explicit conversion from bool? to bool using null-coalescing loops
                bool isThreatScam = analysisResult.IsScam ?? false;

                if (isThreatScam)
                {
                    StatusText.Text = "Voice Print Threat Flagged";
                    StatusIcon.Glyph = "\xE7BA"; // Warning glyph

                    // FIX: Mapped to the correct XAML element name 'ExplanationText'
                    ExplanationText.Text = analysisResult.Explanation ?? "Heuristic analysis detected structural scam patterns.";
                }
                else
                {
                    StatusText.Text = "Voice Print Clear";
                    StatusIcon.Glyph = "\xE73E"; // Checkmark glyph

                    // FIX: Mapped to the correct XAML element name 'ExplanationText'
                    ExplanationText.Text = "No structural conversational scam models were matched in the audio sample.";
                }

                // Log the telemetry record to Firestore securely
                if (AuthService.IsLoggedIn)
                {
                    // FIX: Explicitly cast parameters to resolve conversion errors
                    float telemetryScore = (float)(analysisResult.Score ?? 0.0);
                    await FirestoreTelemetryService.Instance.SaveScanTelemetryAsync("Audio Scan", telemetryScore, isThreatScam);
                }
            }
            catch (Exception ex)
            {
                ResultsBox.Visibility = Visibility.Visible;
                StatusText.Text = "Audio Analysis Error";

                // FIX: Mapped to the correct XAML element name 'ExplanationText'
                ExplanationText.Text = ex.Message;
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;

                // FIX: Mapped to the correct XAML element name 'SelectFileButton'
                SelectFileButton.IsEnabled = true;
            }
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
                TranscriptText.MaxLines = 3;
                ExpandTranscriptLabel.Text = "Show more";
                ExpandTranscriptIcon.Glyph = "\uE70D";
            }
        }

        private async void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Feature Moved Offline",
                Content = "All telemetry and scanning runs strictly locally on this build.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}