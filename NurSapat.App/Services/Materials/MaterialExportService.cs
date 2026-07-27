using System.Text;
using NurSapat.App.ViewModels.Materials;

namespace NurSapat.App.Services.Materials;

public interface IMaterialExportService
{
    Task ExportToExcelAsync(IReadOnlyList<MaterialListItemViewModel> items, string filePath);

    Task ExportToTxtAsync(IReadOnlyList<MaterialListItemViewModel> items, string filePath);
}

public sealed class MaterialExportService : IMaterialExportService
{
    private static readonly string[] Headers =
    [
        "Артикул",
        "Наименование",
        "Раздел",
        "Производитель",
        "Ед. изм.",
        "Валюта",
        "Цена",
        "Отход %"
    ];

    public async Task ExportToExcelAsync(IReadOnlyList<MaterialListItemViewModel> items, string filePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(';', Headers));

        foreach (var item in items)
        {
            builder.AppendLine(string.Join(';',
                Escape(item.Article),
                Escape(item.Name),
                Escape(item.SectionDisplay),
                Escape(item.Manufacturer),
                Escape(item.UnitDisplay),
                Escape(item.CurrencyDisplay),
                Escape(item.PriceDisplay),
                item.WastePercent.ToString("F2")));
        }

        await File.WriteAllTextAsync(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public async Task ExportToTxtAsync(IReadOnlyList<MaterialListItemViewModel> items, string filePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join('\t', Headers));

        foreach (var item in items)
        {
            builder.AppendLine(string.Join('\t',
                item.Article,
                item.Name,
                item.SectionDisplay,
                item.Manufacturer,
                item.UnitDisplay,
                item.CurrencyDisplay,
                item.PriceDisplay,
                item.WastePercent.ToString("F2")));
        }

        await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8);
    }

    private static string Escape(string value) =>
        value.Contains(';') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
