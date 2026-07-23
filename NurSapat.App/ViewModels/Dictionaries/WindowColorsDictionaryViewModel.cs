using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class WindowColorsDictionaryViewModel : BaseDictionaryViewModel<WindowColorDictionaryItemViewModel>
{
    private readonly IWindowColorsStore _store;

    public override string Title => "Справочник цветов";

    public WindowColorsDictionaryViewModel(IWindowColorsStore store)
    {
        _store = store;
    }

    public Func<Task<string?>>? PickColorHexAsync { get; set; }

    public event Action? RequestClose;

    protected override bool MatchesSearch(WindowColorDictionaryItemViewModel item, string search) =>
        Contains(item.Name, search) || Contains(item.HexCode, search);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(WindowColorDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override Task AddItemAsync()
    {
        var model = new WindowColorItem
        {
            Name = "Новый цвет",
            HexCode = "#FFFFFF"
        };

        _store.Add(model);
        var item = WindowColorDictionaryItemViewModel.FromModel(model);
        AllItems.Add(item);
        SelectedItem = item;
        return Task.CompletedTask;
    }

    protected override Task EditItemAsync(WindowColorDictionaryItemViewModel item)
    {
        _store.Update(item.ToModel());
        return Task.CompletedTask;
    }

    protected override Task DeleteItemAsync(WindowColorDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AssignColor(WindowColorDictionaryItemViewModel? item)
    {
        var target = item ?? SelectedItem;
        if (target is null || PickColorHexAsync is null)
        {
            return;
        }

        SelectedItem = target;
        var hex = await PickColorHexAsync();
        if (string.IsNullOrWhiteSpace(hex))
        {
            return;
        }

        target.HexCode = hex;
        await EditItemAsync(target);
        ApplyFilter();
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();
}
