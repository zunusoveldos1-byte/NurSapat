using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Layers;

/// <summary>
/// Слой CAD-сцены. Группирует объекты для управления видимостью, блокировкой и цветом.
/// </summary>
public sealed partial class Layer : ObservableObject
{
    /// <summary>Идентификатор (неизменный).</summary>
    public LayerId Id { get; }

    /// <summary>Порядковый номер отрисовки (меньше = ниже). Управляется <see cref="LayerCollection"/>.</summary>
    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _visible = true;

    [ObservableProperty]
    private bool _locked;

    [ObservableProperty]
    private LayerColor _color = LayerColor.Default;

    public Layer(LayerId id) : this(id, id.Name)
    {
    }

    public Layer(LayerId id, string name)
    {
        Id = id;
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _order = (int)id.Id.ToByteArray()[0]; // стабильный порядок по умолчанию
    }

    /// <summary>Нельзя удалить системные слои (Default/Grid/Selection/...).</summary>
    public bool IsSystem => Id == LayerId.Default || Id == LayerId.Dimensions ||
                           Id == LayerId.Guides || Id == LayerId.Selection ||
                           Id == LayerId.Grid || Id == LayerId.Preview;
}
