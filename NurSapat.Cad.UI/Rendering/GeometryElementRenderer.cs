using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class GeometryElementRenderer
{
    private const double FallbackSpanMm = 2000;
    private const double MarkerRadiusPx = 8;

    private static readonly Color FrameColor = Color.FromRgb(0x1F, 0x29, 0x37);
    private static readonly Color AccentColor = Color.FromRgb(0x25, 0x63, 0xEB);

    private static readonly IBrush FrameBrush = new SolidColorBrush(FrameColor);
    private static readonly IBrush AccentBrush = new SolidColorBrush(AccentColor);
    private static readonly IBrush AccentFill = new SolidColorBrush(Color.FromArgb(40, 0x25, 0x63, 0xEB));
    private static readonly IBrush GlazingFill = new SolidColorBrush(Color.FromArgb(40, 0x93, 0xC5, 0xFD));

    private static readonly IPen FramePen = new Pen(FrameBrush, 2);
    private static readonly IPen ThinPen = new Pen(FrameBrush, 1);
    private static readonly IPen AccentPen = new Pen(AccentBrush, 1.5);
    private static readonly IPen DashedPen = new Pen(FrameBrush, 1.5, new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IPen DashedAccentPen = new Pen(AccentBrush, 1, new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IPen HatchPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 0x1F, 0x29, 0x37)), 0.8);

    public static void DrawAll(DrawingContext context, CadEditorViewModel editor, IReadOnlyList<GeometryElement> elements)
    {
        foreach (var element in elements)
        {
            Draw(context, editor, element);
        }
    }

    private static void Draw(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        switch (element.Kind)
        {
            case GeometryElementKind.Mullion:
            case GeometryElementKind.EdgeMullion:
                DrawMullion(context, editor, element, dashed: false);
                break;
            case GeometryElementKind.DummyMullion:
                DrawMullion(context, editor, element, dashed: true);
                break;
            case GeometryElementKind.Frame:
            case GeometryElementKind.TransitionFrame:
                context.DrawRectangle(null, FramePen, ToScreenRect(editor, element));
                break;
            case GeometryElementKind.DummyFrame:
                context.DrawRectangle(null, DashedPen, ToScreenRect(editor, element));
                break;
            case GeometryElementKind.RoundDummyFrame:
                DrawRoundDummyFrame(context, editor, element);
                break;
            case GeometryElementKind.Sash:
                DrawSash(context, editor, element);
                break;
            case GeometryElementKind.SlidingSash:
                DrawSlidingSash(context, editor, element);
                break;
            case GeometryElementKind.Glazing:
                context.DrawRectangle(GlazingFill, ThinPen, ToScreenRect(editor, element));
                break;
            case GeometryElementKind.Corrugation:
                DrawCorrugation(context, editor, element);
                break;
            case GeometryElementKind.MosquitoNet:
                DrawMosquitoNet(context, editor, element);
                break;
            case GeometryElementKind.Handle:
            case GeometryElementKind.Hardware:
                DrawPointMarker(context, editor, element);
                break;
            case GeometryElementKind.Bead:
                DrawBead(context, editor, element);
                break;
            case GeometryElementKind.Accessory:
                DrawAccessory(context, editor, element);
                break;
            case GeometryElementKind.Installation:
                DrawInstallation(context, editor, element);
                break;
        }
    }

    private static void DrawMullion(DrawingContext context, CadEditorViewModel editor, GeometryElement element, bool dashed)
    {
        var point = new CadPoint((double)element.X, (double)element.Y);
        var (minX, maxX, minY, maxY) = ResolveFrameBounds(editor, point);
        var pen = dashed ? DashedPen : FramePen;

        var orientation = element.SourceToolId switch
        {
            "mullion-h" => 'h',
            "mullion-d" => 'd',
            _ => 'v'
        };

        if (orientation == 'd' && element.EndX.HasValue && element.EndY.HasValue)
        {
            DrawWorldLine(
                context,
                editor,
                pen,
                new CadPoint((double)element.X, (double)element.Y),
                new CadPoint((double)element.EndX.Value, (double)element.EndY.Value));
            return;
        }

        var (start, end) = orientation switch
        {
            'h' => (new CadPoint(minX, point.Y), new CadPoint(maxX, point.Y)),
            'd' => (new CadPoint(minX, minY), new CadPoint(maxX, maxY)),
            _ => (new CadPoint(point.X, minY), new CadPoint(point.X, maxY))
        };

        DrawWorldLine(context, editor, pen, start, end);
    }

    private static void DrawRoundDummyFrame(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawEllipse(null, DashedPen, rect.Center, rect.Width / 2, rect.Height / 2);
    }

    private static void DrawSash(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(null, FramePen, rect);

        var inset = Math.Min(rect.Width, rect.Height) * 0.08;
        var inner = rect.Deflate(Math.Max(2, inset));
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(AccentFill, AccentPen, inner);

        var opening = element.OpeningType ?? SashOpeningType.Fixed;
        if (opening is SashOpeningType.Turn or SashOpeningType.TiltTurn)
        {
            var hingeMid = new Point(inner.Left, inner.Center.Y);
            context.DrawLine(DashedAccentPen, hingeMid, inner.TopRight);
            context.DrawLine(DashedAccentPen, hingeMid, inner.BottomRight);
        }

        if (opening == SashOpeningType.TiltTurn)
        {
            var bottomMid = new Point(inner.Center.X, inner.Bottom);
            context.DrawLine(DashedAccentPen, bottomMid, inner.TopLeft);
            context.DrawLine(DashedAccentPen, bottomMid, inner.TopRight);
        }
    }

    private static void DrawSlidingSash(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(AccentFill, FramePen, rect);

        var toLeft = element.OpeningType != SashOpeningType.SlideRight;
        var y = rect.Center.Y;
        var margin = Math.Min(rect.Width * 0.2, 20);
        var start = new Point(toLeft ? rect.Right - margin : rect.Left + margin, y);
        var end = new Point(toLeft ? rect.Left + margin : rect.Right - margin, y);
        context.DrawLine(AccentPen, start, end);
        DrawArrowHead(context, AccentPen, start, end);
    }

    private static void DrawCorrugation(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(null, ThinPen, rect);
        DrawHatch(context, rect, spacing: 6, diagonal: false);
    }

    private static void DrawMosquitoNet(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        context.DrawRectangle(null, ThinPen, rect);
        DrawHatch(context, rect, spacing: 8, diagonal: true);
    }

    private static void DrawPointMarker(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var center = ToScreenPoint(editor, element);
        var fill = element.Kind == GeometryElementKind.Handle ? AccentBrush : AccentFill;
        context.DrawEllipse(fill, AccentPen, center, MarkerRadiusPx, MarkerRadiusPx);

        if (element.Kind == GeometryElementKind.Handle)
        {
            context.DrawLine(FramePen, center, new Point(center.X, center.Y + MarkerRadiusPx * 1.5));
        }
    }

    private static void DrawBead(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var center = ToScreenPoint(editor, element);
        var half = MarkerRadiusPx * 0.75;
        var rect = new Rect(center.X - half, center.Y - half, half * 2, half * 2);
        context.DrawRectangle(AccentFill, AccentPen, rect);
    }

    private static void DrawAccessory(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var center = ToScreenPoint(editor, element);
        var r = MarkerRadiusPx;
        var diamond = new[]
        {
            new Point(center.X, center.Y - r),
            new Point(center.X + r, center.Y),
            new Point(center.X, center.Y + r),
            new Point(center.X - r, center.Y)
        };
        for (var i = 0; i < diamond.Length; i++)
        {
            context.DrawLine(AccentPen, diamond[i], diamond[(i + 1) % diamond.Length]);
        }
    }

    private static void DrawInstallation(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var center = ToScreenPoint(editor, element);
        var r = MarkerRadiusPx;
        var top = new Point(center.X, center.Y - r);
        var left = new Point(center.X - r, center.Y + r);
        var right = new Point(center.X + r, center.Y + r);
        context.DrawLine(FramePen, top, left);
        context.DrawLine(FramePen, top, right);
        context.DrawLine(FramePen, left, right);
        context.DrawLine(ThinPen, new Point(left.X, left.Y + 3), new Point(right.X, right.Y + 3));
    }

    private static (double MinX, double MaxX, double MinY, double MaxY) ResolveFrameBounds(CadEditorViewModel editor, CadPoint point)
    {
        var rects = editor.Document.Rectangles;
        var containing = rects.Where(r => r.Contains(point)).ToList();
        var source = containing.Count > 0 ? containing : rects.ToList();

        if (source.Count == 0)
        {
            var half = FallbackSpanMm / 2;
            return (point.X - half, point.X + half, point.Y - half, point.Y + half);
        }

        var minX = source.Min(r => Math.Min(r.X, r.X + r.Width));
        var maxX = source.Max(r => Math.Max(r.X, r.X + r.Width));
        var minY = source.Min(r => Math.Min(r.Y, r.Y - r.Height));
        var maxY = source.Max(r => Math.Max(r.Y, r.Y - r.Height));
        return (minX, maxX, minY, maxY);
    }

    private static Rect ToScreenRect(CadEditorViewModel editor, GeometryElement element)
    {
        var cx = (double)element.X;
        var cy = (double)element.Y;
        var halfW = (double)element.WidthMm / 2;
        var halfH = (double)element.HeightMm / 2;
        var topLeft = editor.Viewport.WorldToScreen(new CadPoint(cx - halfW, cy + halfH));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(cx + halfW, cy - halfH));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }

    private static Point ToScreenPoint(CadEditorViewModel editor, GeometryElement element)
    {
        var screen = editor.Viewport.WorldToScreen(new CadPoint((double)element.X, (double)element.Y));
        return new Point(screen.X, screen.Y);
    }

    private static void DrawWorldLine(DrawingContext context, CadEditorViewModel editor, IPen pen, CadPoint start, CadPoint end)
    {
        var p1 = editor.Viewport.WorldToScreen(start);
        var p2 = editor.Viewport.WorldToScreen(end);
        context.DrawLine(pen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
    }

    private static void DrawHatch(DrawingContext context, Rect rect, double spacing, bool diagonal)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var clip = context.PushClip(rect);

        if (diagonal)
        {
            var span = rect.Width + rect.Height;
            for (double offset = -rect.Height; offset < span; offset += spacing)
            {
                context.DrawLine(HatchPen,
                    new Point(rect.Left + offset, rect.Top),
                    new Point(rect.Left + offset - rect.Height, rect.Bottom));
                context.DrawLine(HatchPen,
                    new Point(rect.Left + offset, rect.Top),
                    new Point(rect.Left + offset + rect.Height, rect.Bottom));
            }
        }
        else
        {
            for (var y = rect.Top + spacing; y < rect.Bottom; y += spacing)
            {
                context.DrawLine(HatchPen, new Point(rect.Left, y), new Point(rect.Right, y));
            }
        }
    }

    private static void DrawArrowHead(DrawingContext context, IPen pen, Point from, Point to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1)
        {
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        const double size = 7;
        var left = new Point(to.X - ux * size - uy * size * 0.5, to.Y - uy * size + ux * size * 0.5);
        var right = new Point(to.X - ux * size + uy * size * 0.5, to.Y - uy * size - ux * size * 0.5);
        context.DrawLine(pen, to, left);
        context.DrawLine(pen, to, right);
    }
}
