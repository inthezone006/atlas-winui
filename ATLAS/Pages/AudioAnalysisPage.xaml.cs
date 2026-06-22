using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Vosk;
using NAudio.Wave;
namespace ATLAS.Pages
{
    public sealed partial class AudioAnalysisPage : Page
    {
        private string? _selectedAudioPath;

        private static Vosk.Model? _voskModel;
        private static readonly object _voskLock = new object();
        private static Task? _voskInitializationTask;

        public AudioAnalysisPage()
        {
            this.InitializeComponent();
            this.Loaded += AudioAnalysisPage_Loaded;
        }

        private void AudioAnalysisPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureVoskInitializedAsync();
        }

        private static Task EnsureVoskInitializedAsync()
        {
            lock (_voskLock)
            {
                if (_voskInitializationTask == null)
                {
                    _voskInitializationTask = Task.Run(() =>
                    {
                        try
                        {
                            string installedPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                            string modelPath = Path.Combine(installedPath, "Assets", "model");

                            if (Directory.Exists(modelPath))
                            {
                                Vosk.Vosk.SetLogLevel(-1);
                                _voskModel = new Vosk.Model(modelPath);
                                System.Diagnostics.Debug.WriteLine("[Vosk]: Acoustic speech models prepared.");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Vosk Init Exception]: {ex.Message}");
                        }
                    });
                }
                return _voskInitializationTask;
            }
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            var app = Application.Current as App;
            var window = app?._window;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

            openPicker.FileTypeFilter.Add(".mp3");
            openPicker.FileTypeFilter.Add(".wav");
            openPicker.FileTypeFilter.Add(".m4a");
            openPicker.FileTypeFilter.Add(".wma");
            openPicker.FileTypeFilter.Add(".ogg");
            openPicker.FileTypeFilter.Add(".aac");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                _selectedAudioPath = file.Path;
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
            SelectFileButton.IsEnabled = false;

            try
            {
                await EnsureVoskInitializedAsync();
                await TextAnalysisPage.EnsureModelInitializedAsync();

                if (_voskModel == null)
                {
                    throw new InvalidOperationException("Vosk transcription pipeline is unavailable or unconfigured.");
                }

                string transcribedResultText = await Task.Run(() => PerformAudioTranscription(_selectedAudioPath));

                if (string.IsNullOrWhiteSpace(transcribedResultText))
                {
                    transcribedResultText = "[No readable spoken vocal structures were parsed from the audio file source context]";
                }

                var textPage = new TextAnalysisPage();
                var privateInferenceMethod = typeof(TextAnalysisPage).GetMethod("PerformLocalInference",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                AnalysisResult analysisResult;
                if (privateInferenceMethod != null)
                {
                    analysisResult = (AnalysisResult)privateInferenceMethod.Invoke(textPage, new object[] { transcribedResultText })!;
                }
                else
                {
                    analysisResult = new AnalysisResult { IsScam = false, Score = 0f, Explanation = "Local text classifier uninitialized." };
                }

                TranscriptBox.Visibility = Visibility.Visible;
                ResultsBox.Visibility = Visibility.Visible;

                TranscriptText.Text = transcribedResultText;
                ScoreText.Text = $"{(analysisResult.Score ?? 0.0):F2}/10";

                if (!string.IsNullOrWhiteSpace(transcribedResultText) && transcribedResultText.Length > 180)
                {
                    ExpandTranscriptButton.Visibility = Visibility.Visible;
                    TranscriptText.MaxLines = 3;
                    ExpandTranscriptLabel.Text = "Show more";
                    ExpandTranscriptIcon.Glyph = "\uE70D";
                }
                else
                {
                    ExpandTranscriptButton.Visibility = Visibility.Collapsed;
                    TranscriptText.MaxLines = 0;
                }

                bool isThreatScam = analysisResult.IsScam ?? false;

                if (isThreatScam)
                {
                    StatusText.Text = "Threat Detected";
                    StatusIcon.Glyph = "\xE7BA";
                    ExplanationText.Text = analysisResult.Explanation ?? "Audio analysis detected structural scam patterns.";
                }
                else
                {
                    StatusText.Text = "No Threat Detected";
                    StatusIcon.Glyph = "\xE73E";
                    ExplanationText.Text = "No structural conversational scam models were matched in the audio sample.";
                }

                if (AuthService.IsLoggedIn)
                {
                    float telemetryScore = (float)(analysisResult.Score ?? 0.0);
                    string fileName = Path.GetFileName(_selectedAudioPath) ?? "Unknown Audio";
                    await FirestoreTelemetryService.Instance.SaveScanTelemetryAsync("Audio Analysis", telemetryScore, isThreatScam, fileName);
                }
            }
            catch (Exception ex)
            {
                ResultsBox.Visibility = Visibility.Visible;
                StatusText.Text = "Audio Analysis Error";
                ExplanationText.Text = ex.Message;
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
            }
        }

        private string PerformAudioTranscription(string audioPath)
        {
            try
            {
                using var recognizer = new VoskRecognizer(_voskModel, 16000.0f);
                recognizer.SetWords(false);

                using var audioReader = new AudioFileReader(audioPath);

                var outFormat = new WaveFormat(16000, 16, 1);

                using var resampler = new MediaFoundationResampler(audioReader, outFormat);

                resampler.ResamplerQuality = 60;

                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                {
                    recognizer.AcceptWaveform(buffer, bytesRead);
                }

                string finalJsonResult = recognizer.FinalResult();
                using var jsonDoc = JsonDocument.Parse(finalJsonResult);

                if (jsonDoc.RootElement.TryGetProperty("text", out var textProperty))
                {
                    return textProperty.GetString() ?? "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Universal Decoding Error]: {ex.Message}");
            }
            return "";
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