using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Models;

public sealed record NewConstructionSizeInputResult(
    double Width,
    double Height,
    double AnchorX,
    double AnchorY,
    PlacementAnchor Anchor);
