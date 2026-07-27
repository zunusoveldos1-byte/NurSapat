using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Foundation.Primitives;
using NurSapat.Cad.Foundation.Rendering;

namespace NurSapat.Cad.Foundation.Scene;

/// <summary>
/// Базовый класс всех CAD-объектов сцены.
/// </summary>
/// <remarks>
/// Все будущие объекты (Line, Arc, Frame, Sash и т.д.) наследуются от <see cref="SceneObject"/>
/// и реализуют абстрактные члены: <see cref="Bounds"/> и <see cref="Kind"/>.
/// Идентичность объекта — <see cref="ObjectId"/> (неизменный <see cref="Guid"/>); именно по нему
/// работают Undo/Redo и Selection.
/// </remarks>
public abstract partial class SceneObject : ObservableObject, IRenderable
{
    /// <summary>Неизменный идентификатор объекта. Никогда не меняется после создания.</summary>
    public Guid ObjectId { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>Слой, которому принадлежит объект.</summary>
    [ObservableProperty]
    private Guid _layerId;

    [ObservableProperty]
    private bool _visible = true;

    [ObservableProperty]
    private bool _locked;

    /// <summary>Находится ли объект в текущем выделении (кэш SelectionManager'а).</summary>
    [ObservableProperty]
    private bool _selected;

    /// <summary>Время создания (UTC).</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Время последней модификации (UTC).</summary>
    public DateTime ModifiedAt { get; protected set; }

    /// <summary>Аффинное преобразование объекта (перенос/поворот/масштаб).</summary>
    [ObservableProperty]
    private Transform2D _transform = Transform2D.Identity;

    /// <summary>Произвольные метаданные (для плагинов, расширений, расчётов).</summary>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Тип объекта (строковый дискриминатор для рендера и сериализации).</summary>
    public abstract string Kind { get; }

    /// <summary>Границы объекта в его локальных координатах (до применения Transform).</summary>
    public abstract Bounds2D LocalBounds { get; }

    /// <summary>Границы объекта в мировых координатах (с учётом Transform).</summary>
    public Bounds2D Bounds => TransformBounds(LocalBounds, Transform);

    protected SceneObject() : this(Guid.NewGuid())
    {
    }

    protected SceneObject(Guid objectId)
    {
        ObjectId = objectId;
        var now = DateTime.UtcNow;
        CreatedAt = now;
        ModifiedAt = now;
    }

    /// <summary>Отметить объект как изменённый (обновляет <see cref="ModifiedAt"/>).</summary>
    public void Touch() => ModifiedAt = DateTime.UtcNow;

    /// <summary>
    /// Данные отрисовки объекта. По умолчанию <c>null</c> — конкретные объекты
    /// (Line, Arc, Frame и т.д.) переопределят этот метод на Этапе 3.
    /// </summary>
    public virtual RenderData? GetRenderData() => null;

    /// <summary>Применить преобразование к AABB.</summary>
    private static Bounds2D TransformBounds(Bounds2D bounds, Transform2D transform)
    {
        if (transform.IsIdentity) return bounds;
        var a = transform.Apply(new Point2D(bounds.MinX, bounds.MinY));
        var b = transform.Apply(new Point2D(bounds.MaxX, bounds.MinY));
        var c = transform.Apply(new Point2D(bounds.MaxX, bounds.MaxY));
        var d = transform.Apply(new Point2D(bounds.MinX, bounds.MaxY));
        var min = new Point2D(Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X)),
                              Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)));
        var max = new Point2D(Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X)),
                              Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)));
        return new Bounds2D(min.X, min.Y, max.X, max.Y);
    }

    public override string ToString() => $"{Kind} «{Name}» [{ObjectId}]";
}
