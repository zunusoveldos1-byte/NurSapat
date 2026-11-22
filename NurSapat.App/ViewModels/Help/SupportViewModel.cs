using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Help;
using System.Diagnostics;

namespace NurSapat.App.ViewModels.Help;

public sealed class SupportViewModel : HelpDialogViewModelBase
{
    public SupportViewModel()
    {
        OpenTelegramCommand = new RelayCommand(() => OpenUrl(SupportContacts.TelegramUrl));
        OpenWhatsAppCommand = new RelayCommand(() => OpenUrl(SupportContacts.WhatsAppUrl));
        OpenPhoneCommand = new RelayCommand(() => OpenUrl(SupportContacts.PhoneTelUri));
        OpenEmailCommand = new RelayCommand(() => OpenUrl(SupportContacts.EmailMailtoUri));
    }

    public string TelegramHandle { get; } = SupportContacts.TelegramHandle;

    public string WhatsAppDisplay { get; } = SupportContacts.WhatsAppDisplay;

    public string PhoneDisplay { get; } = SupportContacts.PhoneDisplay;

    public string EmailAddress { get; } = SupportContacts.EmailAddress;

    public IRelayCommand OpenTelegramCommand { get; }

    public IRelayCommand OpenWhatsAppCommand { get; }

    public IRelayCommand OpenPhoneCommand { get; }

    public IRelayCommand OpenEmailCommand { get; }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
