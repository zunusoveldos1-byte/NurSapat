using System.Text.Json;
using NurSapat.App.Models.EditDialogs;
using NurSapat.App.Services.EditDialogs;
using NurSapat.App.Services.Settings;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.Export;

public interface IExportService
{
    Task<ExportResult> ExportJsonAsync(
        Order order,
        CadEditorViewModel? cadEditor,
        string targetFolder,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ExportResult> ExportDbfAsync(
        Order order,
        string targetFolder,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ExportResult> ExportXlsAsync(
        Order order,
        string targetFolder,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    OrderExportSnapshot BuildSnapshot(Order order, CadEditorViewModel? cadEditor);
}

public sealed class ExportService : IExportService
{
    private readonly IEnvironmentSettingsService _environmentSettingsService;
    private readonly IDrawingExportService _drawingExportService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ExportService(
        IEnvironmentSettingsService environmentSettingsService,
        IDrawingExportService drawingExportService)
    {
        _environmentSettingsService = environmentSettingsService;
        _drawingExportService = drawingExportService;
    }

    public OrderExportSnapshot BuildSnapshot(Order order, CadEditorViewModel? cadEditor)
    {
        var snapshot = new OrderExportSnapshot
        {
            OrderId = order.Id,
            OrderNumber = order.Header.OrderNumber,
            Header = new OrderHeaderExportNode
            {
                OrderNumber = order.Header.OrderNumber,
                OrderDate = order.Header.OrderDate,
                CustomerName = order.Header.CustomerName,
                CustomerPhone = order.Header.CustomerPhone,
                SiteAddress = order.Header.SiteAddress,
                DealerName = order.Header.DealerName,
                ContractNumber = order.Header.ContractNumber,
                CurrencyCode = order.Header.CurrencyCode,
                Note = order.Header.Note
            },
            Totals = new OrderTotalsExportNode
            {
                ProfitabilityPercent = order.ProfitabilityPercent,
                TotalCost = order.Totals.GrandTotal
            },
            Constructions = order.Constructions
                .OrderBy(construction => construction.Number)
                .Select(BuildConstructionNode)
                .ToList(),
            SketchImageBase64 = TryCaptureSketchBase64(cadEditor)
        };

        return snapshot;
    }

    public async Task<ExportResult> ExportJsonAsync(
        Order order,
        CadEditorViewModel? cadEditor,
        string targetFolder,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateOrderNumber(order);
        if (validation is not null)
        {
            return new ExportResult(false, validation);
        }

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(15);

            Directory.CreateDirectory(targetFolder);
            var snapshot = BuildSnapshot(order, cadEditor);
            var safeNumber = SanitizeFileToken(order.Header.OrderNumber);
            var filePath = Path.Combine(targetFolder, $"order_{safeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            progress?.Report(60);
            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            File.WriteAllText(filePath, json);
            progress?.Report(100);

            return new ExportResult(true, "JSON-снимок заказа успешно сформирован.", filePath, [filePath]);
        }, cancellationToken);
    }

    public async Task<ExportResult> ExportDbfAsync(
        Order order,
        string targetFolder,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateOrderNumber(order);
        if (validation is not null)
        {
            return new ExportResult(false, validation);
        }

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(targetFolder);
            var snapshot = BuildSnapshot(order, cadEditor: null);
            var createdFiles = new List<string>();

            progress?.Report(10);
            WriteBase1(targetFolder, snapshot);
            createdFiles.Add(Path.Combine(targetFolder, "Base1.dbf"));

            progress?.Report(30);
            WriteBase2(targetFolder, snapshot);
            createdFiles.Add(Path.Combine(targetFolder, "Base2.dbf"));

            progress?.Report(50);
            WriteBase3(targetFolder, snapshot);
            createdFiles.Add(Path.Combine(targetFolder, "Base3.dbf"));

            progress?.Report(70);
            WriteBase4(targetFolder, snapshot);
            createdFiles.Add(Path.Combine(targetFolder, "Base4.dbf"));

            progress?.Report(90);
            WriteBase5(targetFolder, snapshot);
            createdFiles.Add(Path.Combine(targetFolder, "Base5.dbf"));

            progress?.Report(100);
            return new ExportResult(true, "Файлы dBASE 5.0 (Base1–Base5) успешно сформированы.", createdFiles[0], createdFiles);
        }, cancellationToken);
    }

