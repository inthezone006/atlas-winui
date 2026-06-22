using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace ATLAS.Pages
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            this.InitializeComponent();
            this.Loaded += DashboardPage_Loaded;
            this.Unloaded += DashboardPage_Unloaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            AuthService.OnLoginStateChanged += OnLoginStateChangedHandler;
            RefreshDashboardUI();
        }

        private void DashboardPage_Unloaded(object sender, RoutedEventArgs e)
        {
            AuthService.OnLoginStateChanged -= OnLoginStateChangedHandler;
        }

        private void OnLoginStateChangedHandler()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                RefreshDashboardUI();
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshDashboardUI();
        }

        private async void RefreshDashboardUI()
        {
            if (AuthService.IsLoggedIn && AuthService.CurrentUser != null)
            {
                WelcomeTextBlock.Visibility = Visibility.Visible;
                WelcomeTextBlock.Text = $"{GetTimeOfDayGreeting()}, {AuthService.CurrentUser.FirstName}!";
                await LoadUserStats();
            }
            else
            {
                WelcomeTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private string GetTimeOfDayGreeting()
        {
            int currentHour = DateTime.Now.Hour;
            if (currentHour >= 0 && currentHour < 12) return "Good morning";
            if (currentHour >= 12 && currentHour < 18) return "Good afternoon";
            return "Good evening";
        }

        private async Task LoadUserStats()
        {
            try
            {
                var stats = await FirestoreTelemetryService.Instance.GetUserStatsAsync();

                TotalAnalysesText.Text = stats.TotalScans.ToString();
                ScamsDetectedText.Text = stats.ScamCount.ToString();
                SubmissionsText.Text = stats.SafeCount.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard stats sync delay: {ex.Message}");
            }
        }

        private void StatCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filter)
            {
                (Application.Current as App)?.RootFrame?.Navigate(
                    typeof(HistoryPage),
                    filter,
                    new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }
    }
}