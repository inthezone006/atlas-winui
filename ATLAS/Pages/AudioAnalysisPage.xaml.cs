using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
            try
            {
                var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
                var app = Application.Current as App;
                var window = app?._window;

                if (window == null)
                {
                    System.Diagnostics.Debug.WriteLine("Window context was null, cannot launch picker.");
                    return;
                }

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;

                openPicker.FileTypeFilter.Add(".mp3");
                openPicker.FileTypeFilter.Add(".wav");
                openPicker.FileTypeFilter.Add(".m4a");
                openPicker.FileTypeFilter.Add(".wma");
                openPicker.FileTypeFilter.Add(".ogg");
                openPicker.FileTypeFilter.Add(".aac");

                var file = await openPicker.PickSingleFileAsync();

                if (file != null)
                {
                    try
                    {
                        var tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;
                        var tempFile = await file.CopyAsync(tempFolder, file.Name, Windows.Storage.NameCollisionOption.ReplaceExisting);

                        _selectedAudioPath = tempFile.Path;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to copy file to temp folder: {ex.Message}");
                        _selectedAudioPath = file.Path;
                    }

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        SelectedFileNameText.Text = file.Name;
                        AnalyzeButton.IsEnabled = true;

                        TranscriptBox.Visibility = Visibility.Collapsed;
                        ResultsBox.Visibility = Visibility.Collapsed;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error launching file picker or processing file: {ex.Message}");
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
                var analysisResult = await PerformTextClassificationAsync(transcribedResultText, textPage);

                float score = (float)(analysisResult.Score ?? 0.0);
                bool isThreatScam = score > 5.0f;

                if (AuthService.IsLoggedIn)
                {
                    string fileName = Path.GetFileName(_selectedAudioPath) ?? "Unknown Audio";
                    await FirestoreTelemetryService.Instance.SaveScanTelemetryAsync("Audio Analysis", score, isThreatScam, fileName);
                }

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    TranscriptBox.Visibility = Visibility.Visible;
                    ResultsBox.Visibility = Visibility.Visible;

                    TranscriptText.Text = transcribedResultText;
                    ScoreText.Text = $"{score:F2}/10.00";

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

                    if (score <= 2.5f)
                    {
                        StatusIcon.Glyph = "\uE73E";
                        StatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                        StatusText.Text = "Safe";
                        StatusText.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else if (score <= 5.0f)
                    {
                        StatusIcon.Glyph = "\uE7BA";
                        StatusIcon.Foreground = new SolidColorBrush(Colors.Yellow);
                        StatusText.Text = "Medium Risk";
                        StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
                    }
                    else if (score <= 7.5f)
                    {
                        StatusIcon.Glyph = "\uE7BA";
                        StatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
                        StatusText.Text = "High Risk";
                        StatusText.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        StatusIcon.Glyph = "\uE814";
                        StatusIcon.Foreground = new SolidColorBrush(Colors.OrangeRed);
                        StatusText.Text = "Unsafe";
                        StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    }

                    ExplanationText.Text = analysisResult.Explanation ?? "Audio analysis completed.";
                });
            }
            catch (Exception ex)
            {
                // 3. Safely handle error UI updates
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ResultsBox.Visibility = Visibility.Visible;
                    StatusText.Text = "Audio Analysis Error";
                    ExplanationText.Text = ex.Message;
                });
            }
            finally
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    LoadingRing.IsActive = false;
                    AnalyzeButton.IsEnabled = true;
                    SelectFileButton.IsEnabled = true;
                });
            }
        }

        private async Task<AnalysisResult> PerformTextClassificationAsync(string text, TextAnalysisPage textPage)
        {
            try
            {
                var publicMethod = typeof(TextAnalysisPage).GetMethod("PerformGeminiInferenceAsync",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (publicMethod != null)
                {
                    var task = (Task<AnalysisResult>)publicMethod.Invoke(textPage, new object[] { text })!;
                    return await task;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Gemini Audio Classification Error]: {ex.Message}");
            }

            return new AnalysisResult { IsScam = false, Score = 0f, Explanation = "Failed to run Gemini text inference." };
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