namespace NurSapat.Core.Models;

public sealed class SystemLogEntry
{
    public Guid Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string User { get; set; } = string.Empty;

    public string OperationType { get; set; } = string.Empty;

    public string ActionDetails { get; set; } = string.Empty;
}
