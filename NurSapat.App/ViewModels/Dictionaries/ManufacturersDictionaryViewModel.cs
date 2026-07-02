using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ManufacturersDictionaryViewModel : BaseDictionaryViewModel<ManufacturerDictionaryItemViewModel>
{
    private readonly IManufacturersStore _store;
    private readonly IManufacturerEditorService _editorService;

    public ManufacturersDictionaryViewModel(
        IManufacturersStore store,
        IManufacturerEditorService editorService)
    {
        _store = store;
        _editorService = editorService;
    }

    public override string Title => "Производители";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand EditManufacturerCommand => EditCommand;

    protected override bool MatchesSearch(ManufacturerDictionaryItemViewModel item, string search) =>
        Contains(item.Name, search)
        || item.EndCuttingDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
        || item.BladeWidthDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
        || item.WeldingShrinkageDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
        || (item.IsActive ? "актив" : "неактив").Contains(search, StringComparison.OrdinalIgnoreCase);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(ManufacturerDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override Task AddItemAsync() => Task.CompletedTask;

    protected override async Task EditItemAsync(ManufacturerDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);
    }

    protected override Task DeleteItemAsync(ManufacturerDictionaryItemViewModel item) => Task.CompletedTask;

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(ManufacturerDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
