using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Linq;

namespace ATLAS.Pages
{
    public sealed partial class ProtectionHubPage : Page
    {
        public ProtectionHubPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Default to the first item, "File Scan"
            ProtectionSelectionList.SelectedIndex = 0;
        }

        private void ProtectionSelectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is ListViewItem selectedItem)
            {
                if (selectedItem.Tag is string selectedTag)
                {
                    switch (selectedTag)
                    {
                        case "FileScan":
                            ProtectionContentFrame.Navigate(typeof(FileScanPage), null, new DrillInNavigationTransitionInfo());
                            break;
                    }
                }
            }
        }
    }
}