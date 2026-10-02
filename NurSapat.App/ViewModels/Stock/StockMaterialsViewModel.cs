using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Materials;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services;
using NurSapat.App.Services.Materials;
using NurSapat.App.Services.Stock;
using System.Collections.ObjectModel;
using System.Text;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class StockMaterialsViewModel : ObservableObject
{
    private readonly IStockMaterialsStore _store;
    private readonly IStockDocumentEditorService _editorService;
    private readonly IStockDocumentExportService _exportService;
    private readonly IStockReportDialogService _reportDialogService;
    private readonly IMaterialsDetailStore _materialsStore;
    private readonly IAppMessageService _messageService;

    public StockMaterialsViewModel(
        IStockMaterialsStore store,
        IStockDocumentEditorService editorService,
        IStockDocumentExportService exportService,
        IStockReportDialogService reportDialogService,
        IMaterialsDetailStore materialsStore,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _exportService = exportService;
        _reportDialogService = reportDialogService;
        _materialsStore = materialsStore;
        _messageService = messageService;
    }

    public string Title => "Склад: материалы";

    public ObservableCollection<StockDocumentItemViewModel> Documents { get; } = [];

    [ObservableProperty]
    private StockDocumentItemViewModel? _selectedDocument;

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public Task InitializeAsync()
    {
        ReloadDocuments();
        return Task.CompletedTask;
    }

    partial void OnSelectedDocumentChanged(StockDocumentItemViewModel? value)
    {
        EditDocumentCommand.NotifyCanExecuteChanged();
        DeleteDocumentCommand.NotifyCanExecuteChanged();
        ExportDocumentCommand.NotifyCanExecuteChanged();
        PrintDocumentCommand.NotifyCanExecuteChanged();
    }

    private bool HasSelectedDocument() => SelectedDocument is not null;

    private void ReloadDocuments()
    {
        Documents.Clear();
        foreach (var document in _store.GetAll())
        {
            Documents.Add(StockDocumentItemViewModel.FromModel(document));
        }

        SelectedDocument = Documents.FirstOrDefault();
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    [RelayCommand]
    private async Task AddDocument()
    {
        var sequence = _store.GetNextSequence();
        var source = StockDocumentHeader.CreateDefault(StockDocumentType.ReceiptAct, sequence);
        source.Number = $"ДОК-{sequence:D4}";

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        ReloadDocuments();
        SelectedDocument = Documents.FirstOrDefault(item => item.Id == saved.Id);
        _messageService.LogInfo($"Документ {saved.Number} добавлен.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDocument))]
    private async Task EditDocument()
    {
        if (SelectedDocument is null)
        {
            return;
        }

        var source = _store.GetById(SelectedDocument.Id)?.Clone();
        if (source is null)
        {
            return;
        }

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        SelectedDocument.RefreshFrom(saved);
        _messageService.LogInfo($"Документ {saved.Number} сохранён.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDocument))]
    private Task DeleteDocument()
    {
        if (SelectedDocument is null)
        {
            return Task.CompletedTask;
        }

        var number = SelectedDocument.Number;
        _store.Delete(SelectedDocument.Id);
        ReloadDocuments();
        _messageService.LogInfo($"Документ {number} удалён.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDocument))]
    private async Task ExportDocument()
    {
        if (SelectedDocument is null)
        {
            return;
        }

        var document = _store.GetById(SelectedDocument.Id);
        if (document is null)
        {
            return;
        }

        var path = await _exportService.ExportAsync(document);
        _messageService.LogInfo($"Документ экспортирован: {path}");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDocument))]
    private Task PrintDocument()
    {
        if (SelectedDocument is null)
        {
            return Task.CompletedTask;
        }

        _messageService.LogInfo(
            $"Печать документа {SelectedDocument.Number} ({SelectedDocument.TypeDisplay}) отправлена в очередь.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task StockReport()
    {
        var period = await _reportDialogService.ShowDateRangeAsync(
            OwnerWindow,
            "Отчёт по складу",
            StockDateDialogMode.SinglePeriod);

        if (period is null)
        {
            return;
        }

        var allDocuments = _store.GetAll();
        var opening = StockBalanceCalculator.CalculateBalances(allDocuments, period.Period1Start.AddDays(-1));
        var closing = StockBalanceCalculator.CalculateBalances(allDocuments, period.Period1End);
        var movements = StockBalanceCalculator.CalculateMovements(allDocuments, period.Period1Start, period.Period1End);

        var builder = new StringBuilder();
        builder.AppendLine(
            $"Отчёт по складу за {period.Period1Start:dd.MM.yyyy} — {period.Period1End:dd.MM.yyyy}.");
        builder.AppendLine($"Позиций на конец периода: {closing.Count}.");

        foreach (var article in closing.Keys.OrderBy(key => key))
        {
            opening.TryGetValue(article, out var openingBalance);
            movements.TryGetValue(article, out var movement);
            builder.AppendLine(
                $"{article}: остаток {closing[article].Quantity:N3} (приход {movement?.ReceiptQuantity ?? 0m:N3}, расход {movement?.IssueQuantity ?? 0m:N3}, начало {(openingBalance?.Quantity ?? 0m):N3})");
        }

        _messageService.LogInfo(builder.ToString());
    }

    [RelayCommand]
    private async Task PlanningReport()
    {
        var period = await _reportDialogService.ShowDateRangeAsync(
            OwnerWindow,
            "Отчёт по складу (планирование)",
            StockDateDialogMode.DualPeriod);

        if (period?.Period2Start is null || period.Period2End is null)
        {
            return;
        }

        var allDocuments = _store.GetAll();
        var period1 = StockBalanceCalculator.CalculateMovements(allDocuments, period.Period1Start, period.Period1End);
        var period2 = StockBalanceCalculator.CalculateMovements(allDocuments, period.Period2Start.Value, period.Period2End.Value);
        var articles = period1.Keys.Union(period2.Keys).OrderBy(key => key);

        var builder = new StringBuilder();
        builder.AppendLine("Сравнение оборотов материалов за два периода.");
        foreach (var article in articles)
        {
            period1.TryGetValue(article, out var first);
            period2.TryGetValue(article, out var second);
            var deltaReceipt = (first?.ReceiptQuantity ?? 0m) - (second?.ReceiptQuantity ?? 0m);
            var deltaIssue = (first?.IssueQuantity ?? 0m) - (second?.IssueQuantity ?? 0m);
            builder.AppendLine(
                $"{article}: Δ приход {deltaReceipt:N3}, Δ расход {deltaIssue:N3}");
        }

        _messageService.LogInfo(builder.ToString());
    }

    [RelayCommand]
    private async Task InventoryReport()
    {
        var period = await _reportDialogService.ShowDateRangeAsync(
            OwnerWindow,
            "Отчёт по инвентаризации",
            StockDateDialogMode.SinglePeriod);

        if (period is null)
        {
            return;
        }

        var allDocuments = _store.GetAll();
        var theoretical = StockBalanceCalculator.CalculateBalances(allDocuments, period.Period1End);
        var inventoryActs = allDocuments
            .Where(document => document.Type == StockDocumentType.InventoryAct)
            .Where(document => document.Date.Date >= period.Period1Start.Date && document.Date.Date <= period.Period1End.Date)
            .OrderByDescending(document => document.Date)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine(
            $"Инвентаризация за {period.Period1Start:dd.MM.yyyy} — {period.Period1End:dd.MM.yyyy}.");

        if (inventoryActs.Count == 0)
        {
            builder.AppendLine("Акты инвентаризации за период не найдены.");
            _messageService.LogWarning(builder.ToString());
            return;
        }

        foreach (var act in inventoryActs)
        {
            builder.AppendLine($"Акт {act.Number} от {act.Date:dd.MM.yyyy}:");
            foreach (var line in act.Lines)
            {
                theoretical.TryGetValue(line.Article, out var calculated);
                var delta = line.Quantity - (calculated?.Quantity ?? 0m);
                builder.AppendLine(
                    $"  {line.Article}: факт {line.Quantity:N3}, расчёт {(calculated?.Quantity ?? 0m):N3}, Δ {delta:N3}");
            }
        }

        _messageService.LogInfo(builder.ToString());
    }

    [RelayCommand]
    private async Task MinStockSettings()
    {
        var minStock = _store.GetMinStockLimitDocument();
        if (minStock is null)
        {
            minStock = StockDocumentHeader.CreateDefault(StockDocumentType.MinStockLimit, _store.GetNextSequence());
            minStock.Number = "MIN-001";
            var saved = await _editorService.ShowEditorAsync(OwnerWindow, minStock, isNew: true);
            if (saved is null)
            {
                return;
            }

            _store.Add(saved);
            ReloadDocuments();
            return;
        }

        SelectedDocument = Documents.FirstOrDefault(item => item.Id == minStock.Id);
        await EditDocumentCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private Task DeficitReport()
    {
        var allDocuments = _store.GetAll();
        var balances = StockBalanceCalculator.CalculateBalances(allDocuments, DateTime.Today);
        var limits = StockBalanceCalculator.GetMinStockLimits(_store.GetMinStockLimitDocument());

        var builder = new StringBuilder();
        builder.AppendLine("Ведомость дефицита на сегодня:");
        var hasDeficit = false;

        foreach (var (article, minQty) in limits.OrderBy(pair => pair.Key))
        {
            balances.TryGetValue(article, out var balance);
            var current = balance?.Quantity ?? 0m;
            if (current < minQty)
            {
                hasDeficit = true;
                builder.AppendLine(
                    $"{article}: остаток {current:N3}, минимум {minQty:N3}, дефицит {(minQty - current):N3}");
            }
        }

        if (!hasDeficit)
        {
            builder.AppendLine("Дефицитных позиций не обнаружено.");
        }

        _messageService.LogWarning(builder.ToString());
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task CreateInventoryTemplate()
    {
        var template = StockDocumentHeader.CreateDefault(StockDocumentType.InventoryAct, _store.GetNextSequence());
        template.Number = $"ИНВ-{_store.GetNextSequence():D4}";
        template.Note = "Шаблон инвентаризации (автозаполнение из справочника материалов)";

        foreach (var material in _materialsStore.GetAll().OrderBy(item => item.Article))
        {
            template.Lines.Add(new StockDocumentLine
            {
                MaterialId = material.Id,
                Article = material.Article,
                Name = material.Name,
                Unit = MaterialEnumLabels.MainUnit(material.MainUnit),
                Quantity = 0m,
                Price = material.BasePrice,
                Currency = MaterialEnumLabels.Currency(material.Currency)
            });
        }

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, template, isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        ReloadDocuments();
        SelectedDocument = Documents.FirstOrDefault(item => item.Id == saved.Id);
        _messageService.LogInfo($"Создан шаблон инвентаризации {saved.Number} ({saved.Lines.Count} поз.).");
    }

    public async Task OpenEditorForItemAsync(StockDocumentItemViewModel item)
    {
        SelectedDocument = item;
        await EditDocumentCommand.ExecuteAsync(null);
    }
}
