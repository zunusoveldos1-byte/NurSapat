using NurSapat.Cad.Core.Document;

namespace NurSapat.Cad.Core.Commands;

public sealed class CadActionCommand : ICadCommand
{
    private readonly Action<CadDocument> _execute;
    private readonly Action<CadDocument> _undo;

    public string Name { get; }

    public CadActionCommand(string name, Action<CadDocument> execute, Action<CadDocument> undo)
    {
        Name = name;
        _execute = execute;
        _undo = undo;
    }

    public void Execute(CadDocument document) => _execute(document);

    public void Undo(CadDocument document) => _undo(document);
}
