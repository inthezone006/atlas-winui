using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ATLAS.Pages
{
    public sealed partial class HistoryPage : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private bool isDetailsLoaded = false;

        public HistoryPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string filter)
            {
                PageTitle.Text = filter switch
                {
                    "scams_detected" => "Scams Detected History",
                    "total_analyses" => "Total Analysis History",
                    _ => "History"
                };

                await LoadHistory(filter);
            }
        }

        private async Task LoadHistory(string filter)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/history?filter={filter}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var items = JsonSerializer.Deserialize<List<AnalysisHistoryItem>>(jsonResponse, JsonContext.Default.ListAnalysisHistoryItem);
                    HistoryListView.ItemsSource = items;
                }
            }
            catch (Exception) { /* Handle error */ }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App)?.RootFrame?.Navigate(
                typeof(DashboardPage),
                null,
                new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }

        private async void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is AnalysisHistoryItem selectedItem && !string.IsNullOrEmpty(selectedItem.Id))
            {
                // Create and show a temporary loading dialog
                var loadingDialog = new ContentDialog
                {
                    Title = "Loading Details...",
                    Content = new ProgressRing { IsActive = true },
                    XamlRoot = this.XamlRoot
                };
                var loadingTask = loadingDialog.ShowAsync();

                // Fetch the full details from the new backend endpoint
                string detailsJson = await FetchHistoryDetailsAsync(selectedItem.Id);

                loadingDialog.Hide(); // Hide the loading dialog

                // Create and show the final details dialog
                var detailsDialog = new ContentDialog
                {
                    Title = "Analysis Details",
                    Content = CreateDetailsContent(detailsJson), // Build the UI for the dialog
                    CloseButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };
                await detailsDialog.ShowAsync();
            }
        }

        private async Task<string> FetchHistoryDetailsAsync(string analysisId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/me/history/{analysisId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : "{\"error\":\"Could not load details.\"}";
            }
            catch (Exception ex) { return $"{{\"error\":\"{ex.Message}\"}}"; }
        }

        private UIElement CreateDetailsContent(string detailsJson)
        {
            var panel = new StackPanel { Spacing = 10, Padding = new Thickness(20, 10, 20, 10) };
            try
            {
                var json = JsonNode.Parse(detailsJson)!.AsObject();

                // Get the available data from the JSON
                var isScam = json["is_scam"]?.GetValue<bool>() ?? false;
                var score = json["result_score"]?.GetValue<double>() ?? 0;

                // Display the Verdict
                panel.Children.Add(new TextBlock
                {
                    Text = $"Verdict: {(isScam ? "Scam Detected" : "Considered Safe")}",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });

                // Display the Score
                panel.Children.Add(new TextBlock
                {
                    Text = $"Confidence Score: {score:F2}/10"
                });

                // Add a note that original content is not available
                panel.Children.Add(new TextBlock
                {
                    Text = "\n(Service does not currently host original analysis data.)",
                    Opacity = 0.6,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            catch { panel.Children.Add(new TextBlock { Text = "Failed to parse details." }); }

            return panel;
        }

        private async void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {
            // Get the history item associated with this expander
            if (sender.DataContext is AnalysisHistoryItem selectedItem && !string.IsNullOrEmpty(selectedItem.Id))
            {
                // Only load the details the very first time it's expanded
                if (sender.Content is ProgressRing)
                {
                    var detailsJson = await FetchHistoryDetailsAsync(selectedItem.Id);
                    sender.Content = CreateDetailsContent(detailsJson); // Replace the ProgressRing with the details
                }
            }
        }
    }
}