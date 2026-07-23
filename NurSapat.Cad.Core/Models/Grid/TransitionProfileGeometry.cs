using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models.Grid;

/// <summary>
/// Единый расчёт bounds переходного профиля поверх любой GridLine.
/// </summary>
public static class TransitionProfileGeometry
{
    public static TransitionProfileBounds ResolveTransitionProfileBounds(
        GridLine line,
        double thicknessMm,
        FrameNode root,
        double? spanMinOverride = null,
        double? spanMaxOverride = null)
    {
        var thickness = Math.Max(0, thicknessMm);
        var (face1, face2, center) = AxisMath.ResolveFaces(line.Coordinate, thickness);
        var (spanMin, spanMax) = AxisMath.ContainerSpan(
            line.Orientation,
            root.X,
            root.Y,
            root.Width,
            root.Height);

        if (spanMinOverride is double sMin)
        {
            spanMin = sMin;
        }

        if (spanMaxOverride is double sMax)
        {
            spanMax = sMax;
        }

        return new TransitionProfileBounds(
            line.Orientation,
            CadMath.RoundMm(center),
            CadMath.RoundMm(face1),
            CadMath.RoundMm(face2),
            CadMath.RoundMm(thickness),
            CadMath.RoundMm(spanMin),
            CadMath.RoundMm(spanMax));
    }

    /// <summary>
    /// Мировой origin AABB элемента TransitionFrame (нижний левый угол + размеры).
    /// </summary>
    public static (double OriginX, double OriginY, double Width, double Height) ToWorldOriginBounds(
        in TransitionProfileBounds bounds)
    {
        if (bounds.Orientation == GridLineOrientation.Vertical)
        {
            return (
                bounds.OriginAlongAxis,
                bounds.OriginAlongSpan,
                bounds.SizeAlongAxis,
                bounds.SizeAlongSpan);
        }

        return (
            bounds.OriginAlongSpan,
            bounds.OriginAlongAxis,
            bounds.SizeAlongSpan,
            bounds.SizeAlongAxis);
    }
}
