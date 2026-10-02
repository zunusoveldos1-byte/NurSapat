using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Изолированная геометрия линий направления открывания створки.
/// Не связана с треугольником/обрезкой импоста в <see cref="FrameNodeRenderer"/>.
/// </summary>
internal readonly record struct SashOpeningSegment(CadPoint From, CadPoint To);

/// <summary>
/// Линии открывания разделены на две независимые коллекции:
/// <list type="bullet">
/// <item><see cref="BuildSashSegments"/> — направление открывания самой створки (боковой треугольник);</item>
/// <item><see cref="BuildHardwareSegments"/> — линия откидного/фрамужного механизма (всегда вверх).</item>
/// </list>
/// </summary>
internal static class SashOpeningGeometry
{
    /// <summary>
    /// Боковой пунктирный треугольник створки (без линии фурнитуры).
    /// </summary>
    public static IReadOnlyList<SashOpeningSegment> BuildSashSegments(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashOpeningType opening,
        SashHingeSide hingeSide)
    {
        // Линии ведём к внутренним углам профиля створки (запилы 45°), не к светлому проёму стекла.
        var lineBounds = SashGeometry.ResolveSashProfileInnerFaceBounds(editor, inner);
        var segments = new List<SashOpeningSegment>(4);
        switch (opening)
        {
            case SashOpeningType.LeftTurn:
            case SashOpeningType.RightTurn:
            case SashOpeningType.TiltTurn:
            case SashOpeningType.Suspended:
            case SashOpeningType.Turn when IsSideHung(hingeSide):
                if (IsSideHung(hingeSide))
                {
                    AppendSideTurn(segments, editor, lineBounds, hingeSide);
                }

                break;
            case SashOpeningType.Turn:
                AppendTilt(segments, editor, lineBounds, hingeSide);
                break;
            case SashOpeningType.Folding:
                AppendSideTurn(segments, editor, lineBounds, SashHingeSide.Left);
                AppendSideTurn(segments, editor, lineBounds, SashHingeSide.Right);
                break;
        }

        return segments;
    }

    /// <summary>
    /// Линия фурнитуры для откидного/фрамужного механизма.
    /// Всегда строго вверх — независимо от HingeSide (петли могут оставаться слева/справа).
    /// Отдельная коллекция: не мутирует и не переиспользует сегменты створки.
    /// Геометрия строится всегда; решение «рисовать ли» принимает вызывающий код.
    /// </summary>
    public static IReadOnlyList<SashOpeningSegment> BuildHardwareSegments(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashOpeningType opening)
    {
        _ = opening; // тип открывания влияет только на ShouldDraw*; геометрия всегда вверх
        return BuildUpwardHardwareSegments(editor, inner);
    }

    /// <summary>
    /// Строго вверх: основание в нижних углах, вершина в верхнем центре проёма.
    /// Без зависимостей от OpeningType / HingeSide / модели фурнитуры.
    /// </summary>
    public static IReadOnlyList<SashOpeningSegment> BuildUpwardHardwareSegments(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner)
    {
        var lineBounds = SashGeometry.ResolveSashProfileInnerFaceBounds(editor, inner);
        var bottomLeft = new CadPoint(lineBounds.Left, lineBounds.Bottom);
        var bottomRight = new CadPoint(lineBounds.Right, lineBounds.Bottom);
        var midTopX = (lineBounds.Left + lineBounds.Right) / 2.0;
        var midTopY = SashGeometry.TryResolveInnerArcTopY(editor, lineBounds, midTopX, out var arcMidTop)
            ? arcMidTop
            : lineBounds.Top;
        var midTop = new CadPoint(midTopX, midTopY);

        return
        [
            new SashOpeningSegment(bottomLeft, midTop),
            new SashOpeningSegment(bottomRight, midTop)
        ];
    }

    /// <summary>
    /// Полный набор сегментов (створка + фурнитура) для совместимости превью.
    /// </summary>
    public static IReadOnlyList<SashOpeningSegment> BuildSegments(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashOpeningType opening,
        SashHingeSide hingeSide)
    {
        var sash = BuildSashSegments(editor, inner, opening, hingeSide);
        if (!ShouldDrawHardware(opening))
        {
            return sash;
        }

        var hardware = BuildUpwardHardwareSegments(editor, inner);
        if (sash.Count == 0)
        {
            return hardware;
        }

        var combined = new List<SashOpeningSegment>(sash.Count + hardware.Count);
        combined.AddRange(sash);
        combined.AddRange(hardware);
        return combined;
    }

    public static bool ShouldDraw(SashOpeningType opening) =>
        ShouldDrawSash(opening) || ShouldDrawHardware(opening);

    public static bool ShouldDrawSash(SashOpeningType opening) =>
        opening is SashOpeningType.Turn or SashOpeningType.LeftTurn or SashOpeningType.RightTurn
            or SashOpeningType.TiltTurn or SashOpeningType.Suspended or SashOpeningType.Folding;

    public static bool ShouldDrawHardware(SashOpeningType opening) =>
        opening is SashOpeningType.Tilt or SashOpeningType.TiltTurn or SashOpeningType.Suspended;

    private static bool IsSideHung(SashHingeSide hingeSide) =>
        hingeSide is SashHingeSide.Left or SashHingeSide.Right;

    private static void AppendSideTurn(
        List<SashOpeningSegment> segments,
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashHingeSide hingeSide)
    {
        var topLeft = SashGeometry.ResolveInnerCorner(editor, inner, inner.Left, inner.Top);
        var bottomLeft = new CadPoint(inner.Left, inner.Bottom);
        var topRight = SashGeometry.ResolveInnerCorner(editor, inner, inner.Right, inner.Top);
        var bottomRight = new CadPoint(inner.Right, inner.Bottom);
        var midLeft = new CadPoint(inner.Left, (inner.Bottom + inner.Top) / 2.0);
        var midRight = new CadPoint(inner.Right, (inner.Bottom + inner.Top) / 2.0);

        switch (hingeSide)
        {
            case SashHingeSide.Left:
                segments.Add(new SashOpeningSegment(topLeft, midRight));
                segments.Add(new SashOpeningSegment(bottomLeft, midRight));
                break;
            case SashHingeSide.Right:
                segments.Add(new SashOpeningSegment(topRight, midLeft));
                segments.Add(new SashOpeningSegment(bottomRight, midLeft));
                break;
        }
    }

    private static void AppendTilt(
        List<SashOpeningSegment> segments,
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashHingeSide hingeSide)
    {
        var topLeft = SashGeometry.ResolveInnerCorner(editor, inner, inner.Left, inner.Top);
        var bottomLeft = new CadPoint(inner.Left, inner.Bottom);
        var topRight = SashGeometry.ResolveInnerCorner(editor, inner, inner.Right, inner.Top);
        var bottomRight = new CadPoint(inner.Right, inner.Bottom);
        var midTop = new CadPoint(
            (inner.Left + inner.Right) / 2.0,
            SashGeometry.TryResolveInnerArcTopY(editor, inner, (inner.Left + inner.Right) / 2.0, out var arcMidTop)
                ? arcMidTop
                : inner.Top);
        var midBottom = new CadPoint((inner.Left + inner.Right) / 2.0, inner.Bottom);

        switch (hingeSide)
        {
            case SashHingeSide.Bottom:
                segments.Add(new SashOpeningSegment(bottomLeft, midTop));
                segments.Add(new SashOpeningSegment(bottomRight, midTop));
                break;
            case SashHingeSide.Top:
                segments.Add(new SashOpeningSegment(topLeft, midBottom));
                segments.Add(new SashOpeningSegment(topRight, midBottom));
                break;
        }
    }
}
