using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.Core.Tests;

public sealed class SegmentedHorizontalImpostTests
{
    [Fact]
    public void UniformVerticalSplit_CenterToCenter_LeavesAxesAtExactThirds()
    {
        var root = FrameNode.CreateRoot(0, 0, 3000, 2000);

        Assert.True(root.TrySplitUniformWithVolume(
            SplitOrientation.Vertical,
            impostCount: 2,
            impostWidthMm: 60,
            lineStyle: ImpostLineStyle.Solid));

        Assert.True(root.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, 0, out var firstAxis));
        Assert.True(root.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, 1, out var secondAxis));
        Assert.Equal(1000, firstAxis);
        Assert.Equal(2000, secondAxis);
    }

    [Fact]
    public void UniformVerticalSplit_ClearOpening_KeepsAllClearWidthsEqual()
    {
        var root = FrameNode.CreateRoot(0, 0, 3000, 2000);

        Assert.True(root.TrySplitUniformWithVolume(
            SplitOrientation.Vertical,
            impostCount: 2,
            impostWidthMm: 60,
            lineStyle: ImpostLineStyle.Solid,
            distribution: UniformSplitDistribution.ClearOpening));

        var cells = root.UniformCells;
        Assert.Equal(3, cells.Count);
        Assert.Equal(cells[0].Width, cells[1].Width);
        Assert.Equal(cells[1].Width, cells[2].Width);
    }

    [Fact]
    public void HorizontalSegments_FollowVerticalCells_AndCanMoveIndependently()
    {
        var root = FrameNode.CreateRoot(0, 0, 3000, 2000);
        Assert.True(root.TrySplitUniformWithVolume(
            SplitOrientation.Vertical,
            impostCount: 2,
            impostWidthMm: 80,
            lineStyle: ImpostLineStyle.Solid));

        var cells = root.EnumerateTerminalLeaves().OrderBy(cell => cell.X).ToList();
        Assert.Equal(3, cells.Count);
        Assert.All(cells, cell => Assert.True(cell.TrySplitHorizontalAt(1000, ImpostLineStyle.Solid, 80)));

        // В корне остаются только вертикальные импосты. Каждая горизонталь —
        // самостоятельный узел своей колонки, а не единый профиль на всю ширину.
        Assert.Equal(SplitOrientation.Vertical, root.SplitOrientation);
        var horizontalSegments = root.EnumerateInternalNodes()
            .Where(node => node.SplitOrientation == SplitOrientation.Horizontal)
            .ToList();
        Assert.Equal(3, horizontalSegments.Count);
        Assert.All(horizontalSegments, segment => Assert.Contains(segment, root.UniformCells));

        // Сегменты заканчиваются на границах световых ячеек, до тел вертикальных импостов.
        Assert.True(cells[0].X + cells[0].Width < cells[1].X);
        Assert.True(cells[1].X + cells[1].Width < cells[2].X);
        Assert.All(cells, cell => Assert.True(cell.TryGetHorizontalImpostAxisWorldY(out var axis) && axis == 1000));

        Assert.True(cells[1].TrySetHorizontalImpostAxisWorldY(1350));
        Assert.True(cells[0].TryGetHorizontalImpostAxisWorldY(out var leftAxis));
        Assert.True(cells[1].TryGetHorizontalImpostAxisWorldY(out var middleAxis));
        Assert.True(cells[2].TryGetHorizontalImpostAxisWorldY(out var rightAxis));

        Assert.Equal(1000, leftAxis);
        Assert.Equal(1350, middleAxis);
        Assert.Equal(1000, rightAxis);
    }

    [Fact]
    public void SingleHorizontalSplit_ChangesOnlyClickedCell_AndStopsAtVerticalProfileFace()
    {
        var root = FrameNode.CreateRoot(0, 0, 3000, 2000);
        Assert.True(root.TrySplitVerticalAt(1500, ImpostLineStyle.Solid, 80));

        var leftCell = root.ChildA!;
        var rightCell = root.ChildB!;
        Assert.True(leftCell.TrySplitHorizontalAt(1000, ImpostLineStyle.Solid, 80));

        Assert.Equal(SplitOrientation.Horizontal, leftCell.SplitOrientation);
        Assert.True(rightCell.IsTerminalLeaf);
        Assert.Empty(rightCell.EnumerateInternalNodes());

        Assert.Equal(1460, leftCell.X + leftCell.Width);
        Assert.Equal(1540, rightCell.X);
        Assert.Equal(80, rightCell.X - (leftCell.X + leftCell.Width));
    }

    [Fact]
    public void HorizontalHalfSplit_ProducesEqualClearOpeningHeights()
    {
        var cell = FrameNode.CreateRoot(0, 0, 1000, 1000);

        Assert.True(cell.TrySplitHorizontalHalf(ImpostLineStyle.Solid, 20));
        Assert.True(cell.TryGetHorizontalLightOpeningHeights(out var bottom, out var top));

        Assert.Equal(490, bottom);
        Assert.Equal(490, top);
        Assert.Equal(490, cell.ChildA!.Height);
        Assert.Equal(510, cell.ChildB!.Y);
    }
}
