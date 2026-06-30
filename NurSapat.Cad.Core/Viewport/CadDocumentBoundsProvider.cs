using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using System;
using System.Linq;

namespace NurSapat.Cad.Core.Viewport;

public sealed class CadDocumentBoundsProvider
{
    public CadRect GetWorldBounds(CadDocument document)
    {
        var accumulator = new BoundsAccumulator();

        // Добавляем все прямоугольники из документа
        foreach (var rect in document.Rectangles)
        {
            accumulator.Add(rect);
        }

        // Если прямоугольников нет, возвращаем дефолтную область
        if (!accumulator.HasValue)
        {
            return new CadRect(-200, 200, 400, 400);
        }

        return accumulator.ToCadRect();
    }

    private sealed class BoundsAccumulator
    {
        private bool _hasValue;
        private double _left;
        private double _right;
        private double _top;
        private double _bottom;

        public bool HasValue => _hasValue;

        public void Add(CadRect rect)
        {
            Add(rect.TopLeft);
            Add(rect.BottomRight);
        }

        public void Add(CadPoint point)
        {
            if (!_hasValue)
            {
                _left = _right = point.X;
                _top = _bottom = point.Y;
                _hasValue = true;
                return;
            }

            _left = Math.Min(_left, point.X);
            _right = Math.Max(_right, point.X);
            _top = Math.Max(_top, point.Y);
            _bottom = Math.Min(_bottom, point.Y);
        }

        public CadRect ToCadRect()
        {
            if (!_hasValue)
            {
                return new CadRect(0, 0, 1, 1);
            }

            // Добавляем отступы, чтобы объекты не прилипали к краям
            var padding = 100;
            return new CadRect(
                _left - padding,
                _top + padding,
                Math.Max(1, _right - _left + padding * 2),
                Math.Max(1, _top - _bottom + padding * 2));
        }
    }
}