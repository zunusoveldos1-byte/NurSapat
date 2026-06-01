using NurSapat.Cad.Core.Parametric;
using NurSapat.Cad.Tools.Tools;

namespace NurSapat.Cad.Tools;

public sealed class CadToolFactory
{
    public IReadOnlyList<CadToolDescriptor> Tools { get; } =
    [
        new("select", "Выбор", () => new SelectionTool()),
        new("rectangle", "Прямоугольник", () => new RectangleTool()),
        new("frame", "Коробка", () => new FrameTool("frame", "Коробка")),
        new("phantom-frame", "Коробка фиктивная", () => new FrameTool("phantom-frame", "Коробка фиктивная", true)),
        new("vertical-impost", "Импост вертикальный", () => new ImpostTool("vertical-impost", "Импост вертикальный", SectionSplitOrientation.Vertical)),
        new("horizontal-impost", "Импост горизонтальный", () => new ImpostTool("horizontal-impost", "Импост горизонтальный", SectionSplitOrientation.Horizontal)),
        new("angled-impost", "Импост наклонный", () => new ImpostTool("angled-impost", "Импост наклонный", SectionSplitOrientation.Angled)),
        new("sash", "Створка", () => new FillTool("sash", "Створка", SectionFillKind.Sash)),
        new("glazing", "Остекление", () => new FillTool("glazing", "Остекление", SectionFillKind.Glazing)),
        new("bead", "Штапик", () => new FillTool("bead", "Штапик", SectionFillKind.Glazing)),
        new("corrugation", "Гофра", () => new FillTool("corrugation", "Гофра", SectionFillKind.Corrugation)),
        new("hardware", "Фурнитурный комплект", () => new HardwareModifierTool("hardware", "Фурнитурный комплект", SashHardwareModifier.HardwareKit)),
        new("handle", "Ручка", () => new HardwareModifierTool("handle", "Ручка", SashHardwareModifier.Handle)),
        new("lock", "Замок", () => new HardwareModifierTool("lock", "Замок", SashHardwareModifier.Lock)),
        new("bolt", "Шпингалет", () => new HardwareModifierTool("bolt", "Шпингалет", SashHardwareModifier.Bolt)),
        new("limiter", "Ограничитель", () => new HardwareModifierTool("limiter", "Ограничитель", SashHardwareModifier.OpeningLimiter)),
        new("closer", "Доводчик", () => new HardwareModifierTool("closer", "Доводчик", SashHardwareModifier.Closer))
    ];

    public ICadTool Create(string id)
    {
        var descriptor = Tools.FirstOrDefault(t => t.Id == id)
            ?? throw new InvalidOperationException($"CAD tool '{id}' is not registered.");
        return descriptor.Create();
    }
}

public sealed record CadToolDescriptor(string Id, string DisplayName, Func<ICadTool> Create);
