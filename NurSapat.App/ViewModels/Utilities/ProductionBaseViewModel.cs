using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.App.Services.Utilities;

namespace NurSapat.App.ViewModels.Utilities;

public sealed partial class ProductionBaseViewModel : ViewModelBase
{
    private readonly IProductionBaseService _productionBaseService;
    private readonly IDialogService _dialogService;

    public ProductionBaseViewModel(IProductionBaseService productionBaseService, IDialogService dialogService)
    {
        _productionBaseService = productionBaseService;
        _dialogService = dialogService;
        BrowseCommand = new AsyncRelayCommand(BrowseAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync, () => !string.IsNullOrWhiteSpace(UpdateFilePath) && !IsBusy);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
    }

    public IAsyncRelayCommand BrowseCommand { get; }

    public IAsyncRelayCommand ImportCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand TogglePasswordVisibilityCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private string _updateFilePath = string.Empty;

    [ObservableProperty]
    private string _protectionPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordChar))]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private bool _isBusy;

    public char PasswordChar => IsPasswordVisible ? '\0' : '*';

    private async Task BrowseAsync()
    {
        if (OwnerWindow?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите файл обновления",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Пакет обновления NurSapat")
                {
                    Patterns = ["*.prod-update"]
                }
            ]
        });

        UpdateFilePath = files.FirstOrDefault()?.TryGetLocalPath() ?? string.Empty;
    }

    private async Task ImportAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var password = string.IsNullOrWhiteSpace(ProtectionPassword) ? null : ProtectionPassword;
            await _productionBaseService.ImportUpdatePackageAsync(UpdateFilePath, password);

            if (OwnerWindow is not null)
            {
                AppNotificationHelper.ShowSuccess(
                    OwnerWindow,
                    "Производственная база обновлена. Таблица заказов не изменялась.");
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
