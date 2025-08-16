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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int selectedIndex && selectedIndex >= 0)
            {
                AnalysisSelectionList.SelectedIndex = selectedIndex;
            }
            else
            {
                AnalysisSelectionList.SelectedIndex = 0;
            }
        }

        private void AnalysisSelectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedItem = e.AddedItems[0] as ListViewItem;
                if (selectedItem?.Tag is string selectedTag)
                {
                    switch (selectedTag)
                    {
                        case "Text":
                            AnalysisContentFrame.Navigate(typeof(TextAnalysisPage), null, new DrillInNavigationTransitionInfo());
                            break;
                        case "Audio":
                            AnalysisContentFrame.Navigate(typeof(AudioAnalysisPage), null, new DrillInNavigationTransitionInfo());
                            break;
                        case "Image":
                            AnalysisContentFrame.Navigate(typeof(ImageAnalysisPage), null, new DrillInNavigationTransitionInfo());
                            break;
                        case "Link":
                            AnalysisContentFrame.Navigate(typeof(LinkAnalysisPage), null, new DrillInNavigationTransitionInfo());
                            break;
                    }
                }
            }
        }
    }
}