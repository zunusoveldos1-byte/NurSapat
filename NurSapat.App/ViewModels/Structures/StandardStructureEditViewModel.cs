using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Structures;
using NurSapat.App.Services.Structures;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Structures;

public sealed partial class StandardStructureEditViewModel : ObservableObject, IEditDialogResult<StandardStructureItem>
{
    private readonly IStandardStructuresStore _store;
    private Guid _id = Guid.NewGuid();
    private bool _isNew;
    private string _geometryJson = string.Empty;
    private string _thumbnailPath = string.Empty;

    public StandardStructureEditViewModel(IStandardStructuresStore store)
    {
        _store = store;
    }

    public string WindowTitle => _isNew ? "Новая типовая конструкция" : "Переименование конструкции";

    public string GeometryNotice =>
        "Геометрия конструкции зафиксирована. Доступно изменение только наименования и группы.";

    public ObservableCollection<StandardStructureGroup> Groups { get; } = [];

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private StandardStructureGroup? _selectedGroup;

    public event Action<bool>? RequestClose;

    public Task InitializeAsync()
    {
        Groups.Clear();
        foreach (var group in _store.GetGroups())
        {
            Groups.Add(group);
        }

        return Task.CompletedTask;
    }

    public void Load(StandardStructureItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        _geometryJson = source.GeometryJson;
        _thumbnailPath = source.ThumbnailPath;

        Name = source.Name;
        SelectedGroup = Groups.FirstOrDefault(group => group.Id == source.GroupId) ?? Groups.FirstOrDefault();

        OnPropertyChanged(nameof(WindowTitle));
    }

    public StandardStructureItem BuildResult()
    {
        var groupId = SelectedGroup?.Id ?? Groups.FirstOrDefault()?.Id ?? Guid.Empty;
        return new StandardStructureItem
        {
            Id = _id,
            Name = Name.Trim(),
            GroupId = groupId,
            ThumbnailPath = _thumbnailPath,
            GeometryJson = _geometryJson
        };
    }

    [RelayCommand]
    private void Save() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
