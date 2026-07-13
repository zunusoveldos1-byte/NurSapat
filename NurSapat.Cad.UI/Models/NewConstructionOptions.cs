using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Models;

/// <summary>
/// Параметры создания новой конструкции. Не связаны с размерами уже существующей коробки на чертеже.
/// </summary>
public sealed class NewConstructionOptions
{
    public const double DefaultWidth = 1000;
    public const double DefaultHeight = 1000;

    public double Width { get; set; } = DefaultWidth;

    public double Height { get; set; } = DefaultHeight;

    /// <summary>Координата X выбранной точки привязки.</summary>
    public double AnchorX { get; set; }

    /// <summary>Координата Y выбранной точки привязки.</summary>
    public double AnchorY { get; set; }

    public PlacementAnchor Anchor { get; set; } = PlacementAnchor.BottomLeft;

    public NewConstructionOptions Clone() => new()
    {
        Width = Width,
        Height = Height,
        AnchorX = AnchorX,
        AnchorY = AnchorY,
        Anchor = Anchor
    };
}
