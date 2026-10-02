namespace NurSapat.Cad.Foundation.Commands;

/// <summary>
/// Обобщённый контракт CAD-команды с поддержкой Undo.
/// </summary>
/// <typeparam name="TContext">Тип контекста, через который команда меняет состояние.</typeparam>
/// <remarks>
/// Контракт спроектирован так, чтобы существующий <c>NurSapat.Cad.Core.ICadCommand</c>
/// мог унаследовать <see cref="ICadCommand{TContext}"/> с <c>TContext = CadDocument</c>
/// без изменения своих реализаций (<c>CadActionCommand</c> и др.).
/// Идентичность изменяемых объектов внутри команды — всегда по <c>Guid</c> (ObjectId).
/// </remarks>
public interface ICadCommand<in TContext>
{
    /// <summary>Человекочитаемое имя команды (для UI истории и логирования).</summary>
    string Name { get; }

    /// <summary>Применить изменения к контексту.</summary>
    void Execute(TContext context);

    /// <summary>Откатить изменения, выполненные <see cref="Execute"/>.</summary>
    void Undo(TContext context);
}
