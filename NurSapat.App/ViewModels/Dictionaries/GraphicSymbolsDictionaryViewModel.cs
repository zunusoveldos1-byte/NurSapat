using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class GraphicSymbolsDictionaryViewModel : BaseDictionaryViewModel<GraphicSymbolDictionaryItemViewModel>
{
    private readonly IGraphicSymbolsStore _store;
    private readonly IAppMessageService _messageService;

    public GraphicSymbolsDictionaryViewModel(
        IGraphicSymbolsStore store,
        IAppMessageService messageService)
    {
        _store = store;
        _messageService = messageService;
    }

    public override string Title => "Графические обозначения";

    public string PlaceholderMessage =>
        "Справочник графических обозначений находится в разработке. " +
        "Здесь будут отображаться ручки, замки и другие условные символы.";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    protected override bool MatchesSearch(GraphicSymbolDictionaryItemViewModel item, string search) =>
        Contains(item.Name, search) || Contains(item.Code, search);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(GraphicSymbolDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override Task AddItemAsync()
    {
        _messageService.LogInfo("Добавление графических обозначений будет доступно в следующей версии.");
        return Task.CompletedTask;
    }

    protected override Task EditItemAsync(GraphicSymbolDictionaryItemViewModel item)
    {
        _messageService.LogInfo("Редактирование графических обозначений будет доступно в следующей версии.");
        return Task.CompletedTask;
    }

    protected override Task DeleteItemAsync(GraphicSymbolDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    [RelayCommand]
    private void Assign()
    {
        if (SelectedItem is null)
        {
            _messageService.LogWarning("Выберите графическое обозначение в списке.");
            return;
        }

        _messageService.LogInfo(
            $"Назначение обозначения «{SelectedItem.Name}» будет реализовано в модуле чертежа.");
    }
}
