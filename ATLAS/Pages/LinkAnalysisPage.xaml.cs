using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ATLAS.Pages
{
    public sealed partial class LinkAnalysisPage : Page
    {
        public LinkAnalysisPage()
        {
            this.InitializeComponent();
        }

        private void UrlTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AnalyzeButton_Click(AnalyzeButton, new RoutedEventArgs());
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            string targetUrl = UrlTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return;
            }

            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;
            ResultsBox.Visibility = Visibility.Collapsed;

            try
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(targetUrl);
                string base64Url = Convert.ToBase64String(plainTextBytes)
                    .Replace("+", "-").Replace("/", "_").TrimEnd('=');

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-apikey", FirebaseConfig.VirusTotalApiKey);

                string vtApiUrl = $"https://www.virustotal.com/api/v3/urls/{base64Url}";
                var response = await client.GetAsync(vtApiUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var scanContent = new MultipartFormDataContent();
                    scanContent.Add(new StringContent(targetUrl), "url");

                    var postResponse = await client.PostAsync("https://www.virustotal.com/api/v3/urls", scanContent);

                    ResultsBox.Visibility = Visibility.Visible;
                    StatusText.Text = "Scan Dispatched";
                    StatusText.Foreground = new SolidColorBrush(Colors.Orange);
                    StatusIcon.Glyph = "\xE896";
                    StatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
                    ExplanationText.Text = "This link wasn't in the database history cache. Scan requested. Re-run in 1 minute.";

                    HarmlessText.Text = "-";
                    SuspiciousText.Text = "-";
                    MaliciousText.Text = "-";
                    UndetectedText.Text = "-";
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
                    int undetected = stats.GetProperty("undetected").GetInt32();

                    ResultsBox.Visibility = Visibility.Visible;

                    HarmlessText.Text = harmless.ToString();
                    SuspiciousText.Text = suspicious.ToString();
                    MaliciousText.Text = malicious.ToString();
                    UndetectedText.Text = undetected.ToString();

                    if (malicious > 0)
                    {
                        StatusText.Text = "Unsafe";
                        StatusText.Foreground = new SolidColorBrush(Colors.IndianRed);
                        StatusIcon.Glyph = "\xE7BA";
                        StatusIcon.Foreground = new SolidColorBrush(Colors.IndianRed);
                        ExplanationText.Text = $"Flagged as dangerous by structural scan engines. Target: {targetUrl}";
                    }
                    else
                    {
                        StatusText.Text = "Safe";
                        StatusText.Foreground = new SolidColorBrush(Colors.SeaGreen);
                        StatusIcon.Glyph = "\xE73E";
                        StatusIcon.Foreground = new SolidColorBrush(Colors.SeaGreen);
                        ExplanationText.Text = "No engines flagged this link as a known security risk.";
                    }

                    if (AuthService.IsLoggedIn)
                    {
                        float telemetryScore = malicious > 0 ? 100f : 0f;
                        bool isThreat = malicious > 0;

                        await FirestoreTelemetryService.Instance.SaveScanTelemetryAsync("Link Analysis", telemetryScore, isThreat, targetUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                ResultsBox.Visibility = Visibility.Visible;
                StatusText.Text = "Scan Error";
                StatusText.Foreground = new SolidColorBrush(Colors.IndianRed);
                StatusIcon.Glyph = "\uE783";
                StatusIcon.Foreground = new SolidColorBrush(Colors.IndianRed);
                ExplanationText.Text = ex.Message;

                HarmlessText.Text = "-";
                SuspiciousText.Text = "-";
                MaliciousText.Text = "-";
                UndetectedText.Text = "-";
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
            }
        }
    }
}