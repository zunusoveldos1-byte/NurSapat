using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NurSapat.App.Models.EditDialogs;
using NurSapat.Cad.UI.Controls;
using NurSapat.Cad.UI.ViewModels;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using DrawingBitmap = System.Drawing.Bitmap;
using AvaloniaSize = Avalonia.Size;

namespace NurSapat.App.Services.EditDialogs;

public interface IDrawingExportService
{
    Task<DrawingBitmap?> RenderAsync(
        CadEditorViewModel editor,
        CopyDrawingParameters parameters,
        CancellationToken cancellationToken = default);

    Task CopyToClipboardAsync(
        CadEditorViewModel editor,
        CopyDrawingParameters parameters,
        CancellationToken cancellationToken = default);
}

[SupportedOSPlatform("windows")]
public sealed class DrawingExportService : IDrawingExportService
{
    [SupportedOSPlatform("windows")]
    public async Task<DrawingBitmap?> RenderAsync(
        CadEditorViewModel editor,
        CopyDrawingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var renderTarget = RenderOnUiThread(editor, parameters);
            return renderTarget is null ? null : ConvertToBmp(renderTarget);
        });
    }

    public async Task CopyToClipboardAsync(
        CadEditorViewModel editor,
        CopyDrawingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
                || desktop.MainWindow?.Clipboard is not { } clipboard)
            {
                return;
            }

            using var renderTarget = RenderOnUiThread(editor, parameters);
            if (renderTarget is null)
            {
                return;
            }

            await clipboard.SetBitmapAsync(renderTarget);
        });
    }

    [SupportedOSPlatform("windows")]
    private static DrawingBitmap? ConvertToBmp(RenderTargetBitmap renderTarget)
    {
        using var pngStream = new MemoryStream();
        renderTarget.Save(pngStream);
        pngStream.Position = 0;

        using var decoded = new DrawingBitmap(pngStream);
        var bmpStream = new MemoryStream();
        decoded.Save(bmpStream, ImageFormat.Bmp);
        bmpStream.Position = 0;
        return new DrawingBitmap(bmpStream);
    }

    private static RenderTargetBitmap? RenderOnUiThread(
        CadEditorViewModel editor,
        CopyDrawingParameters parameters)
    {
        var bounds = editor.GetConstructionContentBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            bounds = new NurSapat.Cad.Core.Geometry.CadRect(0, 0, 1000, 1000);
        }

        var scale = Math.Max(parameters.ScalePercent, 10) / 100.0;
        var dpi = Math.Max(parameters.ResolutionDpi, 72);
        var pixelsPerMm = dpi / 25.4 * scale;

        var width = Math.Clamp((int)Math.Ceiling(bounds.Width * pixelsPerMm), 320, 4096);
        var height = Math.Clamp((int)Math.Ceiling(bounds.Height * pixelsPerMm), 240, 4096);

        var previousShowDimensions = editor.ShowDimensions;
        var previousGrid = editor.IsGridEnabled;
        var previousViewOnly = editor.IsViewOnlyMode;

        try
        {
            editor.IsViewOnlyMode = true;
            editor.IsGridEnabled = parameters.IncludeAxes;
            editor.ShowDimensions = parameters.IncludeDimensionLines && parameters.IncludeDimensions;

            var canvas = new CadCanvasControl
            {
                Editor = editor,
                IsThumbnailPreview = !parameters.IncludeDimensionLines,
                Width = width,
                Height = height
            };

            editor.Viewport.Resize(width, height);
            editor.FitThumbnailPreview(marginPx: 24, boundsPaddingRatio: 0.2);
            editor.RequestSceneRefresh();

            canvas.Measure(new AvaloniaSize(width, height));
            canvas.Arrange(new Rect(0, 0, width, height));
            canvas.UpdateLayout();

            var renderTarget = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
            renderTarget.Render(canvas);
            return renderTarget;
        }
        finally
        {
            editor.ShowDimensions = previousShowDimensions;
            editor.IsGridEnabled = previousGrid;
            editor.IsViewOnlyMode = previousViewOnly;
            editor.RequestSceneRefresh();
        }
    }
}
