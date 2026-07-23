using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.App.Services.Users;

namespace NurSapat.App.ViewModels.Account;

public sealed partial class ChangePasswordViewModel : ViewModelBase
{
    private const int MinPasswordLength = 4;

    private readonly IUserService _userService;

    public ChangePasswordViewModel(IUserService userService)
    {
        _userService = userService;
        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        ToggleNewPasswordVisibilityCommand = new RelayCommand(ToggleNewPasswordVisibility);
    }

    public IAsyncRelayCommand ChangePasswordCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand ToggleNewPasswordVisibilityCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewPasswordChar))]
    private bool _isNewPasswordVisible;

    public char NewPasswordChar => IsNewPasswordVisible ? '\0' : '*';

    private void ToggleNewPasswordVisibility() =>
        IsNewPasswordVisible = !IsNewPasswordVisible;

    private async Task ChangePasswordAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            ErrorMessage = "Введите текущий пароль.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Введите новый пароль.";
            return;
        }

        if (NewPassword.Length < MinPasswordLength)
        {
            ErrorMessage = $"Новый пароль должен содержать не менее {MinPasswordLength} символов.";
            return;
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Новый пароль и подтверждение не совпадают.";
            return;
        }

        var result = await _userService.ChangePasswordAsync(CurrentPassword, NewPassword);
        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return;
        }

        if (OwnerWindow is not null)
        {
            AppNotificationHelper.ShowSuccess(OwnerWindow, result.Message, "Смена пароля");
        }

        RequestClose?.Invoke();
    }
}
