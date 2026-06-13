using NurSapat.Cad.Tools.Tools;

namespace NurSapat.Cad.Tools;

public sealed class CadToolFactory
{
    public IReadOnlyList<CadToolDescriptor> Tools { get; } =
    [
        new("select", "Выбор", () => new SelectionTool()),
        new("rectangle", "Прямоугольник", () => new RectangleTool()),
        new("mullion-place-v", "Импост вертикальный", () => new MullionPlacementTool("mullion-place-v", "Импост вертикальный", MullionPlacementMode.Vertical)),
        new("mullion-place-h", "Импост горизонтальный", () => new MullionPlacementTool("mullion-place-h", "Импост горизонтальный", MullionPlacementMode.Horizontal)),
        new("mullion-place-d", "Импост наклонный", () => new MullionPlacementTool("mullion-place-d", "Импост наклонный", MullionPlacementMode.Diagonal)),
        new("design-rect", "Область", () => new DesignRectPlacementTool())
    ];

    public ICadTool Create(string id)
    {
        var descriptor = Tools.FirstOrDefault(t => t.Id == id)
            ?? throw new InvalidOperationException($"CAD tool '{id}' is not registered.");
        return descriptor.Create();
    }
}

public sealed record CadToolDescriptor(string Id, string DisplayName, Func<ICadTool> Create);