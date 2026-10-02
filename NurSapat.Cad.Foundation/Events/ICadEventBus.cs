namespace NurSapat.Cad.Foundation.Events;

/// <summary>
/// Шина событий CAD-платформы — универсальный канал сообщений между модулями.
/// </summary>
/// <remarks>
/// Применение: ViewModel и сервисы публикуют события при изменении состояния
/// (объект создан, выбор изменился, вид сместился и т.д.), а другие модули
/// подписываются на нужные типы событий. Это исключает прямые зависимости
/// между компонентами.
/// </remarks>
public interface ICadEventBus
{
    /// <summary>Опубликовать событие всем подписчикам.</summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : CadEventBase;

    /// <summary>Подписаться на события типа <typeparamref name="TEvent"/>.</summary>
    void Subscribe<TEvent>(ICadEventHandler<TEvent> handler) where TEvent : CadEventBase;

    /// <summary>Подписаться на события типа через делегат-обработчик.</summary>
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : CadEventBase;

    /// <summary>Отписать делегат-обработчик.</summary>
    void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : CadEventBase;

    /// <summary>Отписать обработчик.</summary>
    void Unsubscribe<TEvent>(ICadEventHandler<TEvent> handler) where TEvent : CadEventBase;

    /// <summary>Синхронная публикация (выполняет все обработчики и дожидается их завершения).</summary>
    void Publish<TEvent>(TEvent @event) where TEvent : CadEventBase;
}
