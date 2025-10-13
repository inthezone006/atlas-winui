using ATLAS.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ATLAS.Pages;

public sealed partial class SignUpPage : Page
{
    private static readonly HttpClient client = new HttpClient();
    private readonly string backendUrl = "https://atlas-backend-fkgye9e7b6dkf4cj.westus-01.azurewebsites.net/api/signup";
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
        var username = UsernameTextBox.Text;
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
            var signUpData = new
            {
                first_name = firstName,
                last_name = lastName,
                username = username,
                password = password
            };

            var jsonPayload = JsonSerializer.Serialize(signUpData, JsonContext.Default.DictionaryStringObject);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(backendUrl, content);
            if (response.IsSuccessStatusCode)
            {
                if ((Application.Current as App)?.RootFrame != null)
                {
                    (Application.Current as App)!.RootFrame!.Navigate(
                        typeof(LoginPage),
                        null,
                        new DrillInNavigationTransitionInfo());
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var errorDoc = JsonDocument.Parse(errorBody);
                var errorMessage = errorDoc.RootElement.GetProperty("error").GetString();
                ErrorTextBlock.Text = errorMessage ?? "An unknown error occurred.";
            }
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"Could not connect to the server: {ex.Message}";
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
