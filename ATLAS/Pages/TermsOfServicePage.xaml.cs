using Microsoft.UI.Xaml.Controls;

namespace ATLAS.Pages
{
    public sealed partial class TermsOfServicePage : Page
    {
        public TermsOfServicePage()
        {
            this.InitializeComponent();
        }

        private void BackButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
            else
            {
                this.Frame.Navigate(typeof(SettingsPage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
            }
        }
    }
}