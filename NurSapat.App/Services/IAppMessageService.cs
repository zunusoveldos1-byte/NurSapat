using NurSapat.App.Models.View;

namespace NurSapat.App.Services;

public interface IAppMessageService
{
    event EventHandler? MessagesChanged;

    IReadOnlyList<AppMessage> Messages { get; }

    void LogInfo(string text);

    void LogWarning(string text);

    void LogError(string text);

    void Clear();
}

public sealed class AppMessageService : IAppMessageService
{
    private readonly List<AppMessage> _messages = [];

    public event EventHandler? MessagesChanged;

    public IReadOnlyList<AppMessage> Messages => _messages;

    public AppMessageService()
    {
        SeedSampleMessages();
    }

    public void LogInfo(string text) => Add(AppMessageSeverity.Info, text);

    public void LogWarning(string text) => Add(AppMessageSeverity.Warning, text);

    public void LogError(string text) => Add(AppMessageSeverity.Error, text);

    public void Clear()
    {
        if (_messages.Count == 0)
        {
            return;
        }

        _messages.Clear();
        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Add(AppMessageSeverity severity, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _messages.Insert(0, new AppMessage
        {
            Severity = severity,
            Text = text.Trim()
        });

        MessagesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SeedSampleMessages()
    {
        LogInfo("Редактор готов к работе.");
        LogWarning("Арочная рама: проверьте радиус закругления верхней части.");
        LogError("Валидация геометрии: импост выходит за границы створки.");
    }
}
