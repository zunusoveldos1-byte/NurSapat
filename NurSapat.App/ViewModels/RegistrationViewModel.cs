using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;
using System;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    private readonly IUserRepository _userRepo;
    private readonly MainWindowViewModel _mainVM;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _confirmPassword = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    public RegistrationViewModel(IUserRepository userRepo, MainWindowViewModel mainVM)
    {
        _userRepo = userRepo;
        _mainVM = mainVM;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Заполните все поля";
            return;
        }
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Пароли не совпадают";
            return;
        }

        IsBusy = true;
        try
        {
            var existing = await _userRepo.GetByUsernameAsync(Username);
            if (existing != null)
            {
                ErrorMessage = "Пользователь с таким именем уже существует";
                return;
            }

            var newUser = User.Create(Username, Password);
            await _userRepo.CreateAsync(newUser);
            _mainVM.NavigateToLogin();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка регистрации: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackToLogin() => _mainVM.NavigateToLogin();
}