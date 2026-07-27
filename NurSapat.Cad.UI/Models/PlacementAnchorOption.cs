using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Models;

public sealed class PlacementAnchorOption
{
    public required PlacementAnchor Anchor { get; init; }

    public required string Label { get; init; }
}
