using System.Collections.Concurrent;

namespace NurSapat.Cad.Foundation.Events;

/// <summary>
/// In-memory реализация шины событий CAD.
/// </summary>
/// <remarks>
/// Потокобезопасная. Хранит подписчиков в словаре «тип события → список обработчиков».
/// Обработчики вызываются последовательно в порядке подписки. Исключение одного
/// обработчика не прерывает остальные (логируется в трассировку).
/// </remarks>
public sealed class CadEventBus : ICadEventBus
{
    private abstract class Subscription
    {
        public abstract Task InvokeAsync(CadEventBase @event, CancellationToken ct);
    }

    private sealed class HandlerSubscription<TEvent> : Subscription
        where TEvent : CadEventBase
    {
        private readonly Func<TEvent, CancellationToken, Task> _callback;
        public HandlerSubscription(Func<TEvent, CancellationToken, Task> callback) => _callback = callback;
        public override Task InvokeAsync(CadEventBase @event, CancellationToken ct) => _callback((TEvent)@event, ct);
    }

    private readonly ConcurrentDictionary<Type, List<Subscription>> _subscriptions = new();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : CadEventBase
    {
        ArgumentNullException.ThrowIfNull(@event);
        return PublishCoreAsync(@event, cancellationToken);
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : CadEventBase
        => PublishAsync(@event).GetAwaiter().GetResult();

    public void Subscribe<TEvent>(ICadEventHandler<TEvent> handler) where TEvent : CadEventBase
    {
        ArgumentNullException.ThrowIfNull(handler);
        Subscribe<TEvent>((e, ct) => handler.HandleAsync(e, ct));
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : CadEventBase
    {
        ArgumentNullException.ThrowIfNull(handler);
        var list = _subscriptions.GetOrAdd(typeof(TEvent), _ => new List<Subscription>());
        lock (list)
        {
            list.Add(new HandlerSubscription<TEvent>(handler));
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : CadEventBase
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var list)) return;
        lock (list)
        {
            // Удаляем все Subscription, чей callback совпадает по ссылке.
            list.RemoveAll(s => s is HandlerSubscription<TEvent> hs && hs.Equals(handler));
        }
    }

    public void Unsubscribe<TEvent>(ICadEventHandler<TEvent> handler) where TEvent : CadEventBase
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var list)) return;
        lock (list)
        {
            list.RemoveAll(s => s is HandlerSubscription<TEvent> hs);
        }
    }

    private async Task PublishCoreAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : CadEventBase
    {
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var list) || list.Count == 0)
            return;

        // Снимок, чтобы итерация не страдала от изменений коллекции во время выполнения.
        Subscription[] snapshot;
        lock (list)
        {
            snapshot = list.ToArray();
        }

        foreach (var subscription in snapshot)
        {
            try
            {
                await subscription.InvokeAsync(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[CadEventBus] Ошибка обработчика события {typeof(TEvent).Name}: {ex}");
            }
        }
    }
}
