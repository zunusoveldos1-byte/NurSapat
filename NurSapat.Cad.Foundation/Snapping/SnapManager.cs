using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Snapping;

/// <summary>
/// Реализация <see cref="ISnapManager"/>: агрегирует провайдеров и выбирает лучший snap-point.
/// </summary>
/// <remarks>
/// Скелетная реализация: собирает кандидатов от зарегистрированных провайдеров
/// и выбирает ближайший с учётом допуска. Полные алгоритмы snapping — Этап 4.
/// </remarks>
public sealed class SnapManager : ISnapManager
{
    private readonly List<ISnapProvider> _providers = new();

    public SnapSettings Settings { get; } = new();

    public void RegisterProvider(ISnapProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _providers.Add(provider);
    }

    public void UnregisterProvider(ISnapProvider provider) => _providers.Remove(provider);

    public SnapPoint Snap(Point2D inputPoint, SnapKind kinds)
    {
        var best = SnapPoint.None(inputPoint);
        var region = Bounds2D.FromCenter(inputPoint, Settings.Tolerance * 2, Settings.Tolerance * 2);

        foreach (var provider in _providers)
        {
            foreach (var candidate in provider.GetCandidates(kinds & Settings.EnabledKinds, region))
            {
                if (candidate.Distance < best.Distance && candidate.Distance <= Settings.Tolerance)
                    best = candidate;
            }
        }

        return best;
    }
}
