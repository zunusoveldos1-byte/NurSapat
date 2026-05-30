using NurSapat.Cad.Core.Document;

namespace NurSapat.Cad.Core.Commands;

public sealed class CadCommandManager
{
    private readonly Stack<ICadCommand> _undo = new();
    private readonly Stack<ICadCommand> _redo = new();
    private readonly CadDocument _document;

    public event EventHandler<ICadCommand>? CommandExecuted;

    public CadCommandManager(CadDocument document)
    {
        _document = document;
    }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Execute(ICadCommand command)
    {
        command.Execute(_document);
        _undo.Push(command);
        _redo.Clear();
        CommandExecuted?.Invoke(this, command);
    }

    public void Undo()
    {
        if (_undo.TryPop(out var command))
        {
            command.Undo(_document);
            _redo.Push(command);
        }
    }

    public void Redo()
    {
        if (_redo.TryPop(out var command))
        {
            command.Execute(_document);
            _undo.Push(command);
            CommandExecuted?.Invoke(this, command);
        }
    }
}
