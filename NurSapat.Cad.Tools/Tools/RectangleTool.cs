using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Document;

namespace NurSapat.Cad.Tools;

public class RectangleTool : CadToolBase
{
    private CadPoint? _startPoint;
    private CadRect? _previewRect;
    public CadRect? PreviewRect => _previewRect;
    public override string Id => "rectangle";
    public override string DisplayName => "Прямоугольник";

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        // ИСПРАВЛЕНИЕ: используем WorldPoint вместо Location
        _startPoint = e.WorldPoint;
        _previewRect = null;
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint == null) return;

        var start = _startPoint.Value;
        var end = e.WorldPoint; // ИСПРАВЛЕНИЕ

        // Вычисляем корректные координаты прямоугольника (minX, maxY, ширина, высота)
        var x = Math.Min(start.X, end.X);
        var y = Math.Max(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(start.Y - end.Y);

        _previewRect = new CadRect(x, y, width, height);
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        // ИСПРАВЛЕНИЕ: проверяем _previewRect.HasValue и обращаемся к Value.Width/Height
        if (!_previewRect.HasValue ||
            _previewRect.Value.Width < 1 ||
            _previewRect.Value.Height < 1)
            return;

        context.Document.AddRectangle(_previewRect.Value);

        _startPoint = null;
        _previewRect = null;
    }
}