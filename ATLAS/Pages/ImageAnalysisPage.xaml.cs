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
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using WinRT.Interop;

namespace ATLAS.Pages
{
    public sealed partial class ImageAnalysisPage : Page
    {
        private StorageFile? selectedImageFile;
        private string? lastAnalyzedText;

        public ImageAnalysisPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            var window = (Application.Current as App)?._window as MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                selectedImageFile = file;
                SelectedFileNameText.Text = file.Name;
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void ExpandExtractedTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExtractedText.MaxLines == 3)
            {
                ExtractedText.MaxLines = 0;
                ExpandExtractedTextLabel.Text = "Show less";
                ExpandExtractedTextIcon.Glyph = "\uE70E";
            }
            else
            {
                ExtractedText.MaxLines = 3;
                ExpandExtractedTextLabel.Text = "Show more";
                ExpandExtractedTextIcon.Glyph = "\uE70D";
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedImageFile == null) return;
            lastAnalyzedText = null;
            ExtractedTextBox.Visibility = Visibility.Collapsed;
            ResultsBox.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = true;
            AnalyzeButton.IsEnabled = false;
            SelectFileButton.IsEnabled = false;

            try
            {
                using var stream = await selectedImageFile.OpenAsync(FileAccessMode.Read);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                OcrEngine ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                if (ocrEngine == null)
                {
                    throw new InvalidOperationException("Built-in OCR engine failed to load.");
                }

                OcrResult ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
                string extractedText = ocrResult.Text;

                var result = new ImageAnalysisResponse
                {
                    Text = extractedText,
                    Analysis = new AnalysisResult
                    {
                        IsScam = false,
                        Score = 0f,
                        Explanation = "No textual contents were detected inside the provided image frame."
                    }
                };

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    var textPageInstance = new TextAnalysisPage();
                    var localAnalysis = await PerformTextClassificationAsync(extractedText, textPageInstance);
                    result.Analysis = localAnalysis;
                }

                DisplayResults(result);

                if (AuthService.IsLoggedIn && result.Analysis != null)
                {
                    float telemetryScore = (float)(result.Analysis.Score ?? 0.0);
                    bool isThreatScam = telemetryScore > 5.0f;

                    string fileName = selectedImageFile != null ? selectedImageFile.Name : "Unknown Image";
                    await FirestoreTelemetryService.Instance.SaveScanTelemetryAsync("Image Analysis", telemetryScore, isThreatScam, fileName);
                }
            }
            catch (Exception ex)
            {
                DisplayError($"Analysis Error: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                AnalyzeButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
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
                System.Diagnostics.Debug.WriteLine($"[Image OCR Classification Error]: {ex.Message}");
            }

            return new AnalysisResult { IsScam = false, Score = 0f, Explanation = "Failed to run Gemini text inference." };
        }

        private void DisplayResults(ImageAnalysisResponse? result)
        {
            if (result == null)
            {
                DisplayError("Failed to parse the analysis result.");
                return;
            }

            lastAnalyzedText = result.Text;
            ExtractedText.Text = string.IsNullOrWhiteSpace(result.Text) ? "No text found in the image." : result.Text;
            ExtractedTextBox.Visibility = Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(result.Text) && result.Text.Length > 180)
            {
                ExpandButton.Visibility = Visibility.Visible;
                ExtractedText.MaxLines = 3;
                ExpandExtractedTextLabel.Text = "Show more";
                ExpandExtractedTextIcon.Glyph = "\uE70D";
            }
            else
            {
                ExpandButton.Visibility = Visibility.Collapsed;
                ExtractedText.MaxLines = 0;
            }

            var analysis = result.Analysis ?? new AnalysisResult
            {
                IsScam = false,
                Score = 0f,
                Explanation = "Local text analysis returned an empty profile or processing timed out."
            };

            float score = (float)(analysis.Score ?? 0.0);

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

            ScoreText.Text = $"{score:F2}/10.00";
            ExplanationText.Text = analysis.Explanation;

            ResultsBox.Visibility = Visibility.Visible;

            if (ResultsBox.RenderTransform == null || !(ResultsBox.RenderTransform is TranslateTransform))
            {
                ResultsBox.RenderTransform = new TranslateTransform();
            }

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

            storyboard.Begin();
        }

        private void DisplayError(string message)
        {
            ResultsBox.Visibility = Visibility.Visible;
            ExtractedTextBox.Visibility = Visibility.Collapsed;
            StatusText.Text = "Error";
            ScoreText.Text = "-";
            ExplanationText.Text = message;
        }
    }
}