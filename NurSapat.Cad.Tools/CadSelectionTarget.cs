using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Tools;

/// <summary>Результат hit-test при выборе объекта, профиля или вершины.</summary>
public sealed record CadSelectionTarget(
    CadSelectionMode Mode,
    string Label,
    CadPoint WorldPoint,
    Guid? NodeId = null,
    string? ProfileRole = null);