    public async Task<ExportResult> ExportXlsAsync(
        Order order,
        string targetFolder,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateOrderNumber(order);
        if (validation is not null)
        {
            return new ExportResult(false, validation);
        }

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(targetFolder);

            var snapshot = BuildSnapshot(order, cadEditor: null);
            var safeNumber = SanitizeFileToken(order.Header.OrderNumber);
            var filePath = Path.Combine(targetFolder, $"spisanie_{safeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.xls");

            progress?.Report(20);
            var worksheets = BuildExcelWorksheets(snapshot);
            progress?.Report(70);
            ExcelSpreadsheetExporter.WriteWorkbook(filePath, worksheets);
            progress?.Report(100);

            return new ExportResult(true, "Документ Excel успешно сформирован.", filePath, [filePath]);
        }, cancellationToken);
    }

    private string? ValidateOrderNumber(Order order)
    {
        if (!_environmentSettingsService.Current.PreventDbfExportWithoutOrderNumber)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(order.Header.OrderNumber)
            ? "Экспорт запрещён: у заказа не задан номер."
            : null;
    }

    private ConstructionExportNode BuildConstructionNode(OrderConstruction construction)
    {
        var materials = BuildMaterialLines(construction);
        return new ConstructionExportNode
        {
            Id = construction.Id,
            Number = construction.Number,
            Name = construction.Name,
            ProfileSystemName = construction.ProfileSystemName,
            WidthMm = construction.Geometry.OuterWidthMm,
            HeightMm = construction.Geometry.OuterHeightMm,
            Quantity = Math.Max(1, construction.Cost.Quantity),
            Elements = construction.Geometry.Elements
                .Select(element => new ElementExportNode
                {
                    Id = element.Id,
                    Kind = element.Kind.ToString(),
                    Label = element.Label,
                    X = (double)element.X,
                    Y = (double)element.Y,
                    WidthMm = (double)element.WidthMm,
                    HeightMm = (double)element.HeightMm
                })
                .ToList(),
            Materials = materials
        };
    }

    private static List<MaterialLineExportNode> BuildMaterialLines(OrderConstruction construction)
    {
        var lines = new List<MaterialLineExportNode>();
        var quantity = Math.Max(1, construction.Cost.Quantity);

        if (construction.SpecificationLines.Count > 0)
        {
            foreach (var line in construction.SpecificationLines)
            {
                lines.Add(new MaterialLineExportNode
                {
                    MaterialId = line.MaterialId,
                    Article = string.IsNullOrWhiteSpace(line.Article) ? "MAT" : line.Article,
                    Name = line.MaterialName,
                    Unit = line.Unit,
                    Quantity = Math.Round(line.Quantity * quantity, 3),
                    UnitPrice = line.UnitPrice,
                    TotalCost = Math.Round(line.LineTotal * quantity, 2),
                    MaterialType = MapMaterialType(line.Category),
                    ConstructionNumber = construction.Number
                });
            }

            return lines;
        }

        if (construction.Geometry.Elements.Count == 0)
        {
            return lines;
        }

        lines.Add(new MaterialLineExportNode
        {
            Article = "MAT-001",
            Name = construction.Name,
            Unit = "шт.",
            Quantity = quantity,
            UnitPrice = 0m,
            TotalCost = 0m,
            MaterialType = "Other",
            ConstructionNumber = construction.Number
        });

        return lines;
    }

    private static string MapMaterialType(CostMaterialCategory category) => category switch
    {
        CostMaterialCategory.Profile => "Profile",
        CostMaterialCategory.Glazing => "Glass",
        CostMaterialCategory.Hardware => "Hardware",
        CostMaterialCategory.Sealant => "Sealant",
        CostMaterialCategory.Installation => "Installation",
        CostMaterialCategory.Labor => "Labor",
        _ => "Other"
    };

    private string? TryCaptureSketchBase64(CadEditorViewModel? cadEditor)
    {
        if (cadEditor is null || !OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            var bitmap = _drawingExportService.RenderAsync(
                cadEditor,
                new CopyDrawingParameters
                {
                    ScalePercent = 100,
                    ResolutionDpi = 96,
                    IncludeAxes = false,
                    IncludeDimensions = false,
                    IncludeDimensionLines = false
                }).GetAwaiter().GetResult();

            if (bitmap is null)
            {
                return null;
            }

            using (bitmap)
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(stream.ToArray());
            }
        }
        catch
        {
            return null;
        }
    }

    private static void WriteBase1(string folder, OrderExportSnapshot snapshot)
    {
        var columns = new List<DbfColumnDefinition>
        {
            new() { Name = "ORDERNO", Length = 20 },
            new() { Name = "ORDERDT", Type = 'D', Length = 8 },
            new() { Name = "CUSTNAME", Length = 80 },
            new() { Name = "DEALER", Length = 40 },
            new() { Name = "CONTRACT", Length = 30 },
            new() { Name = "CURRENCY", Length = 8 },
            new() { Name = "TOTAL", Type = 'N', Length = 12, DecimalCount = 2 }
        };

        DbfFileWriter.Write(
            Path.Combine(folder, "Base1.dbf"),
            columns,
            [
                new Dictionary<string, object?>
                {
                    ["ORDERNO"] = snapshot.OrderNumber,
                    ["ORDERDT"] = snapshot.Header.OrderDate,
                    ["CUSTNAME"] = snapshot.Header.CustomerName,
                    ["DEALER"] = snapshot.Header.DealerName,
                    ["CONTRACT"] = snapshot.Header.ContractNumber,
                    ["CURRENCY"] = snapshot.Header.CurrencyCode,
                    ["TOTAL"] = snapshot.Totals.TotalCost
                }
            ]);
    }

    private static void WriteBase2(string folder, OrderExportSnapshot snapshot)
    {
        var columns = new List<DbfColumnDefinition>
        {
            new() { Name = "ORDERNO", Length = 20 },
            new() { Name = "ARTICLE", Length = 20 },
            new() { Name = "NAME", Length = 80 },
            new() { Name = "UNIT", Length = 8 },
            new() { Name = "QTY", Type = 'N', Length = 12, DecimalCount = 3 },
            new() { Name = "PRICE", Type = 'N', Length = 12, DecimalCount = 2 },
            new() { Name = "SUMMA", Type = 'N', Length = 12, DecimalCount = 2 }
        };

        var aggregated = snapshot.Constructions
            .SelectMany(construction => construction.Materials)
            .GroupBy(line => $"{line.Article}|{line.Name}|{line.Unit}")
            .Select(group =>
            {
                var first = group.First();
                var qty = group.Sum(item => item.Quantity);
                var sum = group.Sum(item => item.TotalCost);
                return new Dictionary<string, object?>
                {
                    ["ORDERNO"] = snapshot.OrderNumber,
                    ["ARTICLE"] = first.Article,
                    ["NAME"] = first.Name,
                    ["UNIT"] = first.Unit,
                    ["QTY"] = qty,
                    ["PRICE"] = first.UnitPrice,
                    ["SUMMA"] = sum
                };
            })
            .ToList();

        DbfFileWriter.Write(Path.Combine(folder, "Base2.dbf"), columns, aggregated);
    }

    private static void WriteBase3(string folder, OrderExportSnapshot snapshot)
    {
        var columns = new List<DbfColumnDefinition>
        {
            new() { Name = "ORDERNO", Length = 20 },
            new() { Name = "CONSTR", Type = 'N', Length = 4, DecimalCount = 0 },
            new() { Name = "ARTICLE", Length = 20 },
            new() { Name = "NAME", Length = 80 },
            new() { Name = "UNIT", Length = 8 },
            new() { Name = "QTY", Type = 'N', Length = 12, DecimalCount = 3 },
            new() { Name = "SUMMA", Type = 'N', Length = 12, DecimalCount = 2 }
        };

        var rows = snapshot.Constructions
            .SelectMany(construction => construction.Materials.Select(material => new Dictionary<string, object?>
            {
                ["ORDERNO"] = snapshot.OrderNumber,
                ["CONSTR"] = construction.Number,
                ["ARTICLE"] = material.Article,
                ["NAME"] = material.Name,
                ["UNIT"] = material.Unit,
                ["QTY"] = material.Quantity,
                ["SUMMA"] = material.TotalCost
            }))
            .ToList();

        DbfFileWriter.Write(Path.Combine(folder, "Base3.dbf"), columns, rows);
    }

    private static void WriteBase4(string folder, OrderExportSnapshot snapshot)
    {
        var columns = new List<DbfColumnDefinition>
        {
            new() { Name = "ORDERNO", Length = 20 },
            new() { Name = "CONSTR", Type = 'N', Length = 4, DecimalCount = 0 },
            new() { Name = "NAME", Length = 80 },
            new() { Name = "PROFILE", Length = 40 },
            new() { Name = "WIDTH", Type = 'N', Length = 10, DecimalCount = 1 },
            new() { Name = "HEIGHT", Type = 'N', Length = 10, DecimalCount = 1 },
            new() { Name = "QTY", Type = 'N', Length = 6, DecimalCount = 0 }
        };

        var rows = snapshot.Constructions
            .Select(construction => new Dictionary<string, object?>
            {
                ["ORDERNO"] = snapshot.OrderNumber,
                ["CONSTR"] = construction.Number,
                ["NAME"] = construction.Name,
                ["PROFILE"] = construction.ProfileSystemName,
                ["WIDTH"] = construction.WidthMm,
                ["HEIGHT"] = construction.HeightMm,
                ["QTY"] = construction.Quantity
            })
            .ToList();

        DbfFileWriter.Write(Path.Combine(folder, "Base4.dbf"), columns, rows);
    }

    private static void WriteBase5(string folder, OrderExportSnapshot snapshot)
    {
        var columns = new List<DbfColumnDefinition>
        {
            new() { Name = "ORDERNO", Length = 20 },
            new() { Name = "CONSTR", Type = 'N', Length = 4, DecimalCount = 0 },
            new() { Name = "FORMULA", Length = 30 },
            new() { Name = "WIDTH", Type = 'N', Length = 10, DecimalCount = 1 },
            new() { Name = "HEIGHT", Type = 'N', Length = 10, DecimalCount = 1 },
            new() { Name = "AREA", Type = 'N', Length = 10, DecimalCount = 3 }
        };

        var rows = snapshot.Constructions
            .SelectMany(construction => construction.Elements
                .Where(element => string.Equals(element.Kind, GeometryElementKind.Glazing.ToString(), StringComparison.Ordinal))
                .Select(element => new Dictionary<string, object?>
                {
                    ["ORDERNO"] = snapshot.OrderNumber,
                    ["CONSTR"] = construction.Number,
                    ["FORMULA"] = element.Label,
                    ["WIDTH"] = element.WidthMm,
                    ["HEIGHT"] = element.HeightMm,
                    ["AREA"] = Math.Round((decimal)(element.WidthMm * element.HeightMm) / 1_000_000m, 3)
                }))
            .ToList();

        if (rows.Count == 0)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["ORDERNO"] = snapshot.OrderNumber,
                ["CONSTR"] = 1,
                ["FORMULA"] = string.Empty,
                ["WIDTH"] = 0m,
                ["HEIGHT"] = 0m,
                ["AREA"] = 0m
            });
        }

        DbfFileWriter.Write(Path.Combine(folder, "Base5.dbf"), columns, rows);
    }

    private static IReadOnlyList<ExcelWorksheetData> BuildExcelWorksheets(OrderExportSnapshot snapshot)
    {
        var summaryHeaders = new[] { "Номер заказа", "Дата", "Заказчик", "Дилер", "Валюта", "Итого" };
        var summaryRows = new[]
        {
            new[]
            {
                snapshot.OrderNumber,
                snapshot.Header.OrderDate.ToString("dd.MM.yyyy"),
                snapshot.Header.CustomerName,
                snapshot.Header.DealerName,
                snapshot.Header.CurrencyCode,
                snapshot.Totals.TotalCost.ToString("F2")
            }
        };

        var materialHeaders = new[] { "Артикул", "Наименование", "Ед.", "Кол-во", "Цена", "Сумма" };
        var base2Rows = snapshot.Constructions
            .SelectMany(c => c.Materials)
            .GroupBy(line => $"{line.Article}|{line.Name}")
            .Select(group =>
            {
                var first = group.First();
                return new[]
                {
                    first.Article,
                    first.Name,
                    first.Unit,
                    group.Sum(item => item.Quantity).ToString("F3"),
                    first.UnitPrice.ToString("F2"),
                    group.Sum(item => item.TotalCost).ToString("F2")
                };
            })
            .ToList();

        var base3Rows = snapshot.Constructions
            .SelectMany(construction => construction.Materials.Select(material => new[]
            {
                construction.Number.ToString(),
                material.Article,
                material.Name,
                material.Unit,
                material.Quantity.ToString("F3"),
                material.TotalCost.ToString("F2")
            }))
            .ToList();

        var base4Rows = snapshot.Constructions
            .Select(construction => new[]
            {
                construction.Number.ToString(),
                construction.Name,
                construction.ProfileSystemName,
                construction.WidthMm.ToString("F1"),
                construction.HeightMm.ToString("F1"),
                construction.Quantity.ToString()
            })
            .ToList();

        var base5Rows = snapshot.Constructions
            .SelectMany(construction => construction.Elements
                .Where(element => element.Kind.Contains("Glaz", StringComparison.OrdinalIgnoreCase)
                    || element.Kind.Contains("Glass", StringComparison.OrdinalIgnoreCase))
                .Select(element => new[]
                {
                    construction.Number.ToString(),
                    element.Label,
                    element.WidthMm.ToString("F1"),
                    element.HeightMm.ToString("F1"),
                    Math.Round(element.WidthMm * element.HeightMm / 1_000_000d, 3).ToString("F3")
                }))
            .ToList();

        return
        [
            new ExcelWorksheetData
            {
                Name = "Итог (Base1)",
                Headers = summaryHeaders,
                Rows = summaryRows
            },
            new ExcelWorksheetData
            {
                Name = "Списание общее (Base2)",
                Headers = materialHeaders,
                Rows = base2Rows
            },
            new ExcelWorksheetData
            {
                Name = "Поконструкционно (Base3)",
                Headers = ["Констр.", ..materialHeaders],
                Rows = base3Rows
            },
            new ExcelWorksheetData
            {
                Name = "Конструкции (Base4)",
                Headers = ["№", "Наименование", "Профиль", "Ширина", "Высота", "Кол-во"],
                Rows = base4Rows
            },
            new ExcelWorksheetData
            {
                Name = "Стеклопакеты (Base5)",
                Headers = ["Констр.", "Формула", "Ширина", "Высота", "Площадь"],
                Rows = base5Rows
            }
        ];
    }

    private static string SanitizeFileToken(string value)
    {
        var token = string.IsNullOrWhiteSpace(value) ? "bez_nomera" : value.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            token = token.Replace(invalid, '_');
        }

        return token.Replace('#', '_').Replace(' ', '_');
    }
}
