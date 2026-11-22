using NurSapat.App.Models.EditDialogs;
using NurSapat.App.Services.EditDialogs;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Project;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace NurSapat.App.Services.Documents;

public interface IPrintDocumentRenderer
{
    Task PrintAsync(
        CadEditorViewModel cadEditor,
        PrintSettings settings,
        string? printerName,
        int copies,
        CancellationToken cancellationToken = default);
}

[SupportedOSPlatform("windows")]
public sealed class PrintDocumentRenderer : IPrintDocumentRenderer
{
    private readonly IDrawingExportService _drawingExportService;

    public PrintDocumentRenderer(IDrawingExportService drawingExportService)
    {
        _drawingExportService = drawingExportService;
    }

    public async Task PrintAsync(
        CadEditorViewModel cadEditor,
        PrintSettings settings,
        string? printerName,
        int copies,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var parameters = new CopyDrawingParameters
        {
            ScalePercent = (int)settings.ScalePercent,
            ResolutionDpi = 150,
            IncludeDimensions = settings.PrintDimensions,
            IncludeDimensionLines = settings.PrintDimensions,
            IncludeAxes = settings.PrintCoordinates
        };

        using var bitmap = await _drawingExportService.RenderAsync(cadEditor, parameters, cancellationToken);
        if (bitmap is null)
        {
            throw new InvalidOperationException("Не удалось сформировать изображение для печати.");
        }

        using var printDocument = new PrintDocument();
        if (!string.IsNullOrWhiteSpace(printerName)
            && !string.Equals(printerName, "Принтер по умолчанию", StringComparison.OrdinalIgnoreCase))
        {
            printDocument.PrinterSettings.PrinterName = printerName;
        }

        printDocument.PrinterSettings.Copies = (short)Math.Clamp(copies, 1, 99);
        printDocument.DefaultPageSettings.Landscape = settings.Orientation == PageOrientation.Landscape;

        printDocument.PrintPage += (_, args) =>
        {
            var bounds = args.PageBounds;
            var ratio = Math.Min(
                bounds.Width / (float)bitmap.Width,
                bounds.Height / (float)bitmap.Height);
            var width = (int)(bitmap.Width * ratio);
            var height = (int)(bitmap.Height * ratio);
            var x = bounds.Left + (bounds.Width - width) / 2;
            var y = bounds.Top + (bounds.Height - height) / 2;
            args.Graphics?.DrawImage(bitmap, x, y, width, height);
            args.HasMorePages = false;
        };

        printDocument.Print();
    }
}
