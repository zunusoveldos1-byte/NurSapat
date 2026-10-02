namespace NurSapat.App.Models.View;

public sealed class AppMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public AppMessageSeverity Severity { get; init; }

    public string Text { get; init; } = string.Empty;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public string TimeDisplay => Timestamp.LocalDateTime.ToString("HH:mm:ss");
}
