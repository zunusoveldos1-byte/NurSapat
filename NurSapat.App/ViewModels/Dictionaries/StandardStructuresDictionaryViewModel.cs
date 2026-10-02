using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Structures;
using NurSapat.App.Services;
using NurSapat.App.Services.Structures;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StandardStructuresDictionaryViewModel : ObservableObject
{
    private readonly IStandardStructuresStore _store;
    private readonly IStandardStructureEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public StandardStructuresDictionaryViewModel(
        IStandardStructuresStore store,
        IStandardStructureEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _messageService = messageService;
    }

    public string Title => "Типовые конструкции";

    public ObservableCollection<StandardStructureGroupItemViewModel> Groups { get; } = [];

    public ObservableCollection<StandardStructureItemViewModel> Structures { get; } = [];

    [ObservableProperty]
    private StandardStructureGroupItemViewModel? _selectedGroup;

    [ObservableProperty]
    private StandardStructureItemViewModel? _selectedStructure;

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public Task InitializeAsync()
    {
        ReloadGroups();
        return Task.CompletedTask;
    }

    partial void OnSelectedGroupChanged(StandardStructureGroupItemViewModel? value)
    {
        ReloadStructures();
        NotifyStructureCommands();
        NotifyGroupCommands();
    }

    partial void OnSelectedStructureChanged(StandardStructureItemViewModel? value) =>
        NotifyStructureCommands();

    private bool HasSelectedGroup() => SelectedGroup is not null;

    private bool HasSelectedStructure() => SelectedStructure is not null;

    private void ReloadGroups()
    {
        var selectedId = SelectedGroup?.Id;
        Groups.Clear();
        foreach (var group in _store.GetGroups())
        {
            Groups.Add(StandardStructureGroupItemViewModel.FromModel(group));
        }

        SelectedGroup = Groups.FirstOrDefault(group => group.Id == selectedId) ?? Groups.FirstOrDefault();
    }

    private void ReloadStructures()
    {
        Structures.Clear();
        if (SelectedGroup is null)
        {
            SelectedStructure = null;
            return;
        }

        foreach (var structure in _store.GetStructuresByGroup(SelectedGroup.Id))
        {
            Structures.Add(StandardStructureItemViewModel.FromModel(structure));
        }

        SelectedStructure = Structures.FirstOrDefault();
    }

    private void NotifyGroupCommands()
    {
        EditGroupCommand.NotifyCanExecuteChanged();
        DeleteGroupCommand.NotifyCanExecuteChanged();
        AddStructureCommand.NotifyCanExecuteChanged();
    }

    private void NotifyStructureCommands()
    {
        EditStructureCommand.NotifyCanExecuteChanged();
        DeleteStructureCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    [RelayCommand]
    private async Task AddGroup()
    {
        var saved = await _editorService.EditGroupAsync(
            OwnerWindow,
            new StandardStructureGroup { Name = "Новая группа" },
            isNew: true);

        if (saved is null)
        {
            return;
        }

        _store.AddGroup(saved);
        ReloadGroups();
        SelectedGroup = Groups.FirstOrDefault(group => group.Id == saved.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedGroup))]
    private async Task EditGroup()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        var source = _store.GetGroupById(SelectedGroup.Id);
        if (source is null)
        {
            return;
        }

        var saved = await _editorService.EditGroupAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.UpdateGroup(saved);
        SelectedGroup.RefreshFrom(saved);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedGroup))]
    private Task DeleteGroup()
    {
        if (SelectedGroup is null)
        {
            return Task.CompletedTask;
        }

        var name = SelectedGroup.Name;
        _store.DeleteGroup(SelectedGroup.Id);
        ReloadGroups();
        _messageService.LogInfo($"Группа «{name}» и её конструкции удалены.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedGroup))]
    private async Task AddStructure()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        var draft = new StandardStructureItem
        {
            Name = "Новая конструкция",
            GroupId = SelectedGroup.Id
        };

        var saved = await _editorService.EditStructureAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.AddStructure(saved);
        ReloadStructures();
        SelectedStructure = Structures.FirstOrDefault(item => item.Id == saved.Id);
        _messageService.LogInfo($"Конструкция «{saved.Name}» создана. Геометрию задайте в чертеже.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedStructure))]
    private async Task EditStructure()
    {
        if (SelectedStructure is null)
        {
            return;
        }

        var source = _store.GetStructureById(SelectedStructure.Id)?.Clone();
        if (source is null)
        {
            return;
        }

        var saved = await _editorService.EditStructureAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.UpdateStructure(saved);
        if (saved.GroupId != SelectedGroup?.Id)
        {
            ReloadGroups();
            SelectedGroup = Groups.FirstOrDefault(group => group.Id == saved.GroupId);
        }
        else
        {
            ReloadStructures();
            SelectedStructure = Structures.FirstOrDefault(item => item.Id == saved.Id);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedStructure))]
    private Task DeleteStructure()
    {
        if (SelectedStructure is null)
        {
            return Task.CompletedTask;
        }

        var name = SelectedStructure.Name;
        _store.DeleteStructure(SelectedStructure.Id);
        ReloadStructures();
        _messageService.LogInfo($"Конструкция «{name}» удалена.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DeleteAllStructures()
    {
        _store.DeleteAllStructures();
        ReloadStructures();
        _messageService.LogWarning("Все типовые конструкции удалены.");
        return Task.CompletedTask;
    }
}
