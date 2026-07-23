namespace NurSapat.Cad.Foundation.Events;

/// <summary>
/// Обработчик события CAD определённого типа.
/// </summary>
/// <typeparam name="TEvent">Тип события.</typeparam>
public interface ICadEventHandler<in TEvent> where TEvent : CadEventBase
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
