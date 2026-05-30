using NurSapat.Cad.Core.Parametric;

namespace NurSapat.Cad.Core.Document;

public sealed class CadDocument
{
    public string Name { get; set; } = "Новый чертеж";

    public WindowStructure Structure { get; } = new();

    public WindowSection? SelectedSection => Structure.SelectedSection;
}
