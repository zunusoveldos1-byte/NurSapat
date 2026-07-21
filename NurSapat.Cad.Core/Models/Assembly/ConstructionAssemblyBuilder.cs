using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Пайплайн построения ConstructionAssembly из глобального дерева и переходных профилей.
/// </summary>
public static class ConstructionAssemblyBuilder
{
    public const double DefaultTransitionCoverStripMm = 4.0;

    public static ConstructionAssembly Build(
        FrameNode globalRoot,
        IReadOnlyList<TransitionProfileModel> transitions,
        double coverStripMm = DefaultTransitionCoverStripMm)
    {
        var global = GlobalFrame.FromBounds(
            globalRoot.X,
            globalRoot.Y,
            globalRoot.Width,
            globalRoot.Height);

        var orderedTransitions = transitions
            .Where(t => t.Orientation == GridLineOrientation.Vertical)
            .OrderBy(t => t.AxisCoordinate)
            .ToList();

        var subFrames = SplitByTransitions(global, orderedTransitions, globalRoot, coverStripMm);
        WireTransitionSubFrameLinks(orderedTransitions, subFrames);

        return new ConstructionAssembly
        {
            GlobalFrame = global,
            TransitionProfiles = orderedTransitions.ToList(),
            SubFrames = subFrames.ToList()
        };
    }

    public static IReadOnlyList<SubFrame> SplitByTransitions(
        GlobalFrame global,
        IReadOnlyList<TransitionProfileModel> orderedVerticalTransitions,
        FrameNode globalRoot,
        double coverStripMm)
    {
        var strips = ComputeVerticalStrips(global, orderedVerticalTransitions);
        var subFrames = new List<SubFrame>(strips.Count);

        for (var i = 0; i < strips.Count; i++)
        {
            var (originX, outerWidth) = strips[i];
            var leftTransition = i > 0 ? orderedVerticalTransitions[i - 1] : null;
            var rightTransition = i < orderedVerticalTransitions.Count ? orderedVerticalTransitions[i] : null;

            var transitionCover = 0.0;
            if (leftTransition is not null)
            {
                transitionCover += coverStripMm;
            }

            var localRoot = ExtractLocalRoot(
                globalRoot,
                originX,
                global.Y,
                outerWidth,
                global.Height,
                transitionCover);
            var subFrameId = Guid.NewGuid();
            localRoot.AssignOwnerSubFrame(subFrameId);
            subFrames.Add(new SubFrame
            {
                Id = subFrameId,
                OriginWorldX = originX,
                OriginWorldY = global.Y,
                OuterWidth = outerWidth,
                OuterHeight = global.Height,
                TransitionCoverStripMm = transitionCover,
                OrderIndex = i,
                Root = localRoot
            });

            _ = rightTransition;
        }

        return subFrames;
    }

    public static IReadOnlyList<(double OriginX, double OuterWidth)> ComputeVerticalStrips(
        GlobalFrame global,
        IReadOnlyList<TransitionProfileModel> orderedVerticalTransitions)
    {
        var strips = new List<(double OriginX, double OuterWidth)>();
        var cursor = global.X;

        foreach (var transition in orderedVerticalTransitions)
        {
            // SubFrame outer bounds meet at the transition axis.  The physical
            // transition profile is a barrier layered on that junction; its
            // cover strip is accounted for only by InternalWidth.
            var outerWidth = CadMath.RoundMm(transition.AxisCoordinate - cursor);
            if (outerWidth > CadMath.Epsilon)
            {
                strips.Add((cursor, outerWidth));
            }

            cursor = transition.AxisCoordinate;
        }

        var tailWidth = CadMath.RoundMm(global.Right - cursor);
        if (tailWidth > CadMath.Epsilon || strips.Count == 0)
        {
            strips.Add((cursor, tailWidth));
        }

        return strips;
    }

    public static ConstructionAssembly SplitExistingAssemblyAtVerticalTransition(
        ConstructionAssembly assembly,
        SubFrame owner,
        TransitionProfileModel transition,
        double coverStripMm = DefaultTransitionCoverStripMm)
    {
        if (transition.Orientation != GridLineOrientation.Vertical)
        {
            throw new NotSupportedException("Only vertical transition separators are supported.");
        }

        var localAxis = owner.WorldToLocalX(transition.AxisCoordinate);
        if (localAxis <= CadMath.MmPrecision
            || localAxis >= owner.OuterWidth - CadMath.MmPrecision)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transition),
                "Transition axis must be strictly inside its owner SubFrame.");
        }

        var leftId = Guid.NewGuid();
        var leftRoot = PartitionVerticalKeepingNodeIds(
            owner.Root,
            clipLeft: 0,
            clipRight: localAxis,
            remintClippedLeafIds: false);
        leftRoot.AssignOwnerSubFrame(leftId);
        var left = new SubFrame
        {
            Id = leftId,
            OriginWorldX = owner.OriginWorldX,
            OriginWorldY = owner.OriginWorldY,
            OuterWidth = localAxis,
            OuterHeight = owner.OuterHeight,
            TransitionCoverStripMm = owner.TransitionCoverStripMm,
            OuterCoverStripMm = owner.OuterCoverStripMm,
            Root = leftRoot
        };
        var rightWidth = CadMath.RoundMm(owner.OuterWidth - localAxis);
        var rightId = Guid.NewGuid();
        var rightRoot = PartitionVerticalKeepingNodeIds(
            owner.Root,
            clipLeft: localAxis,
            clipRight: owner.OuterWidth,
            remintClippedLeafIds: true);
        rightRoot.AssignOwnerSubFrame(rightId);
        var right = new SubFrame
        {
            Id = rightId,
            OriginWorldX = transition.AxisCoordinate,
            OriginWorldY = owner.OriginWorldY,
            OuterWidth = rightWidth,
            OuterHeight = owner.OuterHeight,
            TransitionCoverStripMm = coverStripMm,
            OuterCoverStripMm = owner.OuterCoverStripMm,
            Root = rightRoot
        };

        var subFrames = assembly.SubFrames
            .Where(candidate => candidate.Id != owner.Id)
            .Append(left)
            .Append(right)
            .OrderBy(candidate => candidate.OriginWorldX)
            .ToList();
        for (var index = 0; index < subFrames.Count; index++)
        {
            subFrames[index].OrderIndex = index;
        }

        var transitions = assembly.TransitionProfiles
            .Append(transition)
            .OrderBy(candidate => candidate.AxisCoordinate)
            .ToList();
        WireTransitionSubFrameLinks(transitions, subFrames);
        return new ConstructionAssembly
        {
            GlobalFrame = assembly.GlobalFrame,
            TransitionProfiles = transitions,
            SubFrames = subFrames
        };
    }

    private static void WireTransitionSubFrameLinks(
        IList<TransitionProfileModel> transitions,
        IReadOnlyList<SubFrame> subFrames)
    {
        for (var i = 0; i < transitions.Count; i++)
        {
            transitions[i].LeftSubFrameId = subFrames[i].Id;
            transitions[i].RightSubFrameId = subFrames[i + 1].Id;
        }
    }

    private static FrameNode ExtractLocalRoot(
        FrameNode globalRoot,
        double originX,
        double originY,
        double outerWidth,
        double outerHeight,
        double leadingCoverStripMm)
    {
        var clone = FrameNode.CreateRoot(0, 0, outerWidth, outerHeight);
        CopySplitsInBounds(
            globalRoot,
            clone,
            originX,
            originY,
            outerWidth,
            outerHeight,
            leadingCoverStripMm);
        return clone;
    }

    private static void CopySplitsInBounds(
        FrameNode source,
        FrameNode targetRoot,
        double worldOriginX,
        double worldOriginY,
        double worldWidth,
        double worldHeight,
        double leadingCoverStripMm)
    {
        var worldRight = worldOriginX + worldWidth;
        var worldTop = worldOriginY + worldHeight;

        foreach (var split in EnumerateBinarySplits(source))
        {
            if (split.SeparatorKind == SeparatorKind.Transition)
            {
                continue;
            }

            var axis = split.SplitOrientation == SplitOrientation.Vertical
                ? split.X + split.Width * (split.SplitRelativeCenter ?? 0.5)
                : split.Y + split.Height * (split.SplitRelativeCenter ?? 0.5);

            if (split.SplitOrientation == SplitOrientation.Vertical)
            {
                if (axis < worldOriginX - CadMath.MmPrecision || axis > worldRight + CadMath.MmPrecision)
                {
                    continue;
                }

                // Legacy trees stored axes in the global fictitious grid.
                // A SubFrame owns internal math after its leading cover strip,
                // therefore 900 + 702 becomes local 698 for a 4 mm strip.
                var localAxis = CadMath.RoundMm(
                    axis - worldOriginX - leadingCoverStripMm);
                if (localAxis <= CadMath.MmPrecision
                    || localAxis >= worldWidth - leadingCoverStripMm - CadMath.MmPrecision)
                {
                    continue;
                }
                var leaf = targetRoot.FindLeafAt(localAxis, worldHeight / 2.0);
                leaf?.TrySplitVerticalAt(localAxis - leaf.X, split.LineStyle, split.ImpostThicknessMm);
            }
            else
            {
                if (axis < worldOriginY - CadMath.MmPrecision || axis > worldTop + CadMath.MmPrecision)
                {
                    continue;
                }

                var localAxis = CadMath.RoundMm(axis - worldOriginY);
                var leaf = targetRoot.FindLeafAt(worldWidth / 2.0, localAxis);
                leaf?.TrySplitHorizontalAt(localAxis - leaf.Y, split.LineStyle, split.ImpostThicknessMm);
            }
        }
    }

    private static IEnumerable<FrameNode> EnumerateBinarySplits(FrameNode node)
    {
        if (!node.IsLeaf && !node.IsUniformSplit && node.ChildA is not null && node.ChildB is not null)
        {
            yield return node;
            foreach (var child in EnumerateBinarySplits(node.ChildA))
            {
                yield return child;
            }

            foreach (var child in EnumerateBinarySplits(node.ChildB))
            {
                yield return child;
            }
        }
        else if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                foreach (var nested in EnumerateBinarySplits(cell))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>
    /// Vertical partition of an existing SubFrame tree that preserves NodeIds for
    /// nodes that remain intact. Clipped leaf remnants remint ids only when
    /// <paramref name="remintClippedLeafIds"/> is true (right half), so left-side
    /// HostFrameNodeId / StableId bindings survive.
    /// </summary>
    private static FrameNode PartitionVerticalKeepingNodeIds(
        FrameNode sourceRoot,
        double clipLeft,
        double clipRight,
        bool remintClippedLeafIds)
    {
        var clipped = ClipTreeSnapshot(
            sourceRoot.CaptureTreeSnapshot(),
            clipLeft,
            clipRight,
            remintClippedLeafIds)
            ?? throw new InvalidOperationException("Vertical partition produced an empty SubFrame tree.");

        var shifted = ShiftTreeSnapshot(clipped, -clipLeft, 0);
        var root = FrameNode.RestoreTree(shifted)
            ?? throw new InvalidOperationException("Failed to restore partitioned SubFrame tree.");
        root.IsFictitiousContainer = sourceRoot.IsFictitiousContainer;
        return root;
    }

    private static FrameNode.TreeSnapshot? ClipTreeSnapshot(
        FrameNode.TreeSnapshot source,
        double clipLeft,
        double clipRight,
        bool remintClippedLeafIds)
    {
        var nodeLeft = source.X;
        var nodeRight = source.X + source.Width;
        if (nodeRight <= clipLeft + CadMath.MmPrecision
            || nodeLeft >= clipRight - CadMath.MmPrecision)
        {
            return null;
        }

        var entirelyInside = nodeLeft >= clipLeft - CadMath.MmPrecision
            && nodeRight <= clipRight + CadMath.MmPrecision;
        var newX = CadMath.RoundMm(Math.Max(nodeLeft, clipLeft));
        var newRight = CadMath.RoundMm(Math.Min(nodeRight, clipRight));
        var newWidth = CadMath.RoundMm(newRight - newX);
        if (newWidth <= CadMath.MmPrecision)
        {
            return null;
        }

        if (source.IsUniformSplit)
        {
            var cells = new List<FrameNode.TreeSnapshot>();
            foreach (var cell in source.UniformCells)
            {
                var clippedCell = ClipTreeSnapshot(cell, clipLeft, clipRight, remintClippedLeafIds);
                if (clippedCell is not null)
                {
                    cells.Add(clippedCell);
                }
            }

            if (cells.Count == 0)
            {
                return null;
            }

            if (cells.Count == 1)
            {
                return cells[0];
            }

            return new FrameNode.TreeSnapshot
            {
                X = newX,
                Y = source.Y,
                Width = newWidth,
                Height = source.Height,
                IsLeaf = false,
                IsUniformSplit = true,
                SplitOrientation = source.SplitOrientation,
                LineStyle = source.LineStyle,
                ImpostThicknessMm = source.ImpostThicknessMm,
                UniformImpostCount = Math.Max(0, cells.Count - 1),
                UniformCells = cells,
                NodeId = source.NodeId,
                IsDummy = source.IsDummy,
                SeparatorKind = source.SeparatorKind,
                TopProfile = source.TopProfile,
                Shape = source.Shape,
                Sill = source.Sill
            };
        }

        if (!source.IsLeaf && source.ChildA is not null && source.ChildB is not null)
        {
            var axis = source.SplitOrientation == SplitOrientation.Vertical
                ? source.X + source.Width * (source.SplitRelativeCenter ?? 0.5)
                : source.Y + source.Height * (source.SplitRelativeCenter ?? 0.5);

            if (source.SplitOrientation == SplitOrientation.Vertical)
            {
                if (axis <= clipLeft + CadMath.MmPrecision)
                {
                    return ClipTreeSnapshot(source.ChildB, clipLeft, clipRight, remintClippedLeafIds);
                }

                if (axis >= clipRight - CadMath.MmPrecision)
                {
                    return ClipTreeSnapshot(source.ChildA, clipLeft, clipRight, remintClippedLeafIds);
                }
            }

            var childA = ClipTreeSnapshot(source.ChildA, clipLeft, clipRight, remintClippedLeafIds);
            var childB = ClipTreeSnapshot(source.ChildB, clipLeft, clipRight, remintClippedLeafIds);
            if (childA is null)
            {
                return childB;
            }

            if (childB is null)
            {
                return childA;
            }

            double? relative = source.SplitRelativeCenter;
            if (source.SplitOrientation == SplitOrientation.Vertical && newWidth > CadMath.Epsilon)
            {
                relative = CadMath.RoundMm((axis - newX) / newWidth);
            }

            return new FrameNode.TreeSnapshot
            {
                X = newX,
                Y = source.Y,
                Width = newWidth,
                Height = source.Height,
                IsLeaf = false,
                IsUniformSplit = false,
                SplitOrientation = source.SplitOrientation,
                LineStyle = source.LineStyle,
                ImpostThicknessMm = source.ImpostThicknessMm,
                UniformImpostCount = source.UniformImpostCount,
                ChildA = childA,
                ChildB = childB,
                SplitRelativeCenter = relative,
                NodeId = source.NodeId,
                IsDummy = source.IsDummy,
                SeparatorKind = source.SeparatorKind,
                TopProfile = source.TopProfile,
                Shape = source.Shape,
                Sill = source.Sill
            };
        }

        var leafNodeId = !entirelyInside && remintClippedLeafIds
            ? Guid.NewGuid()
            : source.NodeId;
        return new FrameNode.TreeSnapshot
        {
            X = newX,
            Y = source.Y,
            Width = newWidth,
            Height = source.Height,
            IsLeaf = true,
            IsUniformSplit = false,
            SplitOrientation = SplitOrientation.None,
            LineStyle = source.LineStyle,
            ImpostThicknessMm = source.ImpostThicknessMm,
            UniformImpostCount = 0,
            NodeId = leafNodeId,
            IsDummy = source.IsDummy,
            SeparatorKind = SeparatorKind.None,
            ExtremeImposts = ClipExtremeImposts(source.ExtremeImposts, newX, newRight, source.Y, source.Y + source.Height),
            TopProfile = source.TopProfile,
            Shape = source.Shape,
            Sill = source.Sill
        };
    }

    private static IReadOnlyList<FrameNode.ExtremeImpostSlotSnapshot> ClipExtremeImposts(
        IReadOnlyList<FrameNode.ExtremeImpostSlotSnapshot> slots,
        double leafLeft,
        double leafRight,
        double leafBottom,
        double leafTop)
    {
        if (slots.Count == 0)
        {
            return slots;
        }

        var clipped = new List<FrameNode.ExtremeImpostSlotSnapshot>(slots.Count);
        foreach (var slot in slots)
        {
            if (slot.SpanEndMm <= slot.SpanStartMm + CadMath.Epsilon)
            {
                clipped.Add(slot);
                continue;
            }

            if (slot.Orientation == SplitOrientation.Vertical)
            {
                var spanStart = Math.Max(slot.SpanStartMm, leafBottom);
                var spanEnd = Math.Min(slot.SpanEndMm, leafTop);
                if (spanEnd - spanStart <= CadMath.MmPrecision)
                {
                    continue;
                }

                clipped.Add(new FrameNode.ExtremeImpostSlotSnapshot
                {
                    Id = slot.Id,
                    Orientation = slot.Orientation,
                    EdgeAlignment = slot.EdgeAlignment,
                    OffsetMm = slot.OffsetMm,
                    ThicknessMm = slot.ThicknessMm,
                    SpanStartMm = spanStart,
                    SpanEndMm = spanEnd,
                    LightOpeningInsetMm = slot.LightOpeningInsetMm
                });
                continue;
            }

            var horizontalStart = Math.Max(slot.SpanStartMm, leafLeft);
            var horizontalEnd = Math.Min(slot.SpanEndMm, leafRight);
            if (horizontalEnd - horizontalStart <= CadMath.MmPrecision)
            {
                continue;
            }

            clipped.Add(new FrameNode.ExtremeImpostSlotSnapshot
            {
                Id = slot.Id,
                Orientation = slot.Orientation,
                EdgeAlignment = slot.EdgeAlignment,
                OffsetMm = slot.OffsetMm,
                ThicknessMm = slot.ThicknessMm,
                SpanStartMm = horizontalStart,
                SpanEndMm = horizontalEnd,
                LightOpeningInsetMm = slot.LightOpeningInsetMm
            });
        }

        return clipped;
    }

    private static FrameNode.TreeSnapshot ShiftTreeSnapshot(
        FrameNode.TreeSnapshot source,
        double deltaX,
        double deltaY)
    {
        if (Math.Abs(deltaX) <= CadMath.Epsilon && Math.Abs(deltaY) <= CadMath.Epsilon)
        {
            return source;
        }

        return new FrameNode.TreeSnapshot
        {
            X = CadMath.RoundMm(source.X + deltaX),
            Y = CadMath.RoundMm(source.Y + deltaY),
            Width = source.Width,
            Height = source.Height,
            IsLeaf = source.IsLeaf,
            IsUniformSplit = source.IsUniformSplit,
            SplitOrientation = source.SplitOrientation,
            LineStyle = source.LineStyle,
            ImpostThicknessMm = source.ImpostThicknessMm,
            UniformImpostCount = source.UniformImpostCount,
            UniformCells = source.UniformCells
                .Select(cell => ShiftTreeSnapshot(cell, deltaX, deltaY))
                .ToList(),
            ChildA = source.ChildA is null ? null : ShiftTreeSnapshot(source.ChildA, deltaX, deltaY),
            ChildB = source.ChildB is null ? null : ShiftTreeSnapshot(source.ChildB, deltaX, deltaY),
            SplitRelativeCenter = source.SplitRelativeCenter,
            NodeId = source.NodeId,
            IsDummy = source.IsDummy,
            SeparatorKind = source.SeparatorKind,
            ExtremeImposts = ShiftExtremeImposts(source.ExtremeImposts, deltaX, deltaY),
            TopProfile = source.TopProfile,
            Shape = source.Shape,
            Sill = source.Sill
        };
    }

    private static IReadOnlyList<FrameNode.ExtremeImpostSlotSnapshot> ShiftExtremeImposts(
        IReadOnlyList<FrameNode.ExtremeImpostSlotSnapshot> slots,
        double deltaX,
        double deltaY)
    {
        if (slots.Count == 0
            || (Math.Abs(deltaX) <= CadMath.Epsilon && Math.Abs(deltaY) <= CadMath.Epsilon))
        {
            return slots;
        }

        return slots.Select(slot =>
        {
            if (slot.SpanEndMm <= slot.SpanStartMm + CadMath.Epsilon)
            {
                return slot;
            }

            if (slot.Orientation == SplitOrientation.Vertical)
            {
                return new FrameNode.ExtremeImpostSlotSnapshot
                {
                    Id = slot.Id,
                    Orientation = slot.Orientation,
                    EdgeAlignment = slot.EdgeAlignment,
                    OffsetMm = slot.OffsetMm,
                    ThicknessMm = slot.ThicknessMm,
                    SpanStartMm = CadMath.RoundMm(slot.SpanStartMm + deltaY),
                    SpanEndMm = CadMath.RoundMm(slot.SpanEndMm + deltaY),
                    LightOpeningInsetMm = slot.LightOpeningInsetMm
                };
            }

            return new FrameNode.ExtremeImpostSlotSnapshot
            {
                Id = slot.Id,
                Orientation = slot.Orientation,
                EdgeAlignment = slot.EdgeAlignment,
                OffsetMm = slot.OffsetMm,
                ThicknessMm = slot.ThicknessMm,
                SpanStartMm = CadMath.RoundMm(slot.SpanStartMm + deltaX),
                SpanEndMm = CadMath.RoundMm(slot.SpanEndMm + deltaX),
                LightOpeningInsetMm = slot.LightOpeningInsetMm
            };
        }).ToList();
    }
}
