using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace NurSapat.App.Services.Export;

public static class ExportNotificationHelper
{
    public static void ShowSuccess(Window owner, string message)
    {
        var manager = new WindowNotificationManager(owner)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3
        };

        manager.Show(new Notification("Экспорт", message, NotificationType.Success));
    }

    public static void ShowWarning(Window owner, string message)
    {
        var manager = new WindowNotificationManager(owner)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 3
        };

        manager.Show(new Notification("Экспорт", message, NotificationType.Warning));
    }
}
