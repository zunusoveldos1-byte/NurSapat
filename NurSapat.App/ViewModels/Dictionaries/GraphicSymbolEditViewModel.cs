using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class GraphicSymbolEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public string WindowTitle => _isNew ? "Новое обозначение" : "Редактирование обозначения";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(GraphicSymbolItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        Name = source.Name;
        Code = source.Code;
        ValidationMessage = string.Empty;
        OnPropertyChanged(nameof(WindowTitle));
    }

    public GraphicSymbolItem BuildResult() => new()
    {
        Id = _id,
        Name = Name.Trim(),
        Code = Code.Trim()
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
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Укажите наименование.";
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            return "Укажите код обозначения.";
        }

        return string.Empty;
    }
}
