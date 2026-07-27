using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Help;
using System.Diagnostics;

namespace NurSapat.App.ViewModels.Help;

public sealed class AboutViewModel : HelpDialogViewModelBase
{
    public AboutViewModel(IAppUpdateService appUpdateService)
    {
        AppVersion = appUpdateService.CurrentVersion;
        BuildVersion = appUpdateService.BuildVersion;
        OpenWebsiteCommand = new RelayCommand(() => OpenUrl(SupportContacts.WebsiteUrl));
        OpenSupportCommand = new RelayCommand(() => OpenUrl(SupportContacts.TelegramUrl));
    }

    public string AppVersion { get; }

    public string BuildVersion { get; }

    public string ProductName { get; } = "NurSapat";

    public string ProductSubtitle { get; } = "CAD / ERP система для оконного производства";

    public string Copyright { get; } = $"© {DateTime.Now.Year} NurSapat. Все права защищены.";

    public string LicenseInfo { get; } =
        "Программное обеспечение предоставляется по лицензионному соглашению. " +
        "Копирование, распространение и модификация без разрешения правообладателя запрещены.";

    public IReadOnlyList<string> Technologies { get; } =
    [
        ".NET 8",
        "Avalonia UI",
        "PostgreSQL",
        "MVVM Toolkit",
        "SkiaSharp"
    ];

    public string WebsiteDisplay { get; } = SupportContacts.WebsiteDisplay;

    public IRelayCommand OpenWebsiteCommand { get; }

    public IRelayCommand OpenSupportCommand { get; }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
