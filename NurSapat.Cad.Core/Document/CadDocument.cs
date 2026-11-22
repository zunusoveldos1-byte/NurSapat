using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Document;

public sealed class CadDocument
{
    public string Name { get; set; } = "Новый чертеж";

    public bool HasUserStructure { get; private set; }
    private readonly List<CadRect> _rectangles = new();
    public IReadOnlyList<CadRect> Rectangles => _rectangles;

    private CadRect? _selectedRectangle;
    public CadRect? SelectedRectangle
    {
        get => _selectedRectangle;
        set
        {
            _selectedRectangle = value;
            // Здесь можно вызвать событие изменения, если нужно
        }
    }

    public bool ReplaceRectangle(CadRect oldRect, CadRect newRect)
    {
        int index = _rectangles.IndexOf(oldRect);
        if (index == -1) return false;
        _rectangles[index] = newRect;
        return true;
    }

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

    public void Reset()
    {
        _rectangles.Clear();
        HasUserStructure = false;
    }

    public void MarkStructureModified()
    {
        HasUserStructure = true;
    }
}
