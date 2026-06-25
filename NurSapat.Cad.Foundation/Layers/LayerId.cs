namespace NurSapat.Cad.Foundation.Layers;

/// <summary>
/// Идентификатор слоя: неизменный <see cref="Guid"/> + отображаемое имя.
/// </summary>
/// <remarks>
/// Идентичность слоя — по <see cref="Id"/> (неизменный Guid). Имя слоя изменяемо
/// и используется только для отображения.
/// </remarks>
public readonly record struct LayerId(Guid Id, string Name)
{
    /// <summary>Создать новый слой со случайным Guid.</summary>
    public static LayerId New(string name) => new(Guid.NewGuid(), name);

    public static LayerId Default { get; } = new(new Guid("00000000-0000-0000-0000-000000000001"), "Default");
    public static LayerId Dimensions { get; } = new(new Guid("00000000-0000-0000-0000-000000000002"), "Dimensions");
    public static LayerId Guides { get; } = new(new Guid("00000000-0000-0000-0000-000000000003"), "Guides");
    public static LayerId Selection { get; } = new(new Guid("00000000-0000-0000-0000-000000000004"), "Selection");
    public static LayerId Grid { get; } = new(new Guid("00000000-0000-0000-0000-000000000005"), "Grid");
    public static LayerId Preview { get; } = new(new Guid("00000000-0000-0000-0000-000000000006"), "Preview");

    public override string ToString() => $"{Name} ({Id})";
}
