using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;

namespace ATLAS.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            LoadAppSettings();
        }

        private void LoadAppSettings()
        {
            // Set the version number text
            var version = Package.Current.Id.Version;
            AppVersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var savedTheme = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppTheme"] as string;
            ThemeRadioButtons.SelectedIndex = savedTheme switch
            {
                "Light" => 0,
                "Dark" => 1,
                _ => 2 // Default
            };
        }

        private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeRadioButtons.SelectedItem is RadioButton selectedRadioButton)
            {
                string selectedThemeTag = selectedRadioButton.Tag.ToString();
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppTheme"] = selectedThemeTag;

                ElementTheme newTheme = selectedThemeTag switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
                (Application.Current as App)?.SetRequestedTheme(newTheme);
            }
        }
    }
}