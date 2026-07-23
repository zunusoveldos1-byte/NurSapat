using System.Linq;
using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class MosquitoNetRenderer
{
    private static readonly IPen FramePen = new Pen(new SolidColorBrush(Color.Parse("#5A6A7A")), 1.0);
    private static readonly IPen HatchPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 0x40, 0x60, 0x80)), 0.6);

    public static void DrawMosquitoNetOverlay(
        DrawingContext context,
        CadEditorViewModel editor,
        GeometryElement element,
        IReadOnlyList<GeometryElement> allElements)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        if (element.Kind == GeometryElementKind.MosquitoNet)
        {
            DrawNetElement(context, editor, element);
            return;
        }

        if (element.Kind == GeometryElementKind.Sash && element.HasMosquitoNet)
        {
            var net = allElements.FirstOrDefault(
                e => e.Kind == GeometryElementKind.MosquitoNet && e.HostSashId == element.Id);
            if (net is not null)
            {
                DrawNetElement(context, editor, net);
            }
        }
    }

    private static void DrawNetElement(DrawingContext context, CadEditorViewModel editor, GeometryElement element)
    {
        var rect = ToScreenRect(editor, element);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        const double inset = 4;
        var inner = new Rect(
            rect.X + inset,
            rect.Y + inset,
            Math.Max(0, rect.Width - inset * 2),
            Math.Max(0, rect.Height - inset * 2));

        using (context.PushClip(inner))
        {
            DrawMeshHatch(context, inner, spacing: 7);
        }

        context.DrawRectangle(FramePen, inner);
    }

    private static void DrawMeshHatch(DrawingContext context, Rect rect, double spacing)
    {
        for (var offset = -rect.Height; offset < rect.Width + rect.Height; offset += spacing)
        {
            context.DrawLine(HatchPen,
                new Point(rect.Left + offset, rect.Top),
                new Point(rect.Left + offset + rect.Height, rect.Bottom));
            context.DrawLine(HatchPen,
                new Point(rect.Left + offset, rect.Bottom),
                new Point(rect.Left + offset + rect.Height, rect.Top));
        }
    }

    private static Rect ToScreenRect(CadEditorViewModel editor, GeometryElement element)
    {
        var cx = (double)element.X;
        var cy = (double)element.Y;
        var halfW = (double)element.WidthMm / 2.0;
        var halfH = (double)element.HeightMm / 2.0;
        var topLeft = editor.Viewport!.WorldToScreen(new NurSapat.Cad.Core.Geometry.CadPoint(cx - halfW, cy + halfH));
        var bottomRight = editor.Viewport.WorldToScreen(new NurSapat.Cad.Core.Geometry.CadPoint(cx + halfW, cy - halfH));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }
}
