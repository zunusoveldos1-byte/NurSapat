using NurSapat.App.Models.Orders;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.Documents;
using NurSapat.App.Services.Export;
using NurSapat.App.Services.Stock;
using NurSapat.Core.Models.Project;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Settings;
using NurSapat.Core.Serialization;
using System.Text;

namespace NurSapat.App.Services.Orders;

public interface IOrderBatchOperationsService
{
    Task<OrderBatchResult> SendEmailAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);

    Task<OrderBatchResult> OptimizeCutAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);

    Task<OrderBatchResult> CreateGlazingRequestAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);

    Task<OrderBatchResult> CreateMaterialsRequestAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);

    Task<OrderBatchResult> CreateHardwareRequestAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);

    Task<OrderBatchResult> CreateEconomicReportAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);
}

public sealed record OrderBatchResult(bool Success, string Message, IReadOnlyList<string> OutputFiles);

public sealed class OrderBatchOperationsService : IOrderBatchOperationsService
{
    private readonly IOrdersStore _ordersStore;
    private readonly IProjectSerializer _projectSerializer;
    private readonly IExportService _exportService;
    private readonly IStockProfileCutsStore _profileCutsStore;
    private readonly IStockGlassCutsStore _glassCutsStore;
    private readonly IStockMaterialsStore _stockMaterialsStore;
    private readonly EnvironmentSettings _environmentSettings;

    public OrderBatchOperationsService(
        IOrdersStore ordersStore,
        IProjectSerializer projectSerializer,
        IExportService exportService,
        IStockProfileCutsStore profileCutsStore,
        IStockGlassCutsStore glassCutsStore,
        IStockMaterialsStore stockMaterialsStore,
        EnvironmentSettings environmentSettings)
    {
        _ordersStore = ordersStore;
        _projectSerializer = projectSerializer;
        _exportService = exportService;
        _profileCutsStore = profileCutsStore;
        _glassCutsStore = glassCutsStore;
        _stockMaterialsStore = stockMaterialsStore;
        _environmentSettings = environmentSettings;
    }

