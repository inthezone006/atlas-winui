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
            //this.SystemBackdrop = new DesktopAcrylicBackdrop();
            var app = Application.Current as App;
            if (app != null)
            {
                app.OnThemeChanged += (theme) => UpdateTitleBarTheme(theme);
            }

            AuthService.OnLoginStateChanged += UpdateAccountNavItem;
            UpdateAccountNavItem();

            NotificationService.OnShowNotification += ShowNotification;

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
            UpdateTitleBarTheme(((FrameworkElement)this.Content).ActualTheme);
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer?.Tag is string navItemTag)
            {
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

            Type? pageType = pageTag switch
            {
                "Home" => typeof(HomePage),
                "AnalysisTools" => typeof(AnalysisHubPage),
                "OnDeviceProtection" => typeof(ProtectionHubPage),
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
                AccountNavItemLoggedIn.Content = AuthService.CurrentUser?.FirstName ?? "Account";
            }
            else
            {
                AccountNavItemLoggedIn.Visibility = Visibility.Collapsed;
                AccountNavItemLoggedOut.Visibility = Visibility.Visible;
            }
        }

        private void UpdateTitleBarTheme(ElementTheme theme)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = this.AppWindow.TitleBar;
                if (theme == ElementTheme.Dark)
                {
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(32, 255, 255, 255);
                    titleBar.ButtonPressedBackgroundColor = Color.FromArgb(64, 255, 255, 255);
                }
                else
                {
                    titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(26, 0, 0, 0);
                    titleBar.ButtonPressedBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                }
            }
        }

        private void ShowNotification(string message, InfoBarSeverity severity)
        {
            NotificationInfoBar.Message = message;
            NotificationInfoBar.Severity = severity;
            NotificationInfoBar.IsOpen = true;
        }
    }
}
