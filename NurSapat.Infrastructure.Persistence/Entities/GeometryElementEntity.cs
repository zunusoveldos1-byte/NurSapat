namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class GeometryElementEntity
{
    public Guid Id { get; set; }

    public Guid OrderConstructionId { get; set; }

    public int Kind { get; set; }

    public string Label { get; set; } = string.Empty;

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public int? OpeningType { get; set; }

    public int? HingeSide { get; set; }

    public bool HasHardware { get; set; }

    public bool HasHandle { get; set; }

    public bool HasMosquitoNet { get; set; }

    public Guid? HostSashId { get; set; }

    public Guid? ParentGlazingId { get; set; }

    public int? SpokeType { get; set; }

    public double? SpokeThicknessMm { get; set; }

    public string? SpokeColor { get; set; }

    public int? SpokeRowsCount { get; set; }

    public int? SpokeColumnsCount { get; set; }

    public int? SpokeRayCount { get; set; }

    public bool SpokeIsArched { get; set; }

    public decimal? SpokeArcCenterX { get; set; }

    public decimal? SpokeArcCenterY { get; set; }

    public double? SpokeArcRadius { get; set; }

    public Guid? ProfileSeriesId { get; set; }

    public Guid? MaterialId { get; set; }

    public string? SourceToolId { get; set; }

    public decimal? EndX { get; set; }

    public decimal? EndY { get; set; }

    public bool IsExtremeImpost { get; set; }

    public int? EdgeAlignment { get; set; }

    public double? ExtremeImpostOffsetMm { get; set; }

    public Guid? HostFrameNodeId { get; set; }

    public Guid? HostGridLineId { get; set; }

    public Guid? SubFrameId { get; set; }

    public Guid? ExtremeImpostSlotId { get; set; }

    public int? FillType { get; set; }

    public int? Shape { get; set; }

    public OrderConstructionEntity OrderConstruction { get; set; } = null!;
}
