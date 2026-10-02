using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Tools;

public static class FrameBoundsHelper
{
    public static CadRect? FindFrameAt(CadDocument document, CadPoint point)
    {
        foreach (var rect in document.Rectangles)
        {
            if (rect.Contains(point))
            {
                return rect;
            }
        }

        return null;
    }

    public static CadRect ResolveBounds(CadDocument document, CadPoint nearPoint)
    {
        var frame = FindFrameAt(document, nearPoint);
        if (frame is CadRect contained)
        {
            return contained;
        }

        var rects = document.Rectangles;
        if (rects.Count == 0)
        {
            return new CadRect(nearPoint.X - 1000, nearPoint.Y + 1000, 2000, 2000);
        }

        var minX = rects.Min(r => Math.Min(r.X, r.X + r.Width));
        var maxX = rects.Max(r => Math.Max(r.X, r.X + r.Width));
        var minY = rects.Min(r => Math.Min(r.Y, r.Y - r.Height));
        var maxY = rects.Max(r => Math.Max(r.Y, r.Y - r.Height));
        return new CadRect(minX, maxY, maxX - minX, maxY - minY);
    }

    public static CadRect ClampToFrame(CadRect rect, CadRect frame)
    {
        var left = Math.Max(Math.Min(rect.X, rect.X + rect.Width), frame.X);
        var right = Math.Min(Math.Max(rect.X, rect.X + rect.Width), frame.X + frame.Width);
        var top = Math.Min(Math.Max(rect.Y, rect.Y - rect.Height), frame.Y);
        var bottom = Math.Max(Math.Min(rect.Y, rect.Y - rect.Height), frame.Y - frame.Height);
        return CadRect.FromPoints(new CadPoint(left, top), new CadPoint(right, bottom));
    }
}
