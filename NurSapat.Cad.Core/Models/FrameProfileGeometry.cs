using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models;

/// <summary>
/// Расчёт толщины профиля рамы — синхронизирован с FrameVolumeRenderer.
/// </summary>
public static class FrameProfileGeometry
{
    public static double ResolveProfileThickness(double width, double height, double profileThickness)
    {
        if (profileThickness <= CadMath.Epsilon)
        {
            return 0;
        }

        var maxT = Math.Min(width, height) * 0.45;
        if (maxT <= 2)
        {
            return maxT;
        }

        return Math.Clamp(profileThickness, 2, maxT);
    }

    public static double ResolveInnerInset(double width, double height, double profileThickness) =>
        ResolveProfileThickness(width, height, profileThickness) / 2.0;
}
