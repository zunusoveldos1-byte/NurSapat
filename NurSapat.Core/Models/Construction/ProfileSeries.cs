namespace NurSapat.Core.Models.Construction;

/// <summary>
/// Серия профилей — задаёт системную глубину (толщину) конструкции.
/// </summary>
public sealed class ProfileSeries
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = "";

    /// <summary>Системная глубина профиля, мм.</summary>
    public double SystemDepth { get; init; }
}
