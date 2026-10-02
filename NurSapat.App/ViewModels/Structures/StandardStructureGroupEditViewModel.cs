using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Structures;
using NurSapat.App.Services.Structures;

namespace NurSapat.App.ViewModels.Structures;

public sealed partial class StandardStructureGroupEditViewModel : ObservableObject, IEditDialogResult<StandardStructureGroup>
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public string WindowTitle => _isNew ? "Новая группа" : "Редактирование группы";

    [ObservableProperty]
    private string _name = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(StandardStructureGroup source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        Name = source.Name;
        OnPropertyChanged(nameof(WindowTitle));
    }

    public StandardStructureGroup BuildResult() => new()
    {
        Id = _id,
        Name = Name.Trim()
    };

    [RelayCommand]
    private void Save() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
