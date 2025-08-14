using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ATLAS.Pages
{
    public sealed partial class VoiceAnalysisPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private StorageFile selectedAudioFile;

        public VoiceAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();

            // This gets the handle for the main window
            var window = (Application.Current as App)?._window as MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(filePicker, hwnd);

            // Set file type filters
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

            // --- 1. Set UI to loading state ---
            TranscriptBox.Visibility = Visibility.Collapsed;
            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;
            SelectFileButton.IsEnabled = false;

            try
            {
                // --- 2. Call Transcription and Analysis APIs ---
                // We can run both API calls at the same time for efficiency
                var transcribeTask = TranscribeAudioAsync(selectedAudioFile);
                var analyzeTask = AnalyzeAudioAsync(selectedAudioFile);

                await Task.WhenAll(transcribeTask, analyzeTask);

                // --- 3. Process the results ---
                var transcript = await transcribeTask;
                var analysisResult = await analyzeTask;

                DisplayTranscript(transcript);
                DisplayAnalysis(analysisResult);
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }
            finally
            {
                // --- 4. Reset UI from loading state ---
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
            }
        }

        private async Task<string> TranscribeAudioAsync(StorageFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = await file.OpenStreamForReadAsync();
            content.Add(new StreamContent(stream), "audio", file.Name);

            var response = await client.PostAsync("https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/transcribe", content);
            if (!response.IsSuccessStatusCode) return "Failed to transcribe audio.";

            var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return jsonDoc.RootElement.GetProperty("transcript").GetString() ?? "Transcript not found.";
        }

        private async Task<AnalysisResult> AnalyzeAudioAsync(StorageFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = await file.OpenStreamForReadAsync();
            content.Add(new StreamContent(stream), "audio", file.Name);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/analyze-audio") { Content = content };
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

        private void DisplayAnalysis(AnalysisResult result)
        {
            if (result == null)
            {
                DisplayError("Could not get an analysis from the server.");
                return;
            }
            ScoreText.Text = $"{result.Score:F2}/10";
            InterpretationText.Text = result.IsScam ? "Scam Likely" : "Not Likely a Scam";
            ExplanationText.Text = result.Explanation;
            ResultsBox.Visibility = Visibility.Visible;
        }

        private void DisplayError(string message)
        {
            ResultsBox.Visibility = Visibility.Visible;
            InterpretationText.Text = "Error";
            ScoreText.Text = "-";
            ExplanationText.Text = message;
        }
    }
}