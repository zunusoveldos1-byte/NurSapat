using NurSapat.Cad.Core.Document;

namespace NurSapat.Cad.Core.Commands;

/// <summary>
/// CAD-команда редактора, привязанная к <see cref="CadDocument"/>.
/// </summary>
/// <remarks>
/// <b>Мост с NurSapat.Cad.Foundation</b>: наследует обобщённый контракт
/// <c>NurSapat.Cad.Foundation.Commands.ICadCommand&lt;CadDocument&gt;</c>.
/// Сигнатуры методов (<c>Execute(CadDocument)</c> / <c>Undo(CadDocument)</c>) полностью совпадают,
/// поэтому существующие реализации (<c>CadActionCommand</c> и др.) продолжают работать без изменений.
/// </remarks>
public interface ICadCommand : NurSapat.Cad.Foundation.Commands.ICadCommand<CadDocument>
{
}
