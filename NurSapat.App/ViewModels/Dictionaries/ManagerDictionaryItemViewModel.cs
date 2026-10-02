using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ManagerDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty] private int _rowNumber;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _position = string.Empty;
    [ObservableProperty] private string _department = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private string _role = string.Empty;
    [ObservableProperty] private DateTime? _hireDate;
    [ObservableProperty] private DateTime _createdAt;
    [ObservableProperty] private DateTime _modifiedAt;

    public string ActiveDisplay => IsActive ? "Да" : "Нет";

    public string HireDateDisplay => HireDate?.ToString("dd.MM.yyyy") ?? "—";

    public string ForegroundBrush => IsActive ? "#0F172A" : "#94A3B8";

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(ActiveDisplay));
        OnPropertyChanged(nameof(ForegroundBrush));
    }

    partial void OnHireDateChanged(DateTime? value) => OnPropertyChanged(nameof(HireDateDisplay));

    public static ManagerDictionaryItemViewModel FromModel(ManagerItem item, int rowNumber) => new()
    {
        Id = item.Id,
        RowNumber = rowNumber,
        FullName = item.FullName,
        Phone = item.Phone,
        Email = item.Email,
        Position = item.Position,
        Department = item.Department,
        IsActive = item.IsActive,
        Note = item.Note,
        Login = item.Login,
        Role = item.Role,
        HireDate = item.HireDate,
        CreatedAt = item.CreatedAt,
        ModifiedAt = item.ModifiedAt
    };

    public ManagerItem ToModel() => new()
    {
        Id = Id,
        FullName = FullName,
        Phone = Phone,
        Email = Email,
        Position = Position,
        Department = Department,
        IsActive = IsActive,
        Note = Note,
        Login = Login,
        Role = Role,
        HireDate = HireDate,
        CreatedAt = CreatedAt,
        ModifiedAt = ModifiedAt
    };

    public void RefreshFrom(ManagerItem item)
    {
        FullName = item.FullName;
        Phone = item.Phone;
        Email = item.Email;
        Position = item.Position;
        Department = item.Department;
        IsActive = item.IsActive;
        Note = item.Note;
        Login = item.Login;
        Role = item.Role;
        HireDate = item.HireDate;
        CreatedAt = item.CreatedAt;
        ModifiedAt = item.ModifiedAt;
    }
}
