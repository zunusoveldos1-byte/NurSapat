using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StructureNameEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public string WindowTitle => _isNew ? "Новое наименование" : "Редактирование наименования";

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(StructureNameItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        Code = source.Code;
        Name = source.Name;
        ValidationMessage = string.Empty;
        OnPropertyChanged(nameof(WindowTitle));
    }

    public StructureNameItem BuildResult() => new()
    {
        Id = _id,
        Code = Code.Trim(),
        Name = Name.Trim()
    };

    [RelayCommand]
    private void Save()
    {
        ValidationMessage = Validate();
        if (!string.IsNullOrEmpty(ValidationMessage))
        {
            return;
        }

        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            return "Укажите бухгалтерский код.";
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Укажите типовое наименование изделия.";
        }

        return string.Empty;
    }
}
