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
            if (AuthService.CurrentUser != null)
            {
                FirstNameTextBox.Text = AuthService.CurrentUser.FirstName ?? "";
                LastNameTextBox.Text = AuthService.CurrentUser.LastName ?? "";
            }
        }

        private async Task ShowConfirmationDialogAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
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
                await ShowConfirmationDialogAsync("Name Updated", "Your local profile first and last display names have been updated successfully.");
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
                await ShowConfirmationDialogAsync("Password Changed", "Your password has been securely rotated on the server.");
                OldPasswordBox.Password = "";
                NewPasswordBox.Password = "";
            }
            else
            {
                ErrorTextBlock.Text = "Failed to update password. Try logging out and back in to refresh credentials.";
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