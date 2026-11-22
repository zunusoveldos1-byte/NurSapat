using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>Отрисовка декоративных шпросов поверх стеклопакета.</summary>
internal static class DecorativeSpokeRenderer
{
    private static readonly Dictionary<string, (byte R, byte G, byte B)> ColorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [DecorativeSpokeElement.Colors.Gold] = (212, 175, 55),
        [DecorativeSpokeElement.Colors.White] = (245, 245, 245),
        [DecorativeSpokeElement.Colors.Brown] = (139, 90, 43)
    };

    public static void DrawLayer(
        DrawingContext context,
        CadEditorViewModel editor,
        IReadOnlyList<GeometryElement> elements)
    {
        foreach (var element in elements)
        {
            if (element.Kind != GeometryElementKind.DecorativeSpoke)
            {
                continue;
            }

            if (!DecorativeSpokeElement.TryFromGeometryElement(element, out var spoke))
            {
                continue;
            }

            var glazing = elements.FirstOrDefault(e => e.Id == spoke.ParentGlazingId && e.Kind == GeometryElementKind.Glazing);
            if (glazing is null ||
                !SashGeometry.TryResolveGlassForGlazingElement(editor, glazing, elements, out var glass))
            {
                DrawFromStoredBounds(context, editor, spoke);
                continue;
            }

            DrawSpoke(context, editor, spoke, glass);
        }
    }

    private static void DrawFromStoredBounds(
        DrawingContext context,
        CadEditorViewModel editor,
        DecorativeSpokeElement spoke)
    {
        var glass = SashGeometry.GlassOpeningBounds.FromRect(
            spoke.CenterX - spoke.WidthMm / 2.0,
            spoke.CenterY - spoke.HeightMm / 2.0,
            spoke.CenterX + spoke.WidthMm / 2.0,
            spoke.CenterY + spoke.HeightMm / 2.0);
        if (spoke.IsArched && spoke.ArcRadius > 0)
        {
            glass = new SashGeometry.GlassOpeningBounds
            {
                Left = glass.Left,
                Bottom = glass.Bottom,
                Right = glass.Right,
                Top = glass.Top,
                IsArched = true,
                ArcCenter = new CadPoint(spoke.ArcCenterX, spoke.ArcCenterY),
                ArcRadius = spoke.ArcRadius,
                BottomLeft = glass.BottomLeft,
                BottomRight = glass.BottomRight,
                SpringingLeft = new CadPoint(glass.Left, glass.Top),
                SpringingRight = new CadPoint(glass.Right, glass.Top)
            };
        }

        DrawSpoke(context, editor, spoke, glass);
    }

    private static void DrawSpoke(
        DrawingContext context,
        CadEditorViewModel editor,
        DecorativeSpokeElement spoke,
        SashGeometry.GlassOpeningBounds glass)
    {
        var pen = CreatePen(spoke);
        switch (spoke.SpokeType)
        {
            case DecorativeSpokeType.StandardGrid:
                DrawStandardGrid(context, editor, glass, pen, spoke.RowsCount, spoke.ColumnsCount);
                break;
            case DecorativeSpokeType.Sunburst:
                DrawSunburst(context, editor, glass, pen, spoke);
                break;
            case DecorativeSpokeType.ArchContour:
                DrawArchContour(context, editor, glass, pen, spoke);
                break;
            case DecorativeSpokeType.Diamond:
                DrawDiamond(context, editor, glass, pen);
                break;
        }
    }

    private static IPen CreatePen(DecorativeSpokeElement spoke)
    {
        var color = ColorMap.TryGetValue(spoke.Color, out var rgb)
            ? rgb
            : ColorMap[DecorativeSpokeElement.Colors.White];
        var thicknessPx = Math.Max(1.0, spoke.Thickness / 6.0);
        return new Pen(new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B)), thicknessPx)
        {
            LineCap = PenLineCap.Round
        };
    }

    private static void DrawStandardGrid(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        IPen pen,
        int rows,
        int columns)
    {
        rows = Math.Max(1, rows);
        columns = Math.Max(1, columns);

        for (var column = 1; column < columns; column++)
        {
            var x = glass.Left + glass.Width * column / columns;
            DrawVerticalSegment(context, editor, glass, x, pen);
        }

        for (var row = 1; row < rows; row++)
        {
            var y = glass.Bottom + glass.Height * row / rows;
            DrawHorizontalSegment(context, editor, glass, y, pen);
        }
    }

    private static void DrawSunburst(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        IPen pen,
        DecorativeSpokeElement spoke)
    {
        var rayCount = Math.Max(2, spoke.RayCount);
        NurSapat.Cad.Core.Geometry.CadPoint center;
        if (glass.IsArched)
        {
            center = glass.ArcCenter;
        }
        else
        {
            center = new CadPoint(
                (glass.Left + glass.Right) / 2.0,
                glass.Bottom);
        }

        for (var i = 0; i < rayCount; i++)
        {
            var t = rayCount == 1 ? 0.5 : (double)i / (rayCount - 1);
            var targetX = glass.Left + glass.Width * t;
            var targetY = glass.ResolveTopAtX(targetX);
            context.DrawLine(
                pen,
                WorldToScreen(editor, center),
                WorldToScreen(editor, new CadPoint(targetX, targetY)));
        }
    }

    private static void DrawArchContour(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        IPen pen,
        DecorativeSpokeElement spoke)
    {
        if (!glass.IsArched || glass.ArcRadius <= 0)
        {
            var midY = glass.Bottom + glass.Height * 0.35;
            context.DrawLine(
                pen,
                WorldToScreen(editor, new CadPoint(glass.Left, midY)),
                WorldToScreen(editor, new CadPoint(glass.Right, midY)));
            return;
        }

        var inset = Math.Min(glass.ArcRadius * 0.25, 80);
        var innerRadius = Math.Max(glass.ArcRadius - inset, 1);
        var startAngle = CircularArc.AngleRad(glass.ArcCenter, glass.SpringingLeft);
        var endAngle = CircularArc.AngleRad(glass.ArcCenter, glass.SpringingRight);
        var previous = WorldToScreen(
            editor,
            ProfileSegment.GetPointOnUpperArc(glass.ArcCenter, innerRadius, glass.SpringingLeft.X));
        foreach (var worldPoint in CircularArc.TessellateSpringingArc(
                     glass.ArcCenter, innerRadius, startAngle, endAngle))
        {
            var current = WorldToScreen(editor, worldPoint);
            context.DrawLine(pen, previous, current);
            previous = current;
        }
    }

    private static void DrawDiamond(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        IPen pen)
    {
        var topMid = new CadPoint((glass.Left + glass.Right) / 2.0, glass.ResolveTopAtX((glass.Left + glass.Right) / 2.0));
        var bottomMid = new CadPoint((glass.Left + glass.Right) / 2.0, glass.Bottom);
        var leftMid = new CadPoint(glass.Left, (glass.Bottom + glass.Top) / 2.0);
        var rightMid = new CadPoint(glass.Right, (glass.Bottom + glass.Top) / 2.0);

        context.DrawLine(pen, WorldToScreen(editor, glass.BottomLeft), WorldToScreen(editor, topMid));
        context.DrawLine(pen, WorldToScreen(editor, glass.BottomRight), WorldToScreen(editor, topMid));
        context.DrawLine(pen, WorldToScreen(editor, leftMid), WorldToScreen(editor, rightMid));
        context.DrawLine(pen, WorldToScreen(editor, topMid), WorldToScreen(editor, bottomMid));
        context.DrawLine(pen, WorldToScreen(editor, leftMid), WorldToScreen(editor, topMid));
        context.DrawLine(pen, WorldToScreen(editor, rightMid), WorldToScreen(editor, topMid));
        context.DrawLine(pen, WorldToScreen(editor, leftMid), WorldToScreen(editor, bottomMid));
        context.DrawLine(pen, WorldToScreen(editor, rightMid), WorldToScreen(editor, bottomMid));
    }

    private static void DrawVerticalSegment(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        double x,
        IPen pen)
    {
        var topY = glass.ResolveTopAtX(x);
        context.DrawLine(
            pen,
            WorldToScreen(editor, new CadPoint(x, glass.Bottom)),
            WorldToScreen(editor, new CadPoint(x, topY)));
    }

    private static void DrawHorizontalSegment(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass,
        double y,
        IPen pen)
    {
        if (y > glass.Top + CadMath.Epsilon)
        {
            return;
        }

        var leftX = glass.Left;
        var rightX = glass.Right;
        if (glass.IsArched)
        {
            leftX = ResolveArcIntersectionX(glass, y, left: true);
            rightX = ResolveArcIntersectionX(glass, y, left: false);
        }

        if (rightX > leftX + CadMath.Epsilon)
        {
            context.DrawLine(
                pen,
                WorldToScreen(editor, new CadPoint(leftX, y)),
                WorldToScreen(editor, new CadPoint(rightX, y)));
        }
    }

    private static double ResolveArcIntersectionX(SashGeometry.GlassOpeningBounds glass, double y, bool left)
    {
        var dy = y - glass.ArcCenter.Y;
        if (Math.Abs(dy) > glass.ArcRadius)
        {
            return left ? glass.Left : glass.Right;
        }

        var dx = Math.Sqrt(Math.Max(0, glass.ArcRadius * glass.ArcRadius - dy * dy));
        return left ? glass.ArcCenter.X - dx : glass.ArcCenter.X + dx;
    }

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint worldPoint)
    {
        var screen = editor.Viewport.WorldToScreen(worldPoint);
        return new Point(screen.X, screen.Y);
    }
}
