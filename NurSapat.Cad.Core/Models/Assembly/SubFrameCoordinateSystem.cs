using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Локальная система координат подрамы и преобразования world ↔ local.
/// </summary>
public static class SubFrameCoordinateSystem
{
    public static (double LocalX, double LocalY) WorldToLocal(SubFrame subFrame, double worldX, double worldY) =>
        (subFrame.WorldToLocalX(worldX), subFrame.WorldToLocalY(worldY));

    public static (double WorldX, double WorldY) LocalToWorld(SubFrame subFrame, double localX, double localY) =>
        (subFrame.LocalToWorldX(localX), subFrame.LocalToWorldY(localY));

    public static FrameNode? FindLeafAtWorld(SubFrame subFrame, double worldX, double worldY)
    {
        var localX = subFrame.WorldToLocalX(worldX);
        var localY = subFrame.WorldToLocalY(worldY);
        return subFrame.Root.FindLeafAt(localX, localY);
    }

    public static GridLineCollection BuildGrid(SubFrame subFrame) =>
        FrameNodeGridLineProjector.Project(subFrame.Root);

    /// <summary>
    /// Горизонтальный импост обрезается на внутренней грани смежного переходного профиля.
    /// </summary>
    public static (double Left, double Right) ClipHorizontalSpan(
        SubFrame subFrame,
        ConstructionAssembly assembly,
        double spanLeftWorld,
        double spanRightWorld,
        double axisY)
    {
        var left = spanLeftWorld;
        var right = spanRightWorld;

        var leftTransition = assembly.FindAdjacentTransition(subFrame, leftSide: true);
        if (leftTransition is not null)
        {
            left = Math.Max(left, leftTransition.InnerRightFace);
        }

        var rightTransition = assembly.FindAdjacentTransition(subFrame, leftSide: false);
        if (rightTransition is not null)
        {
            right = Math.Min(right, rightTransition.InnerLeftFace);
        }

        _ = axisY;
        return (CadMath.RoundMm(left), CadMath.RoundMm(right));
    }
}
