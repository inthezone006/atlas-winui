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
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace ATLAS.Pages
{
    public sealed partial class TextAnalysisPage : Page
    {
        private string? lastAnalyzedText;

        private static InferenceSession? _onnxSession;
        private static Tokenizer? _tokenizer;
        private static readonly object _initLock = new object();
        private static Task? _initializationTask;

        public TextAnalysisPage()
        {
            this.InitializeComponent();
            this.Loaded += TextAnalysisPage_Loaded;
        }

        private void TextAnalysisPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureModelInitializedAsync();
        }

        public static Task EnsureModelInitializedAsync()
        {
            lock (_initLock)
            {
                if (_initializationTask == null)
                {
                    _initializationTask = Task.Run(() =>
                    {
                        try
                        {
                            string installedPath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
                            string modelPath = Path.Combine(installedPath, "Assets", "model.onnx");
                            string vocabPath = Path.Combine(installedPath, "Assets", "vocab.txt");

                            if (File.Exists(modelPath) && File.Exists(vocabPath))
                            {
                                if (_onnxSession == null)
                                {
                                    _onnxSession = new InferenceSession(modelPath);
                                }
                                if (_tokenizer == null)
                                {
                                    _tokenizer = Microsoft.ML.Tokenizers.BertTokenizer.Create(vocabPath);
                                }
                                System.Diagnostics.Debug.WriteLine("[ONNX]: Shared pipeline pipelines loaded cleanly.");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[ONNX Error]: Assets missing from deployment directory.");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error initializing ONNX runtime static context: {ex.Message}");
                        }
                    });
                }
                return _initializationTask;
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
                await EnsureModelInitializedAsync();

                if (_onnxSession == null || _tokenizer == null)
                {
                    throw new InvalidOperationException("Local analysis components are still initializing or missing.");
                }

                var result = await Task.Run(() => PerformLocalInference(lastAnalyzedText));

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

        private AnalysisResult PerformLocalInference(string text)
        {
            EnsureModelInitializedAsync().Wait();

            if (_tokenizer == null || _onnxSession == null)
            {
                return new AnalysisResult { IsScam = false, Score = 0f, Explanation = "Local AI model metrics are uninitialized." };
            }

            IReadOnlyList<int> nativeIds = _tokenizer.EncodeToIds(text);

            const int MaxSequenceLength = 512;
            long[] tokenIds = nativeIds.Select(id => (long)id).Take(MaxSequenceLength).ToArray();

            long[] attentionMask = Enumerable.Repeat(1L, tokenIds.Length).ToArray();

            int[] dimensions = new int[] { 1, tokenIds.Length };

            var inputIdsTensor = new DenseTensor<long>(tokenIds, dimensions);
            var attentionMaskTensor = new DenseTensor<long>(attentionMask, dimensions);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
            };

            using var outputs = _onnxSession.Run(inputs);
            var outputTensor = outputs.First(o => o.Name == "logits").AsTensor<float>();

            float logit0 = outputTensor[0, 0];
            float logit1 = outputTensor[0, 1];

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

            if (ResultsBox.RenderTransform == null || !(ResultsBox.RenderTransform is TranslateTransform))
            {
                ResultsBox.RenderTransform = new TranslateTransform();
            }

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