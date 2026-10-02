namespace NurSapat.Cad.Foundation.Events;

/// <summary>Событие: состав текущего выделения изменился.</summary>
public sealed class SelectionChangedEvent : CadEventBase
{
    public IReadOnlyList<Guid> SelectedObjectIds { get; }
    public IReadOnlyList<Guid> DeselectedObjectIds { get; }

    public SelectionChangedEvent(IReadOnlyList<Guid> selectedObjectIds, IReadOnlyList<Guid> deselectedObjectIds)
    {
        SelectedObjectIds = selectedObjectIds ?? Array.Empty<Guid>();
        DeselectedObjectIds = deselectedObjectIds ?? Array.Empty<Guid>();
    }

    public static SelectionChangedEvent Empty { get; } =
        new(Array.Empty<Guid>(), Array.Empty<Guid>());
}
