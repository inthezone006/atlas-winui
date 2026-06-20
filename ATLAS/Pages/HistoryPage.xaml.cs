using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

namespace ATLAS.Pages
{
    public sealed partial class HistoryPage : Page
    {
        public ObservableCollection<Models.AnalysisHistoryItem> HistoryItems { get; set; } = new();

        public HistoryPage()
        {
            this.InitializeComponent();
            this.Loaded += HistoryPage_Loaded;
        }

        private async void HistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingRing.IsActive = true;
            NoHistoryText.Visibility = Visibility.Collapsed;
            HistoryItems.Clear();

            try
            {
                if (AuthService.IsLoggedIn)
                {
                    var records = await FirestoreTelemetryService.Instance.GetUserHistoryAsync();

                    if (records.Count == 0)
                    {
                        NoHistoryText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        foreach (var record in records)
                        {
                            HistoryItems.Add(record);
                        }
                    }
                }
                else
                {
                    NoHistoryText.Text = "Please log in to view analysis logs telemetry history.";
                    NoHistoryText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                NoHistoryText.Text = $"Failed to load history metrics: {ex.Message}";
                NoHistoryText.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        // FIX: Missing navigation method event handler definition added
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
            else
            {
                this.Frame.Navigate(typeof(DashboardPage));
            }
        }
    }
}