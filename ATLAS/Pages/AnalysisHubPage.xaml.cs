using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace ATLAS.Pages
{
    public sealed partial class AnalysisHubPage : Page
    {
        public AnalysisHubPage()
        {
            this.InitializeComponent();
        }

        // This runs when the page is first navigated to
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Set the default selection to "Text Analysis"
            AnalysisSelectionList.SelectedIndex = 0;
            AnalysisContentFrame.Navigate(typeof(TextAnalysisPage), null, new EntranceNavigationTransitionInfo());
        }

        // This event fires whenever the user clicks a different item in the left-hand list
        private void AnalysisSelectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedItem = e.AddedItems[0] as ListViewItem;
                if (selectedItem?.Tag is not null)
                {
                    string selectedTag = selectedItem.Tag.ToString();

                    // Navigate the right-side frame to the corresponding page
                    switch (selectedTag)
                    {
                        case "Text":
                            AnalysisContentFrame.Navigate(typeof(TextAnalysisPage), null, new DrillInNavigationTransitionInfo());
                            break;
                        case "Voice":
                            AnalysisContentFrame.Navigate(typeof(VoiceAnalysisPage), null, new DrillInNavigationTransitionInfo());
                            break;
                        case "Image":
                            AnalysisContentFrame.Navigate(typeof(ImageAnalysisPage), null, new DrillInNavigationTransitionInfo());
                            break;
                    }
                }
            }
        }
    }
}