using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

/// <summary>Снимок состояния конструкции на холсте для отмены/повтора размещения.</summary>
public sealed class ConstructionMemento
{
    public FrameNode.TreeSnapshot? FrameRootSnapshot { get; init; }

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
        HostSashId = source.HostSashId,
        ProfileSeriesId = source.ProfileSeriesId,
        MaterialId = source.MaterialId,
        SourceToolId = source.SourceToolId,
        EndX = source.EndX,
        EndY = source.EndY,
        ParentGlazingId = source.ParentGlazingId,
        SpokeType = source.SpokeType,
        SpokeThicknessMm = source.SpokeThicknessMm,
        SpokeColor = source.SpokeColor,
        SpokeRowsCount = source.SpokeRowsCount,
        SpokeColumnsCount = source.SpokeColumnsCount,
        SpokeRayCount = source.SpokeRayCount,
        SpokeIsArched = source.SpokeIsArched,
        SpokeArcCenterX = source.SpokeArcCenterX,
        SpokeArcCenterY = source.SpokeArcCenterY,
        SpokeArcRadius = source.SpokeArcRadius
    };
}
