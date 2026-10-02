using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Helpers;

public static class PlacementAnchorGeometry
{
    public static (double OriginX, double OriginY) ResolveBottomLeftOrigin(
        PlacementAnchor anchor,
        double anchorX,
        double anchorY,
        double width,
        double height)
    {
        width = CadMath.RoundMm(width);
        height = CadMath.RoundMm(height);

        return anchor switch
        {
            PlacementAnchor.BottomRight => (CadMath.RoundMm(anchorX - width), anchorY),
            PlacementAnchor.TopLeft => (anchorX, CadMath.RoundMm(anchorY - height)),
            PlacementAnchor.TopRight => (CadMath.RoundMm(anchorX - width), CadMath.RoundMm(anchorY - height)),
            _ => (anchorX, anchorY)
        };
    }

    public static (double AnchorX, double AnchorY) ResolveAnchorPoint(
        PlacementAnchor anchor,
        double originX,
        double originY,
        double width,
        double height)
    {
        width = CadMath.RoundMm(width);
        height = CadMath.RoundMm(height);

        return anchor switch
        {
            PlacementAnchor.BottomRight => (CadMath.RoundMm(originX + width), originY),
            PlacementAnchor.TopLeft => (originX, CadMath.RoundMm(originY + height)),
            PlacementAnchor.TopRight => (CadMath.RoundMm(originX + width), CadMath.RoundMm(originY + height)),
            _ => (originX, originY)
        };
    }
}
