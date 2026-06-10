using NurSapat.Cad.Tools.Tools;

namespace NurSapat.Cad.Tools;

public sealed class CadToolFactory
{
    public IReadOnlyList<CadToolDescriptor> Tools { get; } =
    [
        new("select", "Выбор", () => new SelectionTool()),
        new("rectangle", "Прямоугольник", () => new RectangleTool())
    ];

    public ICadTool Create(string id)
    {
        var descriptor = Tools.FirstOrDefault(t => t.Id == id)
            ?? throw new InvalidOperationException($"CAD tool '{id}' is not registered.");
        return descriptor.Create();
    }
}

public sealed record CadToolDescriptor(string Id, string DisplayName, Func<ICadTool> Create);