using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ATLAS.Pages;

namespace ATLAS
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();

            var appWindow = this.AppWindow;
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                SetTitleBar(CustomTitleBar);

                // Set Mica colors for title bar buttons
                titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0); // Transparent
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0); // Transparent
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(32, 255, 255, 255); // Subtle hover
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(64, 255, 255, 255); // Subtle pressed
            }

            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(HomePage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer?.Tag is not null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavigateToPage(navItemTag);
            }
        }

        private void NavigateToPage(string pageTag)
        {
            Type? pageType = pageTag switch
            {
                "Home" => typeof(HomePage),
                "TextAnalysis" => null, // typeof(TextAnalysisPage),
                "VoiceAnalysis" => null, // typeof(VoiceAnalysisPage),
                "ImageAnalysis" => null, // typeof(ImageAnalysisPage),
                "Vision" => null, // typeof(VisionPage),
                "Account" => typeof(AccountPage),
                "Settings" => null, // typeof(SettingsPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType, null, new DrillInNavigationTransitionInfo());
            }
        }
    }
}
