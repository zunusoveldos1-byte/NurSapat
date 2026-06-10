using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Geometry;
using System;

namespace NurSapat.Cad.Tools;

public class RectangleTool : CadToolBase
{
    private CadPoint? _startPoint;
    private CadRect? _previewRect;
    private bool _wasDragged;

    public CadRect? PreviewRect => _previewRect;

    public override string Id => "rectangle";
    public override string DisplayName => "Прямоугольник";

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        _startPoint = e.WorldPoint;
        _previewRect = null;
        _wasDragged = false;
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint == null) return;

        var start = _startPoint.Value;
        var end = e.WorldPoint;

        if ((end - start).Length > 5)
        {
            _wasDragged = true;
            var x = Math.Min(start.X, end.X);
            var topY = Math.Max(start.Y, end.Y);
            var width = Math.Abs(end.X - start.X);
            var height = Math.Abs(start.Y - end.Y);

            _previewRect = new CadRect(x, topY, width, height);
            context.Invalidate?.Invoke();
        }
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint == null) return;

        // Если не перетаскивали — сообщаем, что нужен ввод размеров
        if (!_wasDragged)
        {
            context.InvokeSizeInput?.Invoke(_startPoint.Value);
        }
        else if (_previewRect != null && _previewRect.Value.Width > 1 && _previewRect.Value.Height > 1)
        {
            CreateRectangle(context, _previewRect.Value);
        }

        _startPoint = null;
        _previewRect = null;
        _wasDragged = false;
        context.Invalidate?.Invoke();
    }

    private void CreateRectangle(CadToolContext context, CadRect rect)
    {
        var command = new CadActionCommand(
            name: "Добавить прямоугольник",
            execute: doc => doc.AddRectangle(rect),
            undo: doc => doc.RemoveRectangle(rect)
        );
        context.CommandManager.Execute(command);
    }
}