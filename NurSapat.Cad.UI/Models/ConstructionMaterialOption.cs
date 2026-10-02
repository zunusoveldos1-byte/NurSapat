using NurSapat.Core.Models.Construction;

namespace NurSapat.Cad.UI.Models;

/// <summary>
/// Пункт выпадающего списка материалов (включая «Нет материала»).
/// </summary>
public sealed class ConstructionMaterialOption
{
    public const string NoneDisplayName = "(Нет материала)";

    public ConstructionMaterial? Material { get; init; }

    public string DisplayName { get; init; } = NoneDisplayName;

    public Guid? MaterialId => Material?.Id;

    public static ConstructionMaterialOption None { get; } = new();

    public static ConstructionMaterialOption FromMaterial(ConstructionMaterial material) =>
        new()
        {
            Material = material,
            DisplayName = material.Name
        };
}
