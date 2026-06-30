using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;

namespace NurSapat.Cad.UI.Models;

public sealed class CadToolbarItem
{
    public required string SpeedButton { get; init; }
    public required string Hint { get; init; }
    public required WindowToolMode Mode { get; init; }
    public required string IconGlyph { get; init; }
    public int BlockIndex { get; init; }
    public string? CadToolId { get; init; }
    public CadToolKind? ToolKind { get; init; }
    public bool IsInstantAction { get; init; }
}