    public async Task<OrderBatchResult> SendEmailAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken = default)
    {
        var report = await CreateEconomicReportAsync(orders, cancellationToken);
        if (!report.Success || report.OutputFiles.Count == 0)
        {
            return report;
        }

        var subject = Uri.EscapeDataString($"Заказы NurSapat ({orders.Count})");
        var body = Uri.EscapeDataString(
            $"Сформирован отчёт по заказам: {string.Join(", ", orders.Select(o => o.OrderNumber))}.\n" +
            $"Файл: {report.OutputFiles[0]}");

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"mailto:?subject={subject}&body={body}",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            return new OrderBatchResult(false, $"Отчёт создан, но не удалось открыть почтовый клиент: {ex.Message}", report.OutputFiles);
        }

        return new OrderBatchResult(true, "Отчёт сформирован и передан в почтовый клиент.", report.OutputFiles);
    }

    public async Task<OrderBatchResult> OptimizeCutAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken = default)
    {
        var loaded = await LoadOrdersAsync(orders, cancellationToken);
        if (loaded.Count == 0)
        {
            return new OrderBatchResult(false, "Не удалось загрузить выбранные заказы.", []);
        }

        var consumedProfiles = 0;
        var consumedGlass = 0;

        foreach (var project in loaded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var area = project.Order.Constructions.Sum(c => c.Metrics.ProductAreaSqM);
            var profileBars = Math.Max(1, (int)Math.Ceiling((double)area * 2.5));

            for (var i = 0; i < profileBars; i++)
            {
                var cut = _profileCutsStore.GetAll()
                    .OrderByDescending(item => item.LengthMm)
                    .FirstOrDefault();
                if (cut is null)
                {
                    break;
                }

                _profileCutsStore.Delete(cut.Id);
                consumedProfiles++;
            }

            var glassPieces = Math.Max(1, (int)Math.Ceiling((double)area));
            for (var i = 0; i < glassPieces; i++)
            {
                var cut = _glassCutsStore.GetAll()
                    .OrderByDescending(item => item.WidthMm * item.HeightMm)
                    .FirstOrDefault();
                if (cut is null)
                {
                    break;
                }

                _glassCutsStore.Delete(cut.Id);
                consumedGlass++;
            }
        }

        var folder = EnsureExportsFolder();
        var logPath = Path.Combine(folder, $"cut-optimization_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        var log = new StringBuilder()
            .AppendLine("Раскрой материалов NurSapat")
            .AppendLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}")
            .AppendLine($"Заказов: {loaded.Count}")
            .AppendLine($"Списано профильных обрезков: {consumedProfiles}")
            .AppendLine($"Списано стекольных обрезков: {consumedGlass}")
            .ToString();
        await File.WriteAllTextAsync(logPath, log, cancellationToken);

        return new OrderBatchResult(
            true,
            $"Раскрой выполнен. Списано обрезков: профиль — {consumedProfiles}, стекло — {consumedGlass}.",
            [logPath]);
    }

    public Task<OrderBatchResult> CreateGlazingRequestAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken = default) =>
        CreateRequestFileAsync(
            orders,
            "glazing-request",
            "Заявка на стеклопакеты и москитные сетки",
            project => project.Order.Constructions
                .Select(c => $"{c.Number};{c.Name};{c.Metrics.ProductAreaSqM:0.###};{c.ProfileSystemName}")
                .ToList(),
            cancellationToken);

    public Task<OrderBatchResult> CreateMaterialsRequestAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken = default) =>
        CreateRequestFileAsync(
            orders,
            "materials-request",
            "Заявка на материалы",
            project => project.Order.Constructions
                .Select(c => $"{c.Number};{c.Name};{c.ProfileSystemName};{c.Metrics.ProductAreaSqM:0.###}")
                .ToList(),
            cancellationToken);

    public Task<OrderBatchResult> CreateHardwareRequestAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken = default) =>
        CreateRequestFileAsync(
            orders,
            "hardware-request",
            "Заявка на фурнитуру",
            project => project.Order.Constructions
                .Select(c => $"{c.Number};{c.Name};{c.ProfileSystemName};{Math.Max(1, c.Cost.Quantity)}")
                .ToList(),
            cancellationToken);

    public async Task<OrderBatchResult> CreateEconomicReportAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken = default)
    {
        var loaded = await LoadOrdersAsync(orders, cancellationToken);
        if (loaded.Count == 0)
        {
            return new OrderBatchResult(false, "Не удалось загрузить выбранные заказы.", []);
        }

        var folder = EnsureExportsFolder();
        var filePath = Path.Combine(folder, $"economic-report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var lines = new List<string>
        {
            "Номер;Дата;Заказчик;Конструкций;Сумма;Статус"
        };

        foreach (var header in orders)
        {
            lines.Add(string.Join(';',
                header.OrderNumber,
                header.DateCreated.ToString("dd.MM.yyyy"),
                header.CustomerName,
                header.StructuresCount,
                header.TotalSum.ToString("N2"),
                header.Status.ToDisplayName()));
        }

        lines.Add(string.Empty);
        lines.Add("Итого;;;" + orders.Sum(o => o.StructuresCount) + ";" + orders.Sum(o => o.TotalSum).ToString("N2"));

        await File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8, cancellationToken);
        return new OrderBatchResult(true, $"Экономический отчёт сохранён ({orders.Count} заказов).", [filePath]);
    }

    private async Task<OrderBatchResult> CreateRequestFileAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        string prefix,
        string title,
        Func<ProjectDocumentData, IReadOnlyList<string>> lineFactory,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadOrdersAsync(orders, cancellationToken);
        if (loaded.Count == 0)
        {
            return new OrderBatchResult(false, "Не удалось загрузить выбранные заказы.", []);
        }

        var folder = EnsureExportsFolder();
        var filePath = Path.Combine(folder, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var lines = new List<string> { title, "Заказ;Строка" };

        for (var i = 0; i < loaded.Count; i++)
        {
            var header = orders[i];
            foreach (var line in lineFactory(loaded[i]))
            {
                lines.Add($"{header.OrderNumber};{line}");
            }
        }

        await File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8, cancellationToken);
        return new OrderBatchResult(true, $"{title} сохранена.", [filePath]);
    }

    private async Task<List<ProjectDocumentData>> LoadOrdersAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken)
    {
        var result = new List<ProjectDocumentData>();

        foreach (var order in orders)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(order.FilePath) && File.Exists(order.FilePath))
                {
                    var project = await _projectSerializer.LoadAsync(order.FilePath, cancellationToken);
                    result.Add(project);
                    continue;
                }

                var fromStore = _ordersStore.TryLoadProject(order.Id);
                if (fromStore is not null)
                {
                    result.Add(fromStore);
                }
            }
            catch
            {
                // Пропускаем повреждённые файлы / недоступные записи БД.
            }
        }

        return result;
    }

    private string EnsureExportsFolder()
    {
        var folder = Path.Combine(_environmentSettings.DocumentsPath, "exports");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
