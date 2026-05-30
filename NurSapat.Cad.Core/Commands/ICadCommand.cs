using NurSapat.Cad.Core.Document;

namespace NurSapat.Cad.Core.Commands;

public interface ICadCommand
{
    string Name { get; }

    void Execute(CadDocument document);

    void Undo(CadDocument document);
}
