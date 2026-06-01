using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Parametric;

namespace NurSapat.Cad.Core.Document;

public sealed class CadDocument
{
    public string Name { get; set; } = "Новый чертеж";

    public WindowStructure Structure { get; } = new();

    public WindowSection? SelectedSection => Structure.SelectedSection;
    private readonly List<CadRect> _rectangles = new();
    public IReadOnlyList<CadRect> Rectangles => _rectangles;

    public void AddRectangle(CadRect rect)
    {
        _rectangles.Add(rect);
    }

    public void RemoveRectangle(CadRect rect)
    {
        _rectangles.Remove(rect);
    }

    public void ClearRectangles()
    {
        _rectangles.Clear();
    }
}
