using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool TryChangeOverallDimensionsFromTool()
    {
        if (FrameRoot is null)
        {
            StatusText = "Сначала создайте конструкцию";
            return false;
        }

        _ = ChangeOverallDimensionsFromToolAsync();
        return true;
    }

    private async Task ChangeOverallDimensionsFromToolAsync()
    {
        if (FrameRoot is null)
        {
            return;
        }

        try
        {
            var result = await _dialogService.ShowSizeInputAsync(
                FrameRoot.Width,
                FrameRoot.Height,
                FrameRoot.X,
                FrameRoot.Y);
            if (result is null)
            {
                StatusText = "Изменение габаритов отменено";
                return;
            }

            var width = CadMath.RoundMm(result.Width);
            var height = CadMath.RoundMm(result.Height);
            var originX = CadMath.RoundMm(result.X);
            var originY = CadMath.RoundMm(result.Y);
            if (width < MinEditableOpeningMm || height < MinEditableOpeningMm)
            {
                StatusText = $"Минимальный проём — {MinEditableOpeningMm:0} мм";
                return;
            }

            var before = CaptureConstructionMemento();
            var applied = RecordPlacement(
                "Габариты конструкции изменены",
                () =>
                {
                    var thickness = FrameRoot.IsFictitiousContainer ? 0 : FrameThickness;
                    var sizeChanged = Math.Abs(width - FrameRoot.Width) > CadMath.Epsilon
                                      || Math.Abs(height - FrameRoot.Height) > CadMath.Epsilon;
                    if (sizeChanged && !FrameRoot.TryResizeOuter(width, height, thickness))
                    {
                        ApplyConstructionMemento(before);
                        return false;
                    }

                    var deltaX = originX - FrameRoot.X;
                    var deltaY = originY - FrameRoot.Y;
                    if (Math.Abs(deltaX) > CadMath.Epsilon || Math.Abs(deltaY) > CadMath.Epsilon)
                    {
                        FrameRoot.TranslateSubtree(deltaX, deltaY);
                    }

                    RecalculateFrameGeometry();
                    SyncFrameGeometryToDocument();
                    return true;
                });

            if (!applied)
            {
                RefreshAfterPlacementHistoryChange();
                StatusText = "Невозможно изменить габариты без нарушения геометрии";
                return;
            }

            Length = width;
            Height = height;
            CoordinateX = originX;
            CoordinateY = originY;
            NotifyFrameSelectionProperties();
            RefreshAfterPlacementHistoryChange();
            StatusText = $"Габариты: {width:0}×{height:0} мм";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("ChangeOverallDimensionsFromToolAsync failed", ex);
            StatusText = "Не удалось изменить габариты";
        }
    }

    private bool TryRecalculateConstructionFromTool()
    {
        if (FrameRoot is null && CanvasElements.Count == 0)
        {
            StatusText = "Нет конструкции для пересчёта";
            return false;
        }

        RecalculateFrameGeometry();
        SyncFrameGeometryToDocument();
        StatusText = "Конструкция пересчитана";
        return true;
    }
}
