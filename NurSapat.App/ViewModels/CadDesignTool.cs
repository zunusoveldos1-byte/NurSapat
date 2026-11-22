using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels;

public sealed partial class CadDesignTool : ObservableObject
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Category { get; init; }

    public required string IconGlyph { get; init; }

    public string? CadToolId { get; init; }

    public GeometryElementKind? ElementKind { get; init; }

    public bool PlacesOnCanvas { get; init; }

    [ObservableProperty]
    private bool _isActive;
}

public sealed class CadDesignToolGroup
{
    public required string Title { get; init; }

    public required IReadOnlyList<CadDesignTool> Tools { get; init; }
}
