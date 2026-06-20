using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace ATLAS.Pages
{
    public sealed partial class FileScanPage : Page
    {
        private StorageFile? _selectedFile;

        public FileScanPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();

            // FIX: Use your App instance's Window handle natively
            var app = Application.Current as App;
            var window = app?._window;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            openPicker.FileTypeFilter.Add("*");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                _selectedFile = file;
                SelectedFileNameText.Text = file.Name;
                AnalyzeButton.IsEnabled = true;
                ResultsBox.Visibility = Visibility.Collapsed;
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null) return;

            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;
            SelectFileButton.IsEnabled = false;

            try
            {
                string fileHash = "";
                using (var stream = await _selectedFile.OpenStreamForReadAsync())
                {
                    using var sha256 = SHA256.Create();
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    fileHash = sb.ToString();
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-apikey", FirebaseConfig.VirusTotalApiKey);

                string vtApiUrl = $"https://www.virustotal.com/api/v3/files/{fileHash}";
                var response = await client.GetAsync(vtApiUrl);

                ResultsBox.Visibility = Visibility.Visible;

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    StatusText.Text = "Unknown Signature";
                    StatusIcon.Glyph = "\xE9CE"; // Help/Unknown Info Icon
                    ExplanationText.Text = "This file footprint has never been seen by VirusTotal engines before.";
                    HarmlessCountText.Text = "0";
                    SuspiciousCountText.Text = "0";
                    MaliciousCountText.Text = "0";
                    return;
                }

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonResponse);
                    var stats = doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("last_analysis_stats");

                    int malicious = stats.GetProperty("malicious").GetInt32();
                    int suspicious = stats.GetProperty("suspicious").GetInt32();
                    int harmless = stats.GetProperty("harmless").GetInt32();

                    HarmlessCountText.Text = harmless.ToString();
                    SuspiciousCountText.Text = suspicious.ToString();
                    MaliciousCountText.Text = malicious.ToString();

                    if (malicious > 0)
                    {
                        StatusText.Text = "Threat Detected";
                        StatusIcon.Glyph = "\xE7BA"; // Warning Icon
                        ExplanationText.Text = $"Flagged by {malicious} engine signatures. SHA-256: {fileHash.Substring(0, 16)}...";
                    }
                    else
                    {
                        StatusText.Text = "Verified Safe";
                        StatusIcon.Glyph = "\xE73E"; // Checkmark Icon
                        ExplanationText.Text = "No antivirus engine variants flagged this file asset.";
                    }

                    if (AuthService.IsLoggedIn)
                    {
                        await FirestoreTelemetryService.Instance.SaveScanTelemetryAsync("File Scan", malicious > 0 ? 100f : 0f, malicious > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Scan Error";
                ExplanationText.Text = ex.Message;
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
            }
        }
    }
}