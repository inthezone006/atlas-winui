using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Mscc.GenerativeAI;

namespace ATLAS.Pages
{
    public sealed partial class TextAnalysisPage : Page
    {
        private string? lastAnalyzedText;

        public TextAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            lastAnalyzedText = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(lastAnalyzedText)) return;

            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;

            try
            {
                var result = await PerformGeminiInferenceAsync(lastAnalyzedText);

                if (result != null)
                {
                    DisplayResults(result);
                    if (AuthService.IsLoggedIn)
                    {
                        float scoreValue = (float)(result.Score ?? 0.0);
                        bool scamValue = result.IsScam ?? false;
                        await FirestoreTelemetryService.Instance.SaveScanTelemetryAsync("Text Analysis", scoreValue, scamValue, lastAnalyzedText);
                    }
                }
            }
            catch (Exception ex)
            {
                ResultsBox.Visibility = Visibility.Visible;
                StatusText.Text = "Analysis Error";
                ScoreText.Text = "-";
                ExplanationText.Text = ex.Message;
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private static readonly string[] GeminiModelPool = new string[]
        {
            "gemini-3.1-flash-lite-preview",
            "gemini-2.5-flash-lite",
            "gemini-2.5-flash",
            "gemini-3-flash-preview",
            "gemma-3-1b-it",
            "gemma-3-4b-it",
            "gemini-1.5-flash",
            "gemini-1.5-pro"
        };

        public async Task<AnalysisResult> PerformGeminiInferenceAsync(string text)
        {
            var googleAI = new GoogleAI(Config.GeminiApiKey);
            Exception? lastException = null;

            string systemInstruction =
                "You are an elite cybersecurity automation engine specialized in natural language forensic analysis, " +
                "anti-phishing detection, and social engineering mitigation. " +
                "Analyze the provided text payload thoroughly for indicators of compromise, fraud, phishing, urgency manipulation, " +
                "or credential harvesting. " +
                "You must output strictly valid JSON matching this schema exactly without markdown formatting wrappers: " +
                "{ \"IsScam\": true/false, \"Score\": 0.0, \"Explanation\": \"string\" }. " +
                "The 'Score' field must be a floating-point value from 0.0 to 10.0 mapping precisely to these corporate risk parameters:\n" +
                "- 0.0 to 2.5: Minimal Risk (Verified Safe)\n" +
                "- 2.6 to 5.0: Elevated Risk (Caution Advised)\n" +
                "- 5.1 to 7.5: High Risk (Deceptive Pattern Detected)\n" +
                "- 7.6 to 10.0: Critical Threat (Confirmed Malicious)\n" +
                "The 'Explanation' field must systematically dissect the linguistic anchors, anomalies, or structural vectors used to reach your conclusion.";

            string combinedPrompt = $"{systemInstruction}\n\nPayload to Analyze:\n\"{text}\"";

            foreach (var modelName in GeminiModelPool)
            {
                try
                {
                    var model = googleAI.GenerativeModel(modelName);

                    var response = await model.GenerateContent(combinedPrompt);

                    if (response != null && !string.IsNullOrWhiteSpace(response.Text))
                    {
                        string cleanJson = response.Text
                            .Replace("```json", "")
                            .Replace("```", "")
                            .Trim();

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var analysisResult = JsonSerializer.Deserialize<AnalysisResult>(cleanJson, options);

                        if (analysisResult != null)
                        {
                            return analysisResult;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Gemini Engine Warning]: Tier {modelName} failed or exhausted tokens. Exception: {ex.Message}");
                    lastException = ex;
                }
            }

            throw new InvalidOperationException(
                "Critical Failure: All allocated Gemini orchestration models returned an exhausted token state or API error.",
                lastException
            );
        }

        private void DisplayResults(AnalysisResult? result)
        {
            if (result == null) return;
            float score = (float)(result.Score ?? 0.0);

            if (ResultsBox.RenderTransform == null || !(ResultsBox.RenderTransform is TranslateTransform))
            {
                ResultsBox.RenderTransform = new TranslateTransform();
            }

            ResultsBox.Visibility = Visibility.Visible;
            ExplanationText.Text = result.Explanation;

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

            ScoreText.Text = $"{score:F2}/10";
            ExplanationText.Text = result.Explanation;

            var storyboard = new Storyboard();

            var fadeAnimation = new DoubleAnimation { From = 0.0, To = 1.0, Duration = TimeSpan.FromMilliseconds(400) };
            Storyboard.SetTarget(fadeAnimation, ResultsBox);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
            storyboard.Children.Add(fadeAnimation);

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
    }
}