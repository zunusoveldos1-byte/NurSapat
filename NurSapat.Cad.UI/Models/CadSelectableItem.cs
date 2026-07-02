using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Models;

public enum CadSelectableKind
{
    Frame,
    Impost,
    Sash,
    Glazing,
    Bead,
    SashHandle,
    SashHinge
}

/// <summary>Ключ выделяемого элемента конструкции на холсте.</summary>
public sealed class CadSelectableItem : IEquatable<CadSelectableItem>
{
    public CadSelectableKind Kind { get; init; }
    public Guid? ElementId { get; init; }
    public FrameNode? ImpostNode { get; init; }
    public int ImpostGapIndex { get; init; }
    public int HingeSlot { get; init; }

    public static CadSelectableItem Frame() => new() { Kind = CadSelectableKind.Frame };

    public static CadSelectableItem Sash(Guid id) =>
        new() { Kind = CadSelectableKind.Sash, ElementId = id };

    public static CadSelectableItem Glazing(Guid id) =>
        new() { Kind = CadSelectableKind.Glazing, ElementId = id };

    public static CadSelectableItem Bead(Guid id) =>
        new() { Kind = CadSelectableKind.Bead, ElementId = id };

    public static CadSelectableItem SashHandle(Guid sashId) =>
        new() { Kind = CadSelectableKind.SashHandle, ElementId = sashId };

    public static CadSelectableItem SashHinge(Guid sashId, int hingeSlot) =>
        new() { Kind = CadSelectableKind.SashHinge, ElementId = sashId, HingeSlot = hingeSlot };

    public static CadSelectableItem Impost(FrameNode node, int uniformGapIndex = 0) =>
        new()
        {
            Kind = CadSelectableKind.Impost,
            ImpostNode = node,
            ImpostGapIndex = uniformGapIndex
        };

    public static CadSelectableItem FromImpostTarget(VerticalImpostTarget target) =>
        Impost(target.SplitNode, target.UniformGapIndex ?? 0);

    public bool Equals(CadSelectableItem? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Kind != other.Kind)
        {
            return false;
        }

        return Kind switch
        {
            CadSelectableKind.Frame => true,
            CadSelectableKind.Impost =>
                ReferenceEquals(ImpostNode, other.ImpostNode) && ImpostGapIndex == other.ImpostGapIndex,
            CadSelectableKind.SashHinge =>
                ElementId == other.ElementId && HingeSlot == other.HingeSlot,
            _ => ElementId == other.ElementId && Kind == other.Kind
        };
    }

    public override bool Equals(object? obj) => Equals(obj as CadSelectableItem);

    public override int GetHashCode() =>
        Kind switch
        {
            CadSelectableKind.Frame => HashCode.Combine(Kind),
            CadSelectableKind.Impost => HashCode.Combine(Kind, ImpostNode, ImpostGapIndex),
            CadSelectableKind.SashHinge => HashCode.Combine(Kind, ElementId, HingeSlot),
            _ => HashCode.Combine(Kind, ElementId)
        };
}
