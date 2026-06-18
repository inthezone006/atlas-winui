using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ATLAS.Pages
{
    public sealed partial class AudioAnalysisPage : Page
    {
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
                string path = selectedAudioFile.Path;

                // Perform client side Vosk extraction on background thread
                string transcript = await Task.Run(() => PerformVoskTranscription(path));
                lastAnalyzedText = transcript;
                DisplayTranscript(transcript);

                if (!string.IsNullOrWhiteSpace(transcript))
                {
                    var analysisResult = await Task.Run(() => PerformLocalTextClassification(transcript));
                    DisplayAnalysis(analysisResult);
                }
                else
                {
                    DisplayError("No language patterns were found inside the wave audio clip.");
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

        private string PerformVoskTranscription(string wavFilePath)
        {
            string modelPath = Path.Combine(AppContext.BaseDirectory, "Assets", "model");
            if (!Directory.Exists(modelPath)) return "Vosk language assets missing from app package directory.";

            using var model = new Vosk.Model(modelPath);
            using var rec = new Vosk.VoskRecognizer(model, 16000.0f);
            rec.SetWords(true);

            using var waveStream = new FileStream(wavFilePath, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4000];
            int bytesRead;
            System.Text.StringBuilder textAccumulator = new System.Text.StringBuilder();

            while ((bytesRead = waveStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (rec.AcceptWaveform(buffer, bytesRead))
                {
                    using var chunk = System.Text.Json.JsonDocument.Parse(rec.Result());
                    if (chunk.RootElement.TryGetProperty("text", out var txt))
                        textAccumulator.Append(txt.GetString() + " ");
                }
            }

            using var finalChunk = System.Text.Json.JsonDocument.Parse(rec.FinalResult());
            if (finalChunk.RootElement.TryGetProperty("text", out var finalTxt))
                textAccumulator.Append(finalTxt.GetString());

            return textAccumulator.ToString().Trim();
        }

        private AnalysisResult PerformLocalTextClassification(string text)
        {
            var textPage = new TextAnalysisPage();
            var privateMethod = typeof(TextAnalysisPage).GetMethod("PerformLocalInference",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (privateMethod != null)
            {
                return (AnalysisResult)privateMethod.Invoke(textPage, new object[] { text })!;
            }
            return new AnalysisResult { IsScam = false, Score = 0f, Explanation = "Failed to run inference." };
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
            var dialog = new ContentDialog { Title = "Offline", Content = "Local execution active.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }

        private void DisplayAnalysis(AnalysisResult? result)
        {
            if (result == null) return;

            if (result.IsScam == true)
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusText.Text = "This audio appears to be a scam.";
                StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                StatusIcon.Glyph = "\uE73E";
                StatusText.Text = "This audio appears to be safe.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }

            ScoreText.Text = $"{result.Score:F2}/10";
            ExplanationText.Text = result.Explanation;

            var storyboard = new Storyboard();
            var fadeAnimation = new DoubleAnimation { From = 0.0, To = 1.0, Duration = TimeSpan.FromMilliseconds(400) };
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
                TranscriptText.MaxLines = 3;
                ExpandTranscriptLabel.Text = "Show more";
                ExpandTranscriptIcon.Glyph = "\uE70D";
            }
        }
    }
}