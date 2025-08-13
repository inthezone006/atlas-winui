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
using ATLAS.Services;
using Windows.UI;

namespace ATLAS
{
    public sealed partial class MainWindow : Window
    {
        public Frame AppFrame => ContentFrame;
        public MainWindow()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            AuthService.OnLoginStateChanged += UpdateAccountNavItem;
            UpdateAccountNavItem();

            var appWindow = this.AppWindow;
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                SetTitleBar(CustomTitleBar);
                titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(32, 255, 255, 255);
                titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(64, 255, 255, 255);
            }

            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(HomePage));

            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.ActualThemeChanged += OnThemeChanged;
            }
            UpdateThemeButtonIcon();
            UpdateTitleBarTheme();
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
            if (pageTag == "SignOut")
            {
                AuthService.Logout();
                ContentFrame.Navigate(typeof(HomePage), null, new DrillInNavigationTransitionInfo());
                return;
            }

            Type pageType = pageTag switch
            {
                "Home" => typeof(HomePage),
                "AnalysisTools" => typeof(AnalysisHubPage),
                "Vision" => typeof(VisionPage),
                "Settings" => typeof(SettingsPage),
                "Dashboard" => typeof(DashboardPage),
                "AccountSettings" => typeof(AccountSettingsPage),
                "AccountLoggedOut" => typeof(AccountPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType, null, new DrillInNavigationTransitionInfo());
            }
        }

        private void UpdateAccountNavItem()
        {
            if (AuthService.IsLoggedIn)
            {
                AccountNavItemLoggedIn.Visibility = Visibility.Visible;
                AccountNavItemLoggedOut.Visibility = Visibility.Collapsed;
                AccountNavItemLoggedIn.Content = AuthService.CurrentUser.FirstName;
            }
            else
            {
                AccountNavItemLoggedIn.Visibility = Visibility.Collapsed;
                AccountNavItemLoggedOut.Visibility = Visibility.Visible;
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = (rootElement.ActualTheme == ElementTheme.Dark)
                    ? ElementTheme.Light
                    : ElementTheme.Dark;
            }
        }

        private void OnThemeChanged(FrameworkElement sender, object args)
        {
            UpdateThemeButtonIcon();
            UpdateTitleBarTheme();
        }
        private void UpdateThemeButtonIcon()
        {
            if (this.Content is FrameworkElement rootElement)
            {
                if (rootElement.ActualTheme == ElementTheme.Dark)
                {
                    ThemeIcon.Glyph = "\uE706";
                }
                else
                {
                    ThemeIcon.Glyph = "\uE708";
                }
            }
        }

        private void UpdateTitleBarTheme()
        {
            if (AppWindowTitleBar.IsCustomizationSupported() && this.Content is FrameworkElement rootElement)
            {
                var titleBar = this.AppWindow.TitleBar;
                if (rootElement.ActualTheme == ElementTheme.Dark)
                {
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                }
                else
                {
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                }
            }
        }
    }
}
