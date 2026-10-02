namespace NurSapat.Cad.Foundation.Events;

/// <summary>Событие: активный инструмент CAD изменился.</summary>
public sealed class ToolChangedEvent : CadEventBase
{
    public string? PreviousToolId { get; }
    public string? CurrentToolId { get; }
    public string? DisplayName { get; }

    public ToolChangedEvent(string? previousToolId, string? currentToolId, string? displayName = null)
    {
        PreviousToolId = previousToolId;
        CurrentToolId = currentToolId;
        DisplayName = displayName;
    }
}
