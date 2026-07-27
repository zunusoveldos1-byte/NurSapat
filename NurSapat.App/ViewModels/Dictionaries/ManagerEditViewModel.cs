using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ManagerEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;
    private DateTime _createdAt = DateTime.Now;

    public string WindowTitle => _isNew ? "Новый менеджер" : "Данные менеджера";

    public IReadOnlyList<string> Roles { get; } = ["Менеджер", "Старший менеджер", "Руководитель", "Администратор"];

    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _position = string.Empty;
    [ObservableProperty] private string _department = string.Empty;
    [ObservableProperty] private DateTimeOffset? _hireDate;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private string _role = "Менеджер";
    [ObservableProperty] private string _photoPath = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(ManagerItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        _createdAt = isNew ? DateTime.Now : source.CreatedAt;

        FullName = source.FullName;
        Phone = source.Phone;
        Email = source.Email;
        Position = source.Position;
        Department = source.Department;
        HireDate = source.HireDate.HasValue ? new DateTimeOffset(source.HireDate.Value) : null;
        IsActive = source.IsActive;
        Note = source.Note;
        Login = source.Login;
        Role = string.IsNullOrWhiteSpace(source.Role) ? "Менеджер" : source.Role;
        PhotoPath = source.PhotoPath ?? string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public ManagerItem BuildResult() => new()
    {
        Id = _id,
        FullName = FullName.Trim(),
        Phone = Phone.Trim(),
        Email = Email.Trim(),
        Position = Position.Trim(),
        Department = Department.Trim(),
        HireDate = HireDate?.DateTime.Date,
        IsActive = IsActive,
        Note = Note.Trim(),
        Login = Login.Trim(),
        Role = Role.Trim(),
        PhotoPath = string.IsNullOrWhiteSpace(PhotoPath) ? null : PhotoPath.Trim(),
        CreatedAt = _createdAt,
        ModifiedAt = DateTime.Now
    };

    [RelayCommand]
    private void Save() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
