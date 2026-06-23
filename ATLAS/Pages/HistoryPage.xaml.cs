using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ATLAS.Pages
{
    public sealed partial class HistoryPage : Page
    {
        public ObservableCollection<Models.AnalysisHistoryItem> HistoryItems { get; set; } = new();

        public HistoryPage()
        {
            this.InitializeComponent();
            this.Loaded += HistoryPage_Loaded;
            this.Unloaded += HistoryPage_Unloaded;
        }

        private void HistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            AuthService.OnLoginStateChanged += OnLoginStateChangedHandler;
            LoadHistoryRecords();
        }

        private void HistoryPage_Unloaded(object sender, RoutedEventArgs e)
        {
            AuthService.OnLoginStateChanged -= OnLoginStateChangedHandler;
        }

        private void OnLoginStateChangedHandler()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                LoadHistoryRecords();
            });
        }

        private async void LoadHistoryRecords()
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
                        NoHistoryText.Text = "No history records found in your Firestore collection.";
                        NoHistoryText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            foreach (var record in records)
                            {
                                HistoryItems.Add(record);
                            }
                        });
                    }
                }
                else
                {
                    NoHistoryText.Text = "Please log in to view analysis logs history.";
                    NoHistoryText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG HISTORY EXCEPTION]: {ex}");
                NoHistoryText.Text = $"Failed to load history metrics: {ex.Message}";
                NoHistoryText.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

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