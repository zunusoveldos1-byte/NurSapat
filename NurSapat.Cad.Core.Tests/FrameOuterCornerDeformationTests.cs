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
}
