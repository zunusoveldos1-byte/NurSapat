using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Assembly;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.Core.Tests.Assembly;

public sealed class ConstructionAssemblyTests
{
    [Fact]
    public void ComputeOverallWidth_SumsSubFramesAndTransitions()
    {
        var assembly = new ConstructionAssembly
        {
            GlobalFrame = GlobalFrame.FromBounds(0, 0, 2300, 2100),
            TransitionProfiles =
            [
                new TransitionProfileModel
                {
                    AxisCoordinate = 923,
                    ThicknessMm = 46,
                    Orientation = GridLineOrientation.Vertical
                }
            ],
            SubFrames =
            [
                new SubFrame
                {
                    OuterWidth = 900,
                    OuterHeight = 2100,
                    OriginWorldX = 0,
                    OriginWorldY = 0,
                    Root = FrameNode.CreateRoot(0, 0, 900, 2100)
                },
                new SubFrame
                {
                    OuterWidth = 1400,
                    OuterHeight = 2100,
                    OriginWorldX = 946,
                    OriginWorldY = 0,
                    TransitionCoverStripMm = 4,
                    Root = FrameNode.CreateRoot(0, 0, 1400, 2100)
                }
            ]
        };

        Assert.True(assembly.ValidateOverallWidth(2346));
        Assert.Equal(2346, assembly.ComputeOverallWidth());
    }

    [Fact]
    public void SubFrameDimensionBuilder_SplitsInternal1396Into698Plus698()
    {
        var subFrame = new SubFrame
        {
            OuterWidth = 1400,
            OuterHeight = 2100,
            TransitionCoverStripMm = 4,
            OriginWorldX = 946,
            OriginWorldY = 0,
            Root = FrameNode.CreateRoot(0, 0, 1400, 2100)
        };

        Assert.Equal(1396, subFrame.InternalWidth);

        var segments = SubFrameDimensionBuilder.SplitInternalWidthEvenly(subFrame.InternalWidth, 2);
        Assert.Equal(2, segments.Count);
        Assert.Equal(698, segments[0]);
        Assert.Equal(698, segments[1]);
        Assert.True(SubFrameDimensionBuilder.SegmentsSumToInternal(segments, subFrame.InternalWidth));

        var chain = SubFrameDimensionBuilder.BuildHorizontalChain(subFrame, [698]);
        Assert.Equal(1400, chain.OuterWidthMm);
        Assert.Equal(1396, chain.InternalWidthMm);
        Assert.Equal([698, 698], chain.SegmentSizesMm);
    }

    [Fact]
    public void TransitionProfile_InnerFacesClampHorizontalSpan()
    {
        var transition = new TransitionProfileModel
        {
            AxisCoordinate = 923,
            ThicknessMm = 46,
            Orientation = GridLineOrientation.Vertical
        };

        var leftSubFrame = new SubFrame
        {
            Id = Guid.NewGuid(),
            OuterWidth = 900,
            OuterHeight = 2100,
            OriginWorldX = 0,
            OriginWorldY = 0,
            Root = FrameNode.CreateRoot(0, 0, 900, 2100)
        };

        var rightSubFrame = new SubFrame
        {
            Id = Guid.NewGuid(),
            OuterWidth = 1400,
            OuterHeight = 2100,
            OriginWorldX = 946,
            OriginWorldY = 0,
            TransitionCoverStripMm = 4,
            Root = FrameNode.CreateRoot(0, 0, 1400, 2100)
        };

        transition.LeftSubFrameId = leftSubFrame.Id;
        transition.RightSubFrameId = rightSubFrame.Id;

        var assembly = new ConstructionAssembly
        {
            GlobalFrame = GlobalFrame.FromBounds(0, 0, 2346, 2100),
            TransitionProfiles = [transition],
            SubFrames = [leftSubFrame, rightSubFrame]
        };

        var (left, right) = SubFrameCoordinateSystem.ClipHorizontalSpan(
            rightSubFrame,
            assembly,
            spanLeftWorld: 946,
            spanRightWorld: 2346,
            axisY: 1050);

        Assert.Equal(transition.InnerRightFace, left);
        Assert.Equal(2346, right);
    }

