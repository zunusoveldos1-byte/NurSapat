using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class GraphicSymbolsDictionaryViewModel : BaseDictionaryViewModel<GraphicSymbolDictionaryItemViewModel>
{
    private readonly IGraphicSymbolsStore _store;
    private readonly IGraphicSymbolEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public GraphicSymbolsDictionaryViewModel(
        IGraphicSymbolsStore store,
        IGraphicSymbolEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _messageService = messageService;
    }

    public override string Title => "Графические обозначения";

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

    protected override async Task AddItemAsync()
    {
        var draft = new GraphicSymbolItem
        {
            Name = "Новое обозначение",
            Code = "SYM-001"
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        var item = GraphicSymbolDictionaryItemViewModel.FromModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
        _messageService.LogInfo($"Обозначение «{saved.Name}» добавлено.");
    }

    protected override async Task EditItemAsync(GraphicSymbolDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);
        _messageService.LogInfo($"Обозначение «{saved.Name}» сохранено.");
    }

    protected override Task DeleteItemAsync(GraphicSymbolDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        _messageService.LogInfo($"Обозначение «{item.Name}» удалено.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    [RelayCommand]
    private async Task Assign()
    {
        if (SelectedItem is null)
        {
            _messageService.LogWarning("Выберите графическое обозначение в списке.");
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (OwnerWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(SelectedItem.Code);
            }
        });

        _messageService.LogInfo($"Код «{SelectedItem.Code}» скопирован в буфер обмена для назначения на чертеже.");
    }
}
