using NurSapat.App.Models.Orders;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.Documents;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Project;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Settings;
using NurSapat.Core.Serialization;
using System.Text;

namespace NurSapat.App.Services.Orders;

public interface IOrderBatchOperationsService
{
    Task<OrderBatchResult> SendEmailAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);

    Task<OrderBatchResult> ExportCutRequirementsAsync(IReadOnlyList<OrderHeaderItem> orders, CancellationToken cancellationToken = default);

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
    private readonly EnvironmentSettings _environmentSettings;

    public OrderBatchOperationsService(
        IOrdersStore ordersStore,
        IProjectSerializer projectSerializer,
        EnvironmentSettings environmentSettings)
    {
        _ordersStore = ordersStore;
        _projectSerializer = projectSerializer;
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

    public async Task<OrderBatchResult> ExportCutRequirementsAsync(
        IReadOnlyList<OrderHeaderItem> orders,
        CancellationToken cancellationToken = default)
    {
        var loaded = await LoadOrdersAsync(orders, cancellationToken);
        if (loaded.Count != orders.Count || loaded.Count == 0)
        {
            return new OrderBatchResult(false, "Не удалось загрузить все выбранные заказы.", []);
        }

        var lines = new List<string>
        {
            CsvRow("Заказ", "Конструкция", "Артикул", "Материал", "Количество с отходами",
                "Ед. изм.", "Отходы, %")
        };
        foreach (var project in loaded)
        {
            foreach (var construction in project.Order.Constructions.Where(item => item.IsSelectedForCalculation))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var materials = construction.SpecificationLines
                    .Where(line => line.Category is nameof(CostMaterialCategory.Profile) or nameof(CostMaterialCategory.Glazing))
                    .ToList();
                if (materials.Count == 0)
                {
                    return new OrderBatchResult(false,
                        $"Заказ {project.Order.Header.OrderNumber}, конструкция {construction.Number}. Сначала выполните расчёт материалов.", []);
                }

                foreach (var line in materials)
                {
                    lines.Add(CsvRow(project.Order.Header.OrderNumber, construction.Number.ToString(),
                        line.Article, line.MaterialName, (line.Quantity * Math.Max(1, construction.Cost.Quantity)).ToString("0.####"),
                        line.Unit, line.WastePercent.ToString("0.##")));
                }
            }
        }
        if (lines.Count == 1)
        {
            return new OrderBatchResult(false, "В выбранных заказах нет конструкций для расчёта.", []);
        }

        var folder = EnsureExportsFolder();
        var filePath = Path.Combine(folder, $"cut-requirements_{DateTime.Now:yyyyMMdd_HHmmss_fff}.csv");
        await File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8, cancellationToken);
        return new OrderBatchResult(true,
            $"Ведомость потребности в профиле и стекле сохранена. Складские остатки не изменились. {filePath}", [filePath]);
    }

    private static string CsvRow(params string[] values) =>
        string.Join(';', values.Select(value =>
        {
            var first = value.AsSpan().TrimStart();
            var safeValue = first.Length > 0 && first[0] is '=' or '+' or '-' or '@' ? "'" + value : value;
            return "\"" + safeValue.Replace("\"", "\"\"") + "\"";
        }));

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

        foreach (var project in loaded)
        {
            foreach (var line in lineFactory(project))
            {
                lines.Add($"{project.Order.Header.OrderNumber};{line}");
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
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var project = _ordersStore.TryLoadProject(order.Id);
                if (project is not null)
                {
                    result.Add(project);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(order.FilePath) && File.Exists(order.FilePath))
                {
                    result.Add(await _projectSerializer.LoadAsync(order.FilePath, cancellationToken));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
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
