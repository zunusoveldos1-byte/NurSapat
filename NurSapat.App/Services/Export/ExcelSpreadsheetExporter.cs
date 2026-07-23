using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace NurSapat.App.Services.Export;

internal static class ExcelSpreadsheetExporter
{
    private static readonly XNamespace SpreadsheetMl = "urn:schemas-microsoft-com:office:spreadsheet";
    private static readonly XNamespace OfficeMl = "urn:schemas-microsoft-com:office:office";
    private static readonly XNamespace ExcelMl = "urn:schemas-microsoft-com:office:excel";

    public static void WriteWorkbook(string filePath, IReadOnlyList<ExcelWorksheetData> worksheets)
    {
        var workbook = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XProcessingInstruction("mso-application", "progid=\"Excel.Sheet\""),
            new XElement(SpreadsheetMl + "Workbook",
                new XAttribute(XNamespace.Xmlns + "o", OfficeMl),
                new XAttribute(XNamespace.Xmlns + "x", SpreadsheetMl),
                new XAttribute(XNamespace.Xmlns + "ss", SpreadsheetMl),
                new XAttribute(XNamespace.Xmlns + "html", "http://www.w3.org/TR/REC-html40"),
                worksheets.Select(BuildWorksheet)));

        File.WriteAllText(filePath, workbook.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static XElement BuildWorksheet(ExcelWorksheetData worksheet)
    {
        var rows = new List<XElement>
        {
            BuildRow(worksheet.Headers, isHeader: true)
        };

        rows.AddRange(worksheet.Rows.Select(row => BuildRow(row, isHeader: false)));

        return new XElement(SpreadsheetMl + "Worksheet",
            new XAttribute(ExcelMl + "Name", worksheet.Name),
            new XElement(SpreadsheetMl + "Table", rows));
    }

    private static XElement BuildRow(IReadOnlyList<string> cells, bool isHeader)
    {
        return new XElement(SpreadsheetMl + "Row",
            cells.Select(cell => new XElement(SpreadsheetMl + "Cell",
                isHeader ? new XAttribute(SpreadsheetMl + "StyleID", "Header") : null,
                new XElement(SpreadsheetMl + "Data",
                    new XAttribute(SpreadsheetMl + "Type", InferType(cell)),
                    cell))));
    }

    private static string InferType(string value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
            ? "Number"
            : "String";
}

internal sealed class ExcelWorksheetData
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> Headers { get; init; }

    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}
