using Microsoft.UI.Xaml.Controls;
using System;

namespace ATLAS.Services
{
    public static class NotificationService
    {
        public static event Action<string, InfoBarSeverity>? OnShowNotification;

        public static void Show(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            OnShowNotification?.Invoke(message, severity);
        }
    }
}