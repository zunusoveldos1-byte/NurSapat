using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Snapping;
using NurSapat.Cad.Core.Viewport;
using System;

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