using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Parametric;
using NurSapat.Cad.Core.Snapping;

namespace NurSapat.Cad.Tools;

public sealed class CadToolContext
{
    public CadDocument Document { get; }

    public CadCommandManager Commands { get; }

    public CadSnappingEngine Snapping { get; }

    public WindowSection? HoveredSection { get; private set; }

    public Action<string, string, string>? Track { get; init; }

    public CadToolContext(CadDocument document, CadCommandManager commands, CadSnappingEngine snapping)
    {
        Document = document;
        Commands = commands;
        Snapping = snapping;
    }

    public WindowSection? HitTest(CadPoint point)
    {
        if (Document.Structure.HitTestImpost(point, Math.Max(5, Snapping.ToleranceWorld), out var impostOwner) && impostOwner != null)
        {
            Document.Structure.SelectImpost(impostOwner.Id);
            HoveredSection = null;
            return null;
        }

        HoveredSection = Document.Structure.HitTestLeaf(point);
        if (HoveredSection != null)
        {
            Document.Structure.SelectSection(HoveredSection.Id);
        }
        else
        {
            Document.Structure.SelectFrame();
        }

        return HoveredSection;
    }

    public void InitializeFrame(double width, double height, bool hiddenProfile)
    {
        Document.Structure.InitializeFrame(width, height, hiddenProfile);
        Track?.Invoke(hiddenProfile ? "Коробка фиктивная" : "Коробка", $"Инициализация конструкции {width:0}x{height:0} мм", "Успешно");
    }

    public void SplitSelected(SectionSplitOrientation orientation, CadPoint point)
    {
        Document.Structure.SplitSelected(orientation, point);
        Track?.Invoke(
            orientation == SectionSplitOrientation.Vertical ? "Импост вертикальный" :
            orientation == SectionSplitOrientation.Horizontal ? "Импост горизонтальный" : "Импост наклонный",
            "Деление выбранного проема",
            "Успешно");
    }

    public void ApplyFill(SectionFillKind kind)
    {
        Document.Structure.ApplyFillToSelected(kind);
        Track?.Invoke(kind.ToString(), "Заполнение выбранного проема", "Успешно");
    }

    public void AddHardware(SashHardwareModifier modifier)
    {
        Document.Structure.AddHardwareToSelected(modifier);
        Track?.Invoke(modifier.ToString(), "Добавление фурнитурного модификатора створки", "Успешно");
    }
}
