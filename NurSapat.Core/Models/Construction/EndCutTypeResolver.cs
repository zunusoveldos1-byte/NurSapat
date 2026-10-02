using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Models.Construction;

/// <summary>
/// Определяет тип обработки торца по умолчанию для инструмента / элемента.
/// </summary>
public static class EndCutTypeResolver
{
    public static EndCutType ResolveDefault(GeometryElementKind kind) => kind switch
    {
        GeometryElementKind.RoundDummyFrame => EndCutType.ButtJoint0,
        GeometryElementKind.TriangleDummyFrame => EndCutType.Mitre45,
        GeometryElementKind.Corrugation or GeometryElementKind.MosquitoNet or GeometryElementKind.Sill
            => EndCutType.ButtJoint0,
        GeometryElementKind.Mullion or GeometryElementKind.DummyMullion or GeometryElementKind.EdgeMullion
            => EndCutType.ImpostMilling,
        GeometryElementKind.Frame or GeometryElementKind.Sash or GeometryElementKind.ArchFrame
            or GeometryElementKind.TransitionFrame or GeometryElementKind.Bead
            => EndCutType.Mitre45,
        _ => EndCutType.Mitre45
    };

    public static EndCutType Resolve(GeometryElement element, ConstructionMaterial? material) =>
        material?.EndCutType ?? ResolveDefault(element.Kind);
}
