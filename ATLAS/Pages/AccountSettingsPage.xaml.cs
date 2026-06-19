using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace ATLAS.Pages
{
    public sealed partial class AccountSettingsPage : Page
    {
        public AccountSettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += AccountSettingsPage_Loaded;
        }

        private void AccountSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Pre-populate fields using current session values if available
            if (AuthService.CurrentUser != null)
            {
                FirstNameTextBox.Text = AuthService.CurrentUser.FirstName ?? "";
                LastNameTextBox.Text = AuthService.CurrentUser.LastName ?? "";
            }
            UpdateGoogleLinkStatusUI();
        }

        private void UpdateGoogleLinkStatusUI()
        {
            if (AuthService.CurrentUser != null && !string.IsNullOrEmpty(AuthService.CurrentUser.GoogleId))
            {
                LinkStatusText.Text = "Connected to Google Account";
                LinkGoogleButton.Visibility = Visibility.Collapsed;
                UnlinkGoogleButton.Visibility = Visibility.Visible;
            }
            else
            {
                LinkStatusText.Text = "Not connected to a Google Account";
                LinkGoogleButton.Visibility = Visibility.Visible;
                UnlinkGoogleButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveName_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";
            string firstName = FirstNameTextBox.Text;
            string lastName = LastNameTextBox.Text;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                ErrorTextBlock.Text = "Name fields cannot be empty.";
                return;
            }

            LoadingRing.IsActive = true;
            bool success = await AuthService.UpdateUserNamesAsync(firstName, lastName);
            LoadingRing.IsActive = false;

            if (success)
            {
                ErrorTextBlock.Text = "Name updated successfully!";
            }
            else
            {
                ErrorTextBlock.Text = "Failed to update name.";
            }
        }

        private async void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";
            string newPassword = NewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 7)
            {
                ErrorTextBlock.Text = "New password must be at least 7 characters long.";
                return;
            }

            LoadingRing.IsActive = true;
            bool success = await AuthService.UpdateUserPasswordAsync(newPassword);
            LoadingRing.IsActive = false;

            if (success)
            {
                ErrorTextBlock.Text = "Password updated safely!";
                OldPasswordBox.Password = "";
                NewPasswordBox.Password = "";
            }
            else
            {
                ErrorTextBlock.Text = "Failed to update password. Try logging out and back in to refresh credentials.";
            }
        }

        private async void LinkGoogle_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";
            LoadingRing.IsActive = true;
            bool success = await AuthService.LinkAccountWithGoogleAsync();
            LoadingRing.IsActive = false;

            if (success)
            {
                UpdateGoogleLinkStatusUI();
                ErrorTextBlock.Text = "Google account linked successfully!";
            }
            else
            {
                ErrorTextBlock.Text = "Google linking was cancelled or failed.";
            }
        }

        private async void UnlinkGoogle_Click(object sender, RoutedEventArgs e)
        {
            // Provides visual unlinking placeholder handling
            ContentDialog dialog = new ContentDialog
            {
                Title = "Unlink Google Account",
                Content = "Are you sure you want to disconnect Google authentication methods from this profile?",
                PrimaryButtonText = "Unlink",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (AuthService.CurrentUser != null)
                {
                    AuthService.CurrentUser.GoogleId = null;
                    AuthService.Login(AuthService.CurrentUser, AuthService.AuthToken!);
                    UpdateGoogleLinkStatusUI();
                    ErrorTextBlock.Text = "Google account unlinked.";
                }
            }
        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog confirmationDialog = new ContentDialog
            {
                Title = "Delete Account permanently?",
                Content = "This action is completely irreversible and wipes all historical data logs.",
                PrimaryButtonText = "Delete Permanently",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await confirmationDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                LoadingRing.IsActive = true;
                bool deleted = await AuthService.DeleteCurrentUserAccountAsync();
                LoadingRing.IsActive = false;

                if (deleted)
                {
                    Frame.Navigate(typeof(SignUpPage));
                }
                else
                {
                    ErrorTextBlock.Text = "Purge failed. Re-authenticate your session by re-logging in before running a deletion.";
                }
            }
        }
    }
}