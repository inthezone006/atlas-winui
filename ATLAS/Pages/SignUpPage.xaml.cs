using ATLAS.Models;
using ATLAS.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ATLAS.Pages;

public sealed partial class SignUpPage : Page
{
    public SignUpPage()
    {
        InitializeComponent();
    }

    private void LoginLink_Click(object sender, RoutedEventArgs e)
    {
        (Application.Current as App)?.RootFrame?.Navigate(
            typeof(LoginPage),
            null,
            new DrillInNavigationTransitionInfo());
    }

    private static List<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (password.Length < 7)
        {
            errors.Add("Password must be at least 7 characters long.");
        }
        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }
        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one number.");
        }

        return errors;
    }

    private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            SignUpButton_Click(SignUpButton, new RoutedEventArgs());
        }
    }

    private async void SignUpButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Text = "";

        var firstName = FirstNameTextBox.Text;
        var lastName = LastNameTextBox.Text;
        var username = EmailTextBox.Text;
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(username))
        {
            ErrorTextBlock.Text = "All fields are required.";
            return;
        }

        var passwordErrors = ValidatePassword(password);
        if (passwordErrors.Any())
        {
            ErrorTextBlock.Text = string.Join("\n", passwordErrors);
            return;
        }

        Button button = (Button)sender;
        if (button != null)
        {
            button.IsEnabled = false;
        }

        try
        {
            // FIX: Use your local client-side Firebase Auth wrapper
            bool registrationSuccess = await AuthService.RegisterWithEmailAsync(username, password, firstName, lastName);

            if (registrationSuccess)
            {
                if ((Application.Current as App)?.RootFrame != null)
                {
                    // Redirect directly into the secure app view layout 
                    (Application.Current as App)!.RootFrame!.Navigate(
                        typeof(DashboardPage),
                        null,
                        new DrillInNavigationTransitionInfo());
                }
            }
            else
            {
                ErrorTextBlock.Text = "Registration rejected. Please verify your credentials or email format.";
            }
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"Firebase Connection Error: {ex.Message}";
        }
        finally
        {
            if (button != null)
            {
                button.IsEnabled = true;
            }
        }
    }
}