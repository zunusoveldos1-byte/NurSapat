using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.App.Services.Utilities;

namespace NurSapat.App.ViewModels.Utilities;

public sealed partial class SetDatabasePasswordViewModel : ViewModelBase
{
    private const int MinPasswordLength = 4;

    private readonly IDatabasePasswordService _databasePasswordService;

    public SetDatabasePasswordViewModel(IDatabasePasswordService databasePasswordService)
    {
        _databasePasswordService = databasePasswordService;
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
    }

    public IRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand TogglePasswordVisibilityCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordChar))]
    private bool _isPasswordVisible;

    public char PasswordChar => IsPasswordVisible ? '\0' : '*';

    private void Save()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            _databasePasswordService.SetMasterPassword(string.Empty);
            NotifySaved("Пароль защиты базы снят.");
            return;
        }

        if (NewPassword.Length < MinPasswordLength)
        {
            ErrorMessage = $"Пароль должен содержать не менее {MinPasswordLength} символов.";
            return;
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Пароль и подтверждение не совпадают.";
            return;
        }

        _databasePasswordService.SetMasterPassword(NewPassword);
        NotifySaved("Мастер-пароль для шифрования пакетов и локальной SQLite-базы установлен.");
    }

    private void NotifySaved(string message)
    {
        if (OwnerWindow is not null)
        {
            AppNotificationHelper.ShowSuccess(OwnerWindow, message, "Пароль базы");
        }

        RequestClose?.Invoke();
    }
}
