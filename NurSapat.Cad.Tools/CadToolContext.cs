using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Snapping;
using NurSapat.Cad.Core.Viewport;

namespace NurSapat.Cad.Tools;

public sealed class CadToolContext
{
    public CadDocument Document { get; }
    public CadCommandManager CommandManager { get; }
    public CadSnappingEngine Snapping { get; }
    public CadViewport Viewport { get; set; } = null!;
    public Action? Invalidate { get; set; }
    public Action<CadPoint>? InvokeSizeInput { get; set; }
    public Action<DesignPlacementCommit>? CommitPlacement { get; set; }
    public string? ActiveDesignToolId { get; set; }
    public string? ActiveDesignToolLabel { get; set; }
    public Func<CadPoint, CadPoint>? SnapPoint { get; set; }

    /// <summary>Толщина профиля рамы (мм) для расчёта светового проёма.</summary>
    public double FrameProfileThickness { get; set; } = 70;

    /// <summary>Активный режим выделения (для инструментов группы 2).</summary>
    public CadSelectionMode ActiveSelectionMode { get; set; }

    /// <summary>Подсвечиваемая цель при наведении в режиме выделения.</summary>
    public CadSelectionTarget? HighlightedTarget { get; set; }

    public Action<string>? SetStatus { get; set; }

    public Func<bool>? TryUndo { get; set; }
    public Func<bool>? TryDeleteLastPlaced { get; set; }
    public Func<bool>? TryDeleteSelection { get; set; }
    public Func<bool>? TryClearConstruction { get; set; }
    public Func<bool>? TryAlignGlazingUnits { get; set; }
    public Func<bool>? TryAlignDeadLights { get; set; }
    public Func<bool>? TryAlignImposts { get; set; }
    public Func<CadPoint, (double X, double Y)?>? ShowVertexCoordinateDialog { get; set; }
    public Func<CadPoint, bool>? TrySetVertexCoordinates { get; set; }
    public Func<bool>? TryChangeOverallDimensions { get; set; }
    public Func<bool>? TryRecalculateConstruction { get; set; }
    public Func<bool>? TryShowObjectProperties { get; set; }
    public Func<bool>? TryShowConstructionDetails { get; set; }
    public Func<CadPoint, bool>? TryPressArcVertex { get; set; }
    public Func<bool>? TryRemoveArcVertex { get; set; }
    public Func<CadPoint, bool>? TrySplitArcAtPoint { get; set; }
    public Func<bool>? TrySplitProfile { get; set; }
    public Func<bool>? TrySetArcRadius { get; set; }
    public Func<bool>? TryCopyConstruction { get; set; }
    public Func<bool>? TryPasteConstruction { get; set; }
    public Func<bool>? TryAddConstructionToStandard { get; set; }
    public Func<bool>? TryInsertConstructionFromStandard { get; set; }
    public Func<CadPoint, CadSelectionMode, CadSelectionTarget?>? HitTestSelection { get; set; }
    public Action<CadSelectionTarget>? SelectTarget { get; set; }

    public Func<FrameNode?>? GetFrameRoot { get; set; }

    public Func<double, double>? SnapAxis { get; set; }

    public Func<CadPoint, bool>? HasConstructionElementAtPoint { get; set; }
    public Func<CadPoint, bool, bool>? HandleConstructionClickSelection { get; set; }
    public Action<CadRect, bool>? HandleConstructionBoxSelection { get; set; }

    public CadToolContext(CadDocument document, CadCommandManager commands, CadSnappingEngine snapping)
    {
        Document = document;
        CommandManager = commands;
        Snapping = snapping;
    }

    // Простой HitTest для прямоугольников (вместо сложного из Parametric)
    public CadRect? HitTest(CadPoint point)
    {
        // Ищем первый прямоугольник, содержащий точку
        foreach (var rect in Document.Rectangles)
        {
            if (rect.Contains(point))
                return rect;
        }
        return null;
    }

    public IReadOnlyList<CadRect> SelectWithin(CadRect selectionBounds)
    {
        var selected = Document.Rectangles
            .Where(rect => rect.Intersects(selectionBounds))
            .ToArray();
        return selected;
    }
}