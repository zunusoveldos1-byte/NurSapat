using Avalonia.Media;
using NurSapat.Cad.UI.Rendering;

namespace NurSapat.Cad.UI.Models;

/// <summary>Пункт выпадающего списка выбора глобального цвета контура чертежа.</summary>
public sealed class ContourColorOption
{
    public required CadContourColorOption Option { get; init; }

    public required string Label { get; init; }

    public required IBrush ColorBrush { get; init; }

    public override string ToString() => Label;
}
