using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ExchangeRatesDictionaryViewModel : BaseDictionaryViewModel<ExchangeRateDictionaryItemViewModel>
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly IExchangeRatesStore _store;
    private readonly IExchangeRateEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public ExchangeRatesDictionaryViewModel(
        IExchangeRatesStore store,
        IExchangeRateEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _messageService = messageService;
    }

    public override string Title => "Курсы валют";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand AddRatesCommand => AddCommand;

    public IAsyncRelayCommand EditRatesCommand => EditCommand;

    public IAsyncRelayCommand DeleteRatesCommand => DeleteCommand;

    protected override bool MatchesSearch(ExchangeRateDictionaryItemViewModel item, string search)
    {
        if (item.StartDateDisplay.Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (item.StartDate.Year.ToString(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var monthName = RussianCulture.DateTimeFormat.GetMonthName(item.StartDate.Month);
        if (monthName.Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Contains(item.UsdRateDisplay, search)
            || Contains(item.EurRateDisplay, search)
            || Contains(item.CnyRateDisplay, search)
            || Contains(item.UeRateDisplay, search);
    }

    protected override void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<ExchangeRateDictionaryItemViewModel> query = AllItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => MatchesSearch(item, search));
        }

        query = query.OrderByDescending(item => item.StartDate);

        FilteredItems.Clear();
        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }

        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var package in _store.GetAll())
        {
            AllItems.Add(ExchangeRateDictionaryItemViewModel.FromModel(package));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var latest = _store.GetAll().FirstOrDefault();
        var draft = new ExchangeRatePackage
        {
            StartDate = DateTime.Today,
            UsdRate = latest?.UsdRate ?? 87.45m,
            EurRate = latest?.EurRate ?? 99.62m,
            CnyRate = latest?.CnyRate ?? 12.88m,
            UeRate = latest?.UeRate ?? latest?.UsdRate ?? 87.45m
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        if (HasDuplicateStartDate(saved))
        {
            _messageService.LogError("Пакет курсов с такой датой начала действия уже существует.");
            return;
        }

        _store.Add(saved);
        var item = ExchangeRateDictionaryItemViewModel.FromModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
        _messageService.LogInfo($"Курсы с {saved.StartDate:dd.MM.yyyy} добавлены.");
    }

    protected override async Task EditItemAsync(ExchangeRateDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        if (HasDuplicateStartDate(saved, saved.Id))
        {
            _messageService.LogError("Пакет курсов с такой датой начала действия уже существует.");
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);
        _messageService.LogInfo($"Курсы с {saved.StartDate:dd.MM.yyyy} сохранены.");
    }

    protected override Task DeleteItemAsync(ExchangeRateDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        _messageService.LogInfo($"Пакет курсов от {item.StartDateDisplay} удалён.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(ExchangeRateDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }

    private bool HasDuplicateStartDate(ExchangeRatePackage package, Guid? excludeId = null)
    {
        var duplicate = _store.GetByStartDate(package.StartDate);
        return duplicate is not null && duplicate.Id != (excludeId ?? package.Id);
    }
}
