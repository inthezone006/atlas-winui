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
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace ATLAS.Pages
{
    public sealed partial class TextAnalysisPage : Page
    {
        private string? lastAnalyzedText;
        private InferenceSession? _onnxSession;

        // Use the base Tokenizer class type for the instance field
        private Tokenizer? _tokenizer;

        public TextAnalysisPage()
        {
            this.InitializeComponent();
            this.Loaded += TextAnalysisPage_Loaded;
        }

        private async void TextAnalysisPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
                string modelPath = Path.Combine(assetsPath, "model.onnx");
                string vocabPath = Path.Combine(assetsPath, "vocab.txt");

                if (File.Exists(modelPath) && File.Exists(vocabPath))
                {
                    _onnxSession = new InferenceSession(modelPath);
                    _tokenizer = Microsoft.ML.Tokenizers.BertTokenizer.Create(vocabPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading model components: {ex.Message}");
            }
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
                if (_onnxSession == null || _tokenizer == null)
                {
                    throw new InvalidOperationException("Local analysis components are still initializing or missing.");
                }

                // Run inference out on a background worker thread
                var result = await System.Threading.Tasks.Task.Run(() => PerformLocalInference(lastAnalyzedText));

                if (result != null)
                {
                    DisplayResults(result);
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

        private AnalysisResult PerformLocalInference(string text)
        {
            // FIX: Using the correct high-performance array tokenizer streaming method
            IReadOnlyList<int> nativeIds = _tokenizer!.EncodeToIds(text);

            // Map list elements into standard Int64 primitives for our ONNX Layer
            long[] tokenIds = nativeIds.Select(id => (long)id).ToArray();
            long[] attentionMask = Enumerable.Repeat(1L, tokenIds.Length).ToArray();

            // Structure safe 2D bounds shape matrix
            int[] dimensions = new int[] { 1, tokenIds.Length };

            var inputIdsTensor = new DenseTensor<long>(tokenIds, dimensions);
            var attentionMaskTensor = new DenseTensor<long>(attentionMask, dimensions);

            var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
        NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
    };

            using var outputs = _onnxSession!.Run(inputs);
            var outputTensor = outputs.First(o => o.Name == "logits").AsTensor<float>();

            float logit0 = outputTensor[0, 0];
            float logit1 = outputTensor[0, 1]; // Flagged Scam Class Logit

            double maxLogit = Math.Max(logit0, logit1);
            double exp0 = Math.Exp(logit0 - maxLogit);
            double exp1 = Math.Exp(logit1 - maxLogit);
            double sumExp = exp0 + exp1;

            float probLabel1 = (float)(exp1 / sumExp);
            bool isScam = probLabel1 >= 0.5f;

            return new AnalysisResult
            {
                IsScam = isScam,
                Score = probLabel1 * 10f,
                Explanation = $"Local AI model predicts a {probLabel1:P0} chance of this being a scam."
            };
        }

        private void DisplayResults(AnalysisResult? result)
        {
            if (result == null) return;

            if (result.IsScam == true)
            {
                StatusIcon.Glyph = "\uE7BA";
                StatusText.Text = "This text appears to be a scam.";
                StatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                StatusIcon.Glyph = "\uE73E";
                StatusText.Text = "This text appears to be safe.";
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

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            NotificationService.Show("Reporting functionality is offline.", InfoBarSeverity.Warning);
        }
    }
}