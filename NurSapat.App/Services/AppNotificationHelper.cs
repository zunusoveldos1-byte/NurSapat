using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace NurSapat.App.Services;

public static class AppNotificationHelper
{
    public static void ShowSuccess(Window owner, string message, string title = "Уведомление")
    {
        var manager = new WindowNotificationManager(owner)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3
        };

        manager.Show(new Notification(title, message, NotificationType.Success));
    }
}
