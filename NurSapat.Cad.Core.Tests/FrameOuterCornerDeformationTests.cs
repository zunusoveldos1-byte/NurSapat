using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.Core.Tests;

public sealed class FrameOuterCornerDeformationTests
{
    [Fact]
    public void TrySetOuterCorner_DropsTopLeft_KeepsOverallHeight()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);

        Assert.True(root.TrySetOuterCorner(FrameOuterCornerKind.TopLeft, new CadPoint(0, 1218)));

        Assert.Equal(1400, root.Height);
        Assert.Equal(1600, root.Width);
        Assert.True(root.HasDeformedOuterContour());
        Assert.Equal(1218, root.GetOuterCorner(FrameOuterCornerKind.TopLeft).Y);
        Assert.Equal(1400, root.GetOuterCorner(FrameOuterCornerKind.TopRight).Y);
    }

    [Fact]
    public void ResolveRailBand_SkewedTop_HasSlopedOuterEdge()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        Assert.True(root.TrySetOuterCorner(FrameOuterCornerKind.TopLeft, new CadPoint(0, 1218)));

        var band = root.GetRailBand(FrameProfileRail.Top, profileThickness: 70);
        Assert.Equal(0, band.OuterStart.X);
        Assert.Equal(1218, band.OuterStart.Y);
        Assert.Equal(1600, band.OuterEnd.X);
        Assert.Equal(1400, band.OuterEnd.Y);
    }

    [Fact]
    public void LeftHeightBoundaries_IncludeDroppedCornerY()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        Assert.True(root.TrySetOuterCorner(FrameOuterCornerKind.TopLeft, new CadPoint(0, 1218)));

        var (start, end) = FrameProfileRailGeometry.ResolveOuterEdge(
            root.GetOuterCorners(),
            FrameProfileRail.Left);
        Assert.Equal(0, start.Y);
        Assert.Equal(1218, end.Y);

        var projection = Math.Abs(end.Y - start.Y);
        Assert.Equal(1218, projection);
        Assert.Equal(182, root.Height - projection);
    }

    [Fact]
    public void TopProjection_UsesHorizontalSpan()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        Assert.True(root.TrySetOuterCorner(FrameOuterCornerKind.TopLeft, new CadPoint(821, 1218)));

        var projections = root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Top);
        Assert.Single(projections);
        Assert.Equal(779, projections[0]);
    }

    [Fact]
    public void Snapshot_RestoresDeformedCorners()
    {
        var root = FrameNode.CreateRoot(0, 0, 1000, 1000);
        Assert.True(root.TrySetOuterCorner(FrameOuterCornerKind.TopRight, new CadPoint(1000, 800)));
        var snapshot = root.CaptureTreeSnapshot();

        var restored = FrameNode.RestoreTree(snapshot);
        Assert.NotNull(restored);
        Assert.True(restored!.HasDeformedOuterContour());
        Assert.Equal(800, restored.GetOuterCorner(FrameOuterCornerKind.TopRight).Y);
    }

    [Fact]
    public void SplitTopJoint_LocksHorizontalAxis_AndUpdatesSegmentLengths()
    {
        var root = FrameNode.CreateRoot(0, 0, 1000, 1000);
        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        var joint = Assert.Single(root.GetProfileRailJoints(FrameProfileRail.Top));

        Assert.True(root.TrySetProfileRailJointPosition(joint.Id, new CadPoint(400, 1200)));

        var points = root.GetOuterRailPolylinePoints(FrameProfileRail.Top);
        Assert.Equal(new CadPoint(500, 1200), points[1]);
        var lengths = root.GetOuterRailSegmentLengthsMm(FrameProfileRail.Top);
        Assert.Equal(2, lengths.Count);
        Assert.Equal(lengths[0], lengths[1]);
    }

    [Fact]
    public void BentTopRail_MiterInsetStaysInsideProfile()
    {
        var root = FrameNode.CreateRoot(0, 0, 1000, 1000);
        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        var joint = Assert.Single(root.GetProfileRailJoints(FrameProfileRail.Top));
        Assert.True(root.TrySetProfileRailJointPosition(joint.Id, new CadPoint(500, 1200)));

        var band = root.GetRailBand(FrameProfileRail.Top, 80);
        var outerJoint = root.GetOuterRailPolylinePoints(FrameProfileRail.Top)[1];
        var segments = FrameProfileRailGeometry.SplitRailBandWithMiters(band, [outerJoint]);

        Assert.Equal(2, segments.Count);
        Assert.Equal(segments[0].InnerEnd, segments[1].InnerStart);
        Assert.True(segments[0].InnerEnd.Y < outerJoint.Y);
    }

    [Fact]
    public void Snapshot_RestoresFreeSplitJointPosition()
    {
        var root = FrameNode.CreateRoot(0, 0, 1000, 1000);
        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        var joint = Assert.Single(root.GetProfileRailJoints(FrameProfileRail.Top));
        Assert.True(root.TrySetProfileRailJointPosition(joint.Id, new CadPoint(450, 1180)));

        var restored = FrameNode.RestoreTree(root.CaptureTreeSnapshot());
        Assert.NotNull(restored);
        Assert.Equal(
            new CadPoint(500, 1180),
            restored!.GetOuterRailPolylinePoints(FrameProfileRail.Top)[1]);
    }

    [Fact]
    public void MovingTopLeftCorner_DoesNotMoveCentralSplitAnchor()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1200);
        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        var anchorBefore = root.GetOuterRailPolylinePoints(FrameProfileRail.Top)[1];

        Assert.True(root.TrySetOuterCorner(
            FrameOuterCornerKind.TopLeft,
            new CadPoint(0, 900)));

        var top = root.GetOuterRailPolylinePoints(FrameProfileRail.Top);
        Assert.Equal(new CadPoint(0, 900), top[0]);
        Assert.Equal(anchorBefore, top[1]);
        Assert.Equal(new CadPoint(1600, 1200), top[2]);
    }

    [Fact]
    public void MovingTopRightCorner_DoesNotMoveCentralSplitOrLeftCorner()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1200);
        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        var anchorBefore = root.GetOuterRailPolylinePoints(FrameProfileRail.Top)[1];
        var leftBefore = root.GetOuterCorner(FrameOuterCornerKind.TopLeft);

        Assert.True(root.TrySetOuterCorner(
            FrameOuterCornerKind.TopRight,
            new CadPoint(1600, 850)));

        var top = root.GetOuterRailPolylinePoints(FrameProfileRail.Top);
        Assert.Equal(leftBefore, top[0]);
        Assert.Equal(anchorBefore, top[1]);
        Assert.Equal(new CadPoint(1600, 850), top[2]);
    }

    [Fact]
    public void ResizeOverallHeight_PreservesRoofRiseAndTopSplit()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        Assert.True(root.TrySetOuterCorner(
            FrameOuterCornerKind.TopLeft,
            new CadPoint(0, 1150)));
        Assert.True(root.TrySetOuterCorner(
            FrameOuterCornerKind.TopRight,
            new CadPoint(1600, 1196)));

        Assert.True(root.TryResizeOuter(1600, 1600));

        var top = root.GetOuterRailPolylinePoints(FrameProfileRail.Top);
        Assert.Equal(new CadPoint(0, 1350), top[0]);
        Assert.Equal(new CadPoint(800, 1600), top[1]);
        Assert.Equal(new CadPoint(1600, 1396), top[2]);
        Assert.Equal([800d, 800d], root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Top));
        Assert.Single(root.GetProfileRailJoints(FrameProfileRail.Top));
    }

    [Fact]
    public void SequentialSnapshots_RestoreSplitAndEachCornerMoveIndependently()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        var initial = root.CaptureTreeSnapshot();

        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        var afterSplit = root.CaptureTreeSnapshot();
        Assert.True(root.TrySetOuterCorner(
            FrameOuterCornerKind.TopLeft,
            new CadPoint(0, 1150)));
        var afterLeftDrag = root.CaptureTreeSnapshot();

        var undoDrag = FrameNode.RestoreTree(afterSplit);
        Assert.NotNull(undoDrag);
        Assert.Single(undoDrag!.GetProfileRailJoints(FrameProfileRail.Top));
        Assert.Equal(1400, undoDrag.GetOuterCorner(FrameOuterCornerKind.TopLeft).Y);

        var undoSplit = FrameNode.RestoreTree(initial);
        Assert.NotNull(undoSplit);
        Assert.Empty(undoSplit!.GetProfileRailJoints(FrameProfileRail.Top));

        var redoDrag = FrameNode.RestoreTree(afterLeftDrag);
        Assert.NotNull(redoDrag);
        Assert.Single(redoDrag!.GetProfileRailJoints(FrameProfileRail.Top));
        Assert.Equal(1150, redoDrag.GetOuterCorner(FrameOuterCornerKind.TopLeft).Y);
    }

    [Fact]
    public void RecursiveSplit_LeftHalf_ProducesFourHundredFourHundredEightHundredChain()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        Assert.True(root.TrySplitProfileRailSegmentInHalf(FrameProfileRail.Top, 0));
        Assert.True(root.TrySplitProfileRailSegmentInHalf(FrameProfileRail.Top, 0));

        Assert.Equal(
            [400d, 400d, 800d],
            root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Top));
        Assert.Equal(
            [0d, 400d, 800d, 1600d],
            root.GetOuterRailBoundaryCoordinates(FrameProfileRail.Top));
        Assert.Equal(2, root.GetProfileRailJoints(FrameProfileRail.Top).Count);
    }

    [Fact]
    public void RecursiveSplit_DoesNotMoveExistingRoofApex()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        Assert.True(root.TrySplitProfileRailSegmentInHalf(FrameProfileRail.Top, 0));
        var apex = Assert.Single(root.GetProfileRailJoints(FrameProfileRail.Top));
        Assert.True(root.TrySetProfileRailJointPosition(apex.Id, new CadPoint(800, 1650)));

        Assert.True(root.TrySplitProfileRailSegmentInHalf(FrameProfileRail.Top, 1));

        var points = root.GetOuterRailPolylinePoints(FrameProfileRail.Top);
        Assert.Equal(new CadPoint(800, 1650), points[1]);
        Assert.Equal(new CadPoint(1200, 1525), points[2]);
        Assert.Equal([800d, 400d, 400d], root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Top));
    }

    [Fact]
    public void ExactCoordinateEdit_CanMoveLockedApexHorizontally()
    {
        var root = FrameNode.CreateRoot(0, 0, 1600, 1400);
        Assert.True(root.TrySplitProfileRailInHalf(FrameProfileRail.Top));
        var apex = Assert.Single(root.GetProfileRailJoints(FrameProfileRail.Top));

        Assert.True(root.TrySetProfileRailJointPosition(
            apex.Id,
            new CadPoint(450, 1400),
            overrideLockedAxis: true));

        Assert.Equal(new CadPoint(450, 1400), root.ResolveProfileRailJointOuterPoint(apex));
        Assert.Equal(
            [450d, 1150d],
            root.GetOuterRailSegmentProjectionsMm(FrameProfileRail.Top));
    }
}
