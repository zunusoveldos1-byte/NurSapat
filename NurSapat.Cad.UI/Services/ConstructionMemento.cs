using NurSapat.Cad.Core.Geometry;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

namespace NurSapat.Cad.UI.Services;

/// <summary>Снимок состояния конструкции на холсте для отмены/повтора размещения.</summary>
public sealed class ConstructionMemento
{
    public ConstructionAssemblyData? AssemblyData { get; init; }

    public IReadOnlyList<GeometryElement> Elements { get; init; } = [];

    public CadRect? DocumentRect { get; init; }

    public static GeometryElement CloneElement(GeometryElement source) => new()
    {
        Id = source.Id,
        Kind = source.Kind,
        Label = source.Label,
        X = source.X,
        Y = source.Y,
        WidthMm = source.WidthMm,
        HeightMm = source.HeightMm,
        OpeningType = source.OpeningType,
        HingeSide = source.HingeSide,
        HasHardware = source.HasHardware,
        HasHandle = source.HasHandle,
        HasMosquitoNet = source.HasMosquitoNet,
        HasBead = source.HasBead,
        HostSashId = source.HostSashId,
        ParentGlazingId = source.ParentGlazingId,
        ParentBeadId = source.ParentBeadId,
        LinkedGlazingId = source.LinkedGlazingId,
        GlassMaterialId = source.GlassMaterialId,
        BeadOffsetMm = source.BeadOffsetMm,
        CreatedAt = source.CreatedAt,
        SpokeType = source.SpokeType,
        SpokeThicknessMm = source.SpokeThicknessMm,
        SpokeColor = source.SpokeColor,
        SpokeRowsCount = source.SpokeRowsCount,
        SpokeColumnsCount = source.SpokeColumnsCount,
        SpokeRayCount = source.SpokeRayCount,
        SpokeIsArched = source.SpokeIsArched,
        SpokeArcCenterX = source.SpokeArcCenterX,
        SpokeArcCenterY = source.SpokeArcCenterY,
        SpokeArcRadius = source.SpokeArcRadius,
        ProfileSeriesId = source.ProfileSeriesId,
        MaterialId = source.MaterialId,
        SourceToolId = source.SourceToolId,
        EndX = source.EndX,
        EndY = source.EndY,
        IsExtremeImpost = source.IsExtremeImpost,
        EdgeAlignment = source.EdgeAlignment,
        ExtremeImpostOffsetMm = source.ExtremeImpostOffsetMm,
        HostFrameNodeId = source.HostFrameNodeId,
        HostGridLineId = source.HostGridLineId,
        SubFrameId = source.SubFrameId,
        ExtremeImpostSlotId = source.ExtremeImpostSlotId,
        FillType = source.FillType,
        Shape = source.Shape
    };
}
