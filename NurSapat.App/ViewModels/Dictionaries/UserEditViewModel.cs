using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Users;
using NurSapat.App.Services;
using NurSapat.App.Services.Users;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class UserEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;
    private string _passwordHash = string.Empty;

    public string WindowTitle => _isNew ? "Новый пользователь" : "Редактирование пользователя";

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private int _groupNumber = 1;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _canManageUsers;

    [ObservableProperty]
    private bool _canAccessMaterialsDictionary;

    [ObservableProperty]
    private bool _canConfigureConstructionElements;

    [ObservableProperty]
    private bool _canAccessEconomics;

    [ObservableProperty]
    private bool _canEditOrderSheetCoefficients;

    [ObservableProperty]
    private bool _canEditOrderSheetCostCoefficients;

    [ObservableProperty]
    private bool _canEditOrderSheetExtraExpenseCoefficients;

    [ObservableProperty]
    private bool _canPrintMaterialPrices;

    [ObservableProperty]
    private bool _canDeleteOrders;

    [ObservableProperty]
    private bool _canAccessWarehouse;

    [ObservableProperty]
    private bool _canEditWarehouse;

    [ObservableProperty]
    private bool _canEditWarehouseInvoice;

    [ObservableProperty]
    private bool _groupOnlyDataAccess;

    public event Action<bool>? RequestClose;

    public void Load(UserAccountItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        _passwordHash = source.PasswordHash;

        Username = source.Username;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        GroupNumber = source.GroupNumber;
        ValidationMessage = string.Empty;

        var permissions = source.Permissions;
        CanManageUsers = permissions.CanManageUsers;
        CanAccessMaterialsDictionary = permissions.CanAccessMaterialsDictionary;
        CanConfigureConstructionElements = permissions.CanConfigureConstructionElements;
        CanAccessEconomics = permissions.CanAccessEconomics;
        CanEditOrderSheetCoefficients = permissions.CanEditOrderSheetCoefficients;
        CanEditOrderSheetCostCoefficients = permissions.CanEditOrderSheetCostCoefficients;
        CanEditOrderSheetExtraExpenseCoefficients = permissions.CanEditOrderSheetExtraExpenseCoefficients;
        CanPrintMaterialPrices = permissions.CanPrintMaterialPrices;
        CanDeleteOrders = permissions.CanDeleteOrders;
        CanAccessWarehouse = permissions.CanAccessWarehouse;
        CanEditWarehouse = permissions.CanEditWarehouse;
        CanEditWarehouseInvoice = permissions.CanEditWarehouseInvoice;
        GroupOnlyDataAccess = permissions.GroupOnlyDataAccess;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public UserAccountItem? TryBuildResult()
    {
        ValidationMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ValidationMessage = "Укажите имя пользователя.";
            return null;
        }

        if (_isNew && string.IsNullOrWhiteSpace(Password))
        {
            ValidationMessage = "Укажите пароль.";
            return null;
        }

        if (!string.IsNullOrWhiteSpace(Password) && Password != ConfirmPassword)
        {
            ValidationMessage = "Пароль и подтверждение не совпадают.";
            return null;
        }

        if (GroupNumber < 1)
        {
            ValidationMessage = "№ группы должен быть больше нуля.";
            return null;
        }

        var item = new UserAccountItem
        {
            Id = _id,
            Username = Username.Trim(),
            PasswordHash = _passwordHash,
            GroupNumber = GroupNumber,
            Permissions = BuildPermissions()
        };

        if (!string.IsNullOrWhiteSpace(Password))
        {
            item.SetPassword(Password);
        }

        return item;
    }

    public UserAccountItem BuildResult() => TryBuildResult()!;

    private UserPermissions BuildPermissions() => new()
    {
        CanManageUsers = CanManageUsers,
        CanAccessMaterialsDictionary = CanAccessMaterialsDictionary,
        CanConfigureConstructionElements = CanConfigureConstructionElements,
        CanAccessEconomics = CanAccessEconomics,
        CanEditOrderSheetCoefficients = CanEditOrderSheetCoefficients,
        CanEditOrderSheetCostCoefficients = CanEditOrderSheetCostCoefficients,
        CanEditOrderSheetExtraExpenseCoefficients = CanEditOrderSheetExtraExpenseCoefficients,
        CanPrintMaterialPrices = CanPrintMaterialPrices,
        CanDeleteOrders = CanDeleteOrders,
        CanAccessWarehouse = CanAccessWarehouse,
        CanEditWarehouse = CanEditWarehouse,
        CanEditWarehouseInvoice = CanEditWarehouseInvoice,
        GroupOnlyDataAccess = GroupOnlyDataAccess
    };

    [RelayCommand]
    private void Save()
    {
        if (TryBuildResult() is null)
        {
            return;
        }

        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
