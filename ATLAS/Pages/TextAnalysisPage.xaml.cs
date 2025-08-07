using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ATLAS.Pages
{
    public sealed partial class TextAnalysisPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string backendUrl = "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/analyze";

        public TextAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var inputText = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(inputText)) return;

            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;

            try
            {
                var requestPayload = new { text = inputText };
                var jsonPayload = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, backendUrl) { Content = content };

                if (AuthService.IsLoggedIn)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AnalysisResult>(jsonResponse);
                    DisplayResults(result);
                }
                else
                {
                    ResultsBox.Visibility = Visibility.Visible;
                    InterpretationText.Text = "Error";
                    ScoreText.Text = "-";
                    ExplanationText.Text = "Could not get a response from the server. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                ResultsBox.Visibility = Visibility.Visible;
                InterpretationText.Text = "Connection Error";
                ScoreText.Text = "-";
                ExplanationText.Text = $"Could not connect to the analysis service. Details: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void DisplayResults(AnalysisResult result)
        {
            if (result == null) return;

            ScoreText.Text = $"{result.Score:P0}";

            if (result.IsScam)
            {
                InterpretationText.Text = "Scam Likely";
            }
            else
            {
                InterpretationText.Text = "Not Likely a Scam";
            }

            ExplanationText.Text = result.Explanation;

            ResultsBox.Visibility = Visibility.Visible;
        }
    }
}