using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using System.Collections.Generic;

namespace NurSapat.Cad.Core.Snapping;

public sealed class CadDocumentSnapProvider : ISnapProvider
{
    public IEnumerable<CadSnapTick> GetSnapTicks(CadDocument document)
    {
        // Точки привязки только для прямоугольников (CadRect)
        foreach (var rect in document.Rectangles)
        {
            foreach (var tick in GetRectTicks(rect, "Rectangle"))
            {
                yield return tick;
            }
        }
    }

    private static IEnumerable<CadSnapTick> GetRectTicks(CadRect rect, string source)
    {
        // X-координаты: левый и правый край
        yield return new CadSnapTick(rect.X, CadSnapTickAxis.X, source);
        yield return new CadSnapTick(rect.X + rect.Width, CadSnapTickAxis.X, source);

        // Y-координаты: верхний и нижний край
        yield return new CadSnapTick(rect.Y, CadSnapTickAxis.Y, source);
        yield return new CadSnapTick(rect.Y - rect.Height, CadSnapTickAxis.Y, source);
    }
}