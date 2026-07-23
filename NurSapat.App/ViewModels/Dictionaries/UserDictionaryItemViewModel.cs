using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Users;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class UserDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private int _groupNumber;

    [ObservableProperty]
    private bool _isAdministrator;

    public string AdministratorDisplay => IsAdministrator ? "Да" : "Нет";

    partial void OnIsAdministratorChanged(bool value) => OnPropertyChanged(nameof(AdministratorDisplay));

    public static UserDictionaryItemViewModel FromModel(UserAccountItem item) => new()
    {
        Id = item.Id,
        Username = item.Username,
        GroupNumber = item.GroupNumber,
        IsAdministrator = item.Permissions.CanManageUsers
    };

    public void RefreshFrom(UserAccountItem item)
    {
        Username = item.Username;
        GroupNumber = item.GroupNumber;
        IsAdministrator = item.Permissions.CanManageUsers;
    }
}
