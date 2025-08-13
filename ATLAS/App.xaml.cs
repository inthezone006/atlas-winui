using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ATLAS.Services;

namespace ATLAS
{ 
    public partial class App : Application
    {
        public Frame RootFrame { get; private set; }
        internal Window _window = default!;

        public event Action<ElementTheme> OnThemeChanged;

        public App()
        {
            InitializeComponent();
            AuthService.TryLoadUserFromStorage();
        }

        public void SetRequestedTheme(ElementTheme theme)
        {
            if (_window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
                OnThemeChanged?.Invoke(theme);
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            RootFrame = (_window as MainWindow).AppFrame;
            LoadAndApplyTheme();
            _window.Activate();
        }

        private void LoadAndApplyTheme()
        {
            var savedTheme = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppTheme"] as string;
            ElementTheme theme = savedTheme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
            SetRequestedTheme(theme);
        }
    }
}
