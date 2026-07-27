using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.App.Services.Utilities;

namespace NurSapat.App.ViewModels.Utilities;

public sealed partial class DealerBaseViewModel : ViewModelBase
{
    private readonly IDealerBaseService _dealerBaseService;
    private readonly IDialogService _dialogService;

    public DealerBaseViewModel(IDealerBaseService dealerBaseService, IDialogService dialogService)
    {
        _dealerBaseService = dealerBaseService;
        _dialogService = dialogService;
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
    }

    public IAsyncRelayCommand ExportCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand TogglePasswordVisibilityCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private string _protectionPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordChar))]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public char PasswordChar => IsPasswordVisible ? '\0' : '*';

    private async Task ExportAsync()
    {
        if (OwnerWindow?.StorageProvider is not { } storage)
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сформировать дилерскую базу",
                SuggestedFileName = "dealer.nursapat",
                DefaultExtension = "nursapat",
                FileTypeChoices =
                [
                    new FilePickerFileType("Дилерская база NurSapat")
                    {
                        Patterns = ["*.nursapat", "*.zp"]
                    }
                ]
            });

            var path = file?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var password = string.IsNullOrWhiteSpace(ProtectionPassword) ? null : ProtectionPassword;
            await _dealerBaseService.ExportAsync(path, password);

            if (OwnerWindow is not null)
            {
                AppNotificationHelper.ShowSuccess(OwnerWindow, "Дилерская база успешно сформирована.");
            }

            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
