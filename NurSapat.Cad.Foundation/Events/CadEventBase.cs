namespace NurSapat.Cad.Foundation.Events;

/// <summary>
/// Базовый класс всех событий CAD-платформы.
/// </summary>
/// <remarks>
/// События публикуются через <see cref="ICadEventBus"/> и позволяют модулям и сервисам
/// реагировать на изменения состояния без прямых зависимостей друг от друга.
/// </remarks>
public abstract class CadEventBase
{
    /// <summary>Время возникновения события (UTC).</summary>
    public DateTime TimestampUtc { get; } = DateTime.UtcNow;

    /// <summary>Идентификатор workspace-источника (для мульти-документности). Может быть пустым.</summary>
    public Guid WorkspaceId { get; init; }
}
