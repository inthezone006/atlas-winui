using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

namespace ATLAS.Pages
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";
            LoadingRing.IsActive = true;
            LoginButton.IsEnabled = false;

            var username = EmailTextBox.Text; // This corresponds to the user's email address
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorTextBlock.Text = "Email and password fields cannot be left blank.";
                LoadingRing.IsActive = false;
                LoginButton.IsEnabled = true;
                return;
            }

            try
            {
                // Native Client side Firebase authentication execution
                bool loginSuccess = await AuthService.LoginWithEmailAsync(username, password);

                if (loginSuccess)
                {
                    if ((Application.Current as App)?.RootFrame != null)
                    {
                        (Application.Current as App)!.RootFrame!.Navigate(typeof(DashboardPage), null, new DrillInNavigationTransitionInfo());
                    }
                }
                else
                {
                    ErrorTextBlock.Text = "Invalid email formatting or unauthorized password credentials.";
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Connection Fault: {ex.Message}";
            }
            finally
            {
                LoadingRing.IsActive = false;
                LoginButton.IsEnabled = true;
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                LoginButton_Click(LoginButton, new RoutedEventArgs());
            }
        }

        private async void GoogleLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Note: Since you are eliminating the hosted web server proxy, traditional Web views are disabled.
            // Google Login inside a Win32 application is natively handled using the Google.Apis.Auth NuGet library.
            ContentDialog dialog = new ContentDialog
            {
                Title = "Google Authentication",
                Content = "Google single sign-on requires an active, hosted web authentication redirect proxy server pattern.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void SignUpLink_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App)?.RootFrame?.Navigate(typeof(SignUpPage), null, new DrillInNavigationTransitionInfo());
        }
    }
}