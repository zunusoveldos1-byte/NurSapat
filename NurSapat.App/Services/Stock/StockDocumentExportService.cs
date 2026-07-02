using NurSapat.App.Models.Stock;
using System.Text;

namespace NurSapat.App.Services.Stock;

public interface IStockDocumentExportService
{
    Task<string?> ExportAsync(StockDocumentHeader document);
}

public sealed class StockDocumentExportService : IStockDocumentExportService
{
    public async Task<string?> ExportAsync(StockDocumentHeader document)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "NurSapat",
            "StockExports");

        Directory.CreateDirectory(folder);

        var safeNumber = string.IsNullOrWhiteSpace(document.Number)
            ? document.Id.ToString("N")[..8]
            : document.Number.Replace('/', '-').Replace('\\', '-');

        var path = Path.Combine(folder, $"{safeNumber}_{document.Date:yyyyMMdd}.txt");
        var content = BuildContent(document);
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        return path;
    }

    private static string BuildContent(StockDocumentHeader document)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Документ: {StockDocumentTypeLabels.GetDisplay(document.Type)}");
        builder.AppendLine($"Номер: {document.Number}");
        builder.AppendLine($"Дата: {document.Date:dd.MM.yyyy}");
        builder.AppendLine($"Примечание: {document.Note}");
        builder.AppendLine($"Свои курсы валют: {(document.UseCustomExchangeRates ? "Да" : "Нет")}");
        builder.AppendLine();
        builder.AppendLine("Артикул;Наименование;Цвет;Ед.;Кол-во;Цена;Валюта;Сумма");

        foreach (var line in document.Lines)
        {
            builder.AppendLine(
                $"{line.Article};{line.Name};{line.Color};{line.Unit};{line.Quantity:N3};{line.Price:N2};{line.Currency};{line.TotalSum:N2}");
        }

        builder.AppendLine();
        builder.AppendLine($"Итого по документу: {document.DocumentTotalSum:N2}");
        return builder.ToString();
    }
}
