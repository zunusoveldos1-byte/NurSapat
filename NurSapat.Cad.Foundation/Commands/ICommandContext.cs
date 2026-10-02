namespace NurSapat.Cad.Foundation.Commands;

/// <summary>
/// Маркерный интерфейс контекста выполнения команды.
/// </summary>
/// <remarks>
/// Контекст — это объект, через который команда изменяет состояние документа
/// (например, <c>IScene</c>, <c>CadDocument</c> или композитный <c>Workspace</c>).
/// Нужен, чтобы команда была отвязана от конкретного хранилища и могла выполняться
/// в undo/redo на разных снимках состояния.
/// </remarks>
public interface ICommandContext
{
}