    [Fact]
    public void FindOwnerAt_RejectsPhysicalTransitionBody()
    {
        var left = new SubFrame
        {
            Id = Guid.NewGuid(),
            OrderIndex = 0,
            OriginWorldX = 0,
            OriginWorldY = 0,
            OuterWidth = 900,
            OuterHeight = 2100,
            Root = FrameNode.CreateRoot(0, 0, 900, 2100)
        };
        var right = new SubFrame
        {
            Id = Guid.NewGuid(),
            OrderIndex = 1,
            OriginWorldX = 900,
            OriginWorldY = 0,
            OuterWidth = 1400,
            OuterHeight = 2100,
            Root = FrameNode.CreateRoot(0, 0, 1400, 2100)
        };
        var transition = new TransitionProfileModel
        {
            AxisCoordinate = 900,
            ThicknessMm = 46,
            Orientation = GridLineOrientation.Vertical,
            LeftSubFrameId = left.Id,
            RightSubFrameId = right.Id
        };
        var assembly = new ConstructionAssembly
        {
            GlobalFrame = GlobalFrame.FromBounds(0, 0, 2300, 2100),
            SubFrames = [left, right],
            TransitionProfiles = [transition]
        };

        Assert.Same(left, assembly.FindOwnerAt(800, 1000));
        Assert.Null(assembly.FindOwnerAt(900, 1000));
        Assert.Null(assembly.FindOwnerAt(920, 1000));
        Assert.Same(right, assembly.FindOwnerAt(924, 1000));
    }

    [Fact]
    public void ResizeBounds_RelayoutsEachSubFrameLocally()
    {
        var root = FrameNode.CreateRoot(0, 0, 2300, 2100, isFictitiousContainer: true);
        Assert.True(root.TrySplitVerticalAt(900, ImpostLineStyle.Dashed, 0));
        Assert.True(root.ChildB!.TrySplitVerticalAt(702, ImpostLineStyle.Solid, 60));
        var assembly = ConstructionAssemblyBuilder.Build(
            root,
            [
                new TransitionProfileModel
                {
                    AxisCoordinate = 900,
                    ThicknessMm = 46,
                    Orientation = GridLineOrientation.Vertical
                }
            ]);

        Assert.True(assembly.TryResizeBounds(100, 50, 4600, 2100));
        Assert.Equal(1800, assembly.SubFrames[0].OuterWidth);
        Assert.Equal(2800, assembly.SubFrames[1].OuterWidth);
        Assert.Equal(1900, assembly.SubFrames[1].OriginWorldX);
        Assert.Equal(1900, assembly.TransitionProfiles[0].AxisCoordinate);
        Assert.Equal(100, assembly.GlobalFrame.X);
    }

    [Fact]
    public void ConstructionAssemblyBuilder_Splits2300Into900Transition1400()
    {
        var root = FrameNode.CreateRoot(0, 0, 2300, 2100, isFictitiousContainer: true);
        Assert.True(root.TrySplitVerticalAt(900, ImpostLineStyle.Dashed, 0));
        Assert.NotNull(root.ChildB);
        Assert.True(root.ChildB!.TrySplitVerticalAt(702, ImpostLineStyle.Solid, 60));
        var transition = new TransitionProfileModel
        {
            AxisCoordinate = 900,
            ThicknessMm = 46,
            Orientation = GridLineOrientation.Vertical
        };

        var assembly = ConstructionAssemblyBuilder.Build(root, [transition]);

        Assert.Equal(2, assembly.SubFrames.Count);
        Assert.Equal(900, assembly.SubFrames[0].OuterWidth);
        Assert.Equal(1400, assembly.SubFrames[1].OuterWidth);
        Assert.True(assembly.ValidateOverallWidth(2346));
        Assert.Equal(4, assembly.SubFrames[1].TransitionCoverStripMm);

        var rightGrid = FrameNodeGridLineProjector.Project(assembly.SubFrames[1].Root);
        var rightAxis = Assert.Single(
            rightGrid.OfOrientation(GridLineOrientation.Vertical));
        Assert.Equal(698, rightAxis.Coordinate);

        var chain = SubFrameDimensionBuilder.BuildHorizontalChain(
            assembly.SubFrames[1],
            [rightAxis.Coordinate]);
        Assert.Equal([698, 698], chain.SegmentSizesMm);
    }

    [Fact]
    public void HorizontalImpost_RemainsLocalAndStopsAtTransition()
    {
        var root = FrameNode.CreateRoot(0, 0, 2300, 1300, isFictitiousContainer: true);
        Assert.True(root.TrySplitVerticalAt(900, ImpostLineStyle.Dashed, 0));
        Assert.True(root.ChildB!.TrySplitHorizontalAt(650, ImpostLineStyle.Dashed, 0));
        var assembly = ConstructionAssemblyBuilder.Build(
            root,
            [
                new TransitionProfileModel
                {
                    AxisCoordinate = 900,
                    ThicknessMm = 46,
                    Orientation = GridLineOrientation.Vertical
                }
            ]);

        var right = assembly.SubFrames[1];
        var horizontal = Assert.Single(
            FrameNodeGridLineProjector.Project(right.Root)
                .OfOrientation(GridLineOrientation.Horizontal));
        var chain = SubFrameDimensionBuilder.BuildVerticalChain(right, [horizontal.Coordinate]);
        Assert.Equal([650, 650], chain.SegmentHeightsMm);

        var clipped = SubFrameCoordinateSystem.ClipHorizontalSpan(
            right,
            assembly,
            right.OriginWorldX,
            right.OuterRightWorld,
            horizontal.Coordinate);
        Assert.Equal(assembly.TransitionProfiles[0].InnerRightFace, clipped.Left);
    }

    [Fact]
    public void ResizeLeftSubFrame_DoesNotChangeRightLocalDimensions()
    {
        var root = FrameNode.CreateRoot(0, 0, 2300, 1300, isFictitiousContainer: true);
        Assert.True(root.TrySplitVerticalAt(900, ImpostLineStyle.Dashed, 0));
        Assert.True(root.ChildB!.TrySplitVerticalAt(702, ImpostLineStyle.Solid, 60));
        var assembly = ConstructionAssemblyBuilder.Build(
            root,
            [
                new TransitionProfileModel
                {
                    AxisCoordinate = 900,
                    ThicknessMm = 46,
                    Orientation = GridLineOrientation.Vertical
                }
            ]);
        var right = assembly.SubFrames[1];
        var axisBefore = Assert.Single(
            FrameNodeGridLineProjector.Project(right.Root)
                .OfOrientation(GridLineOrientation.Vertical)).Coordinate;

        Assert.True(assembly.TryResizeSubFrameWidth(assembly.SubFrames[0].Id, 850));

        var axisAfter = Assert.Single(
            FrameNodeGridLineProjector.Project(right.Root)
                .OfOrientation(GridLineOrientation.Vertical)).Coordinate;
        var chain = SubFrameDimensionBuilder.BuildHorizontalChain(right, [axisAfter]);
        Assert.Equal(axisBefore, axisAfter);
        Assert.Equal([698, 698], chain.SegmentSizesMm);
        Assert.Equal(850, assembly.SubFrames[0].OuterWidth);
        Assert.Equal(850, assembly.TransitionProfiles[0].AxisCoordinate);
    }

    [Fact]
    public void FakeImpost_RoundTripPreservesOwnerAndStableGridLine()
    {
        var root = FrameNode.CreateRoot(0, 0, 1400, 1300, isFictitiousContainer: true);
        var assembly = ConstructionAssemblyBuilder.Build(root, []);
        var owner = Assert.Single(assembly.SubFrames);
        Assert.True(owner.Root.TrySplitVerticalAt(698, ImpostLineStyle.Dashed, 0));
        var before = Assert.Single(
            FrameNodeGridLineProjector.Project(owner.Root)
                .OfOrientation(GridLineOrientation.Vertical));

        var restored = ConstructionAssemblyMapper.FromData(
            ConstructionAssemblyMapper.ToData(assembly));
        var restoredOwner = Assert.Single(restored.SubFrames);
        var after = Assert.Single(
            FrameNodeGridLineProjector.Project(restoredOwner.Root)
                .OfOrientation(GridLineOrientation.Vertical));

        Assert.Equal(owner.Id, restoredOwner.Id);
        Assert.Equal(restoredOwner.Id, restoredOwner.Root.OwnerSubFrameId);
        Assert.All(
            restoredOwner.Root.EnumerateAllNodes(),
            node => Assert.Equal(restoredOwner.Id, node.OwnerSubFrameId));
        Assert.Equal(before.Id, after.Id);
        Assert.True(restored.ValidateOwnership());
    }

    [Fact]
    public void SplitExistingAssemblyAtVerticalTransition_PreservesIntactFakeImpostNodeIds()
    {
        var root = FrameNode.CreateRoot(0, 0, 2300, 1300, isFictitiousContainer: true);
        Assert.True(root.TrySplitVerticalAt(450, ImpostLineStyle.Dashed, 0));
        var leftImpostNodeId = root.NodeId;
        var leftLeafId = root.ChildA!.NodeId;
        Assert.True(root.ChildB!.TrySplitVerticalAt(900, ImpostLineStyle.Dashed, 0));
        var rightImpostNodeId = root.ChildB.NodeId;
        var farLeafId = root.ChildB.ChildB!.NodeId;

        var ownerId = Guid.NewGuid();
        root.AssignOwnerSubFrame(ownerId);
        var owner = new SubFrame
        {
            Id = ownerId,
            OuterWidth = 2300,
            OuterHeight = 1300,
            OriginWorldX = 0,
            OriginWorldY = 0,
            Root = root
        };
        var assembly = new ConstructionAssembly
        {
            GlobalFrame = GlobalFrame.FromBounds(0, 0, 2300, 1300),
            SubFrames = [owner]
        };

        var beforeLeftGridId = Assert.Single(
            FrameNodeGridLineProjector.Project(owner.Root)
                .OfOrientation(GridLineOrientation.Vertical)
                .Where(line => Math.Abs(line.Coordinate - 450) < 1)).Id;
        var beforeRightGridId = Assert.Single(
            FrameNodeGridLineProjector.Project(owner.Root)
                .OfOrientation(GridLineOrientation.Vertical)
                .Where(line => Math.Abs(line.Coordinate - 1350) < 1)).Id;

        var split = ConstructionAssemblyBuilder.SplitExistingAssemblyAtVerticalTransition(
            assembly,
            owner,
            new TransitionProfileModel
            {
                AxisCoordinate = 900,
                ThicknessMm = 46,
                Orientation = GridLineOrientation.Vertical
            });

        Assert.Equal(2, split.SubFrames.Count);
        var left = split.SubFrames[0];
        var right = split.SubFrames[1];
        Assert.Equal(900, left.OuterWidth);
        Assert.Equal(1400, right.OuterWidth);

        Assert.Contains(left.Root.EnumerateAllNodes(), node => node.NodeId == leftImpostNodeId);
        Assert.Contains(left.Root.EnumerateAllNodes(), node => node.NodeId == leftLeafId);
        Assert.DoesNotContain(left.Root.EnumerateAllNodes(), node => node.NodeId == rightImpostNodeId);
        Assert.Contains(right.Root.EnumerateAllNodes(), node => node.NodeId == rightImpostNodeId);
        Assert.Contains(right.Root.EnumerateAllNodes(), node => node.NodeId == farLeafId);

        var afterLeftGridId = Assert.Single(
            FrameNodeGridLineProjector.Project(left.Root)
                .OfOrientation(GridLineOrientation.Vertical)).Id;
        var afterRightGridId = Assert.Single(
            FrameNodeGridLineProjector.Project(right.Root)
                .OfOrientation(GridLineOrientation.Vertical)).Id;
        Assert.Equal(beforeLeftGridId, afterLeftGridId);
        Assert.Equal(beforeRightGridId, afterRightGridId);
    }
}
