using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Models;

public enum SplitOrientation
{
    None,
    Vertical,
    Horizontal
}

/// <summary>
/// Бинарный узел рамы: лист или разделённый импостом на два дочерних прямоугольника.
/// Координаты: X,Y — нижний левый угол, Y растёт вверх.
/// </summary>
public sealed class FrameNode
{
    private const double MinSplitSizeMm = 40;

    public const double DefaultImpostProfileMm = CadSystemThickness.DefaultImpostProfileMm;

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsLeaf { get; private set; } = true;
    public bool IsUniformSplit { get; private set; }
    public SplitOrientation SplitOrientation { get; private set; } = SplitOrientation.None;
    public ImpostLineStyle LineStyle { get; private set; } = ImpostLineStyle.Dashed;
    public double ImpostThicknessMm { get; private set; }
    public int UniformImpostCount { get; private set; }
    public IReadOnlyList<FrameNode> UniformCells => _uniformCells;
    public FrameNode? ChildA { get; private set; }
    public FrameNode? ChildB { get; private set; }
    public FrameNode? Parent { get; set; }

    /// <summary>Верхний профиль коробки (только у корневого узла).</summary>
    public ProfileSegment? TopProfile { get; set; }

    private readonly List<FrameNode> _uniformCells = [];

    /// <summary>Корневой узел дерева рамы (узел без родителя).</summary>
    public FrameNode GetRoot()
    {
        var node = this;
        while (node.Parent is not null)
        {
            node = node.Parent;
        }

        return node;
    }

    /// <summary>
    /// Локальная вертикальная протяжённость горизонтального разбиения: прямоугольная высота узла
    /// плюс стрела арки, если верх узла совпадает с хордой коробки.
    /// </summary>
    public static double ResolveHorizontalSplitExtentLocal(FrameNode splitNode, FrameNode? frameRoot)
    {
        var extent = splitNode.Height;
        if (frameRoot is null)
        {
            return extent;
        }

        var profile = frameRoot.TopProfile;
        if (profile?.SegmentType != SegmentType.Arc ||
            profile.ArcHeight <= ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return extent;
        }

        var frameTop = frameRoot.Y + frameRoot.Height;
        if (!TouchesFrameTopEdge(splitNode, frameTop))
        {
            return extent;
        }

        return extent + profile.ArcHeight;
    }

    private double GetHorizontalSplitExtentLocal() =>
        ResolveHorizontalSplitExtentLocal(this, GetRoot());

    private static bool TouchesFrameTopEdge(FrameNode node, double frameTop) =>
        CadMath.NearlyEqual(node.Y + node.Height, frameTop)
        || node.Y + node.Height >= frameTop - CadMath.MmPrecision;

    public bool Contains(double x, double y) =>
        x >= X && x <= X + Width && y >= Y && y <= Y + Height;

    public FrameNode? FindLeafAt(double x, double y)
    {
        // Используем допуск 25 мм, чтобы поиск был устойчив к смещениям из-за нахлестов
        const double lookupTolerance = 25.0;

        if (x < X - lookupTolerance || x > X + Width + lookupTolerance ||
            y < Y - lookupTolerance || y > Y + Height + lookupTolerance)
        {
            return null;
        }

        if (IsUniformSplit)
        {
            // Ищем ближайшую ячейку по расстоянию до центра, если точка на стыке профилей
            FrameNode? bestCell = null;
            double minDist = double.MaxValue;

            foreach (var cell in _uniformCells)
            {
                if (cell.Contains(x, y))
                {
                    return cell.FindLeafAt(x, y);
                }

                // Расчет расстояния до центра ячейки на случай выхода за точные математические границы проёма
                double cellCenterX = cell.X + cell.Width / 2.0;
                double cellCenterY = cell.Y + cell.Height / 2.0;
                double dist = Math.Pow(x - cellCenterX, 2) + Math.Pow(y - cellCenterY, 2);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestCell = cell;
                }
            }

            return bestCell?.FindLeafAt(x, y);
        }

        if (IsLeaf)
        {
            return this;
        }

        // Логика для бинарного сплита (ChildA / ChildB)
        if (ChildA != null && ChildB != null)
        {
            if (ChildA.Contains(x, y)) return ChildA.FindLeafAt(x, y);
            if (ChildB.Contains(x, y)) return ChildB.FindLeafAt(x, y);

            // Если точка зависла ровно на импосте, определяем по полуплоскостям
            if (SplitOrientation == SplitOrientation.Vertical)
            {
                return x <= (ChildA.X + ChildA.Width + ChildB.X) / 2.0
                    ? ChildA.FindLeafAt(x, y)
                    : ChildB.FindLeafAt(x, y);
            }
            else if (SplitOrientation == SplitOrientation.Horizontal)
            {
                return y <= (ChildA.Y + ChildA.Height + ChildB.Y) / 2.0
                    ? ChildA.FindLeafAt(x, y)
                    : ChildB.FindLeafAt(x, y);
            }
        }

        return IsLeaf ? this : null;
    }

    public bool AddSplit(
        SplitOrientation orientation,
        double splitLocal,
        ImpostLineStyle lineStyle,
        double impostWidthMm) =>
        orientation switch
        {
            SplitOrientation.Vertical => TrySplitVerticalAt(splitLocal, lineStyle, impostWidthMm),
            SplitOrientation.Horizontal => TrySplitHorizontalAt(splitLocal, lineStyle, impostWidthMm),
            _ => false
        };

    public bool TrySplitVerticalHalf(ImpostLineStyle lineStyle = ImpostLineStyle.Dashed, double impostWidthMm = 0) =>
        TrySplitVerticalAt(Width / 2, lineStyle, impostWidthMm);

    public bool TrySplitHorizontalHalf(ImpostLineStyle lineStyle = ImpostLineStyle.Dashed, double impostWidthMm = 0) =>
        TrySplitHorizontalAt(Height / 2, lineStyle, impostWidthMm);

    /// <summary>
    /// N-split engine: равномерное деление светового проёма с физическим объёмом каждого импоста.
    /// </summary>
    public bool TrySplitUniformWithVolume(
        SplitOrientation orientation,
        int impostCount,
        double impostWidthMm,
        double frameProfileInset = 0,
        ImpostLineStyle lineStyle = ImpostLineStyle.Solid)
    {
        if (!IsLeaf || impostCount < 1)
        {
            return false;
        }

        var impostWidth = impostWidthMm > CadMath.Epsilon
            ? impostWidthMm
            : DefaultImpostProfileMm;

        if (!TryComputeUniformLayout(
                orientation,
                impostCount,
                impostWidth,
                frameProfileInset,
                out var innerStart,
                out var singleGlassSize))
        {
            return false;
        }

        _uniformCells.Clear();
        var trailingInset = Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0;
        var innerSpan = orientation switch
        {
            SplitOrientation.Vertical => Width - innerStart - trailingInset,
            SplitOrientation.Horizontal => Height - innerStart - trailingInset,
            _ => 0
        };

        var used = 0.0;
        for (var i = 0; i <= impostCount; i++)
        {
            var isLast = i == impostCount;
            var cellWidth = isLast
                ? CadMath.RoundMm(innerSpan - used)
                : CadMath.RoundMm(singleGlassSize);

            var offset = innerStart + used;
            var cell = orientation switch
            {
                SplitOrientation.Vertical => CreateChild(X + offset, Y, cellWidth, Height),
                SplitOrientation.Horizontal => CreateChild(X, Y + offset, Width, cellWidth),
                _ => null
            };

            if (cell == null)
            {
                _uniformCells.Clear();
                return false;
            }

            _uniformCells.Add(cell);
            used += cellWidth + (isLast ? 0 : impostWidth);
        }

        ChildA = null;
        ChildB = null;
        IsUniformSplit = true;
        IsLeaf = false;
        SplitOrientation = orientation;
        LineStyle = lineStyle;
        ImpostThicknessMm = impostWidth;
        UniformImpostCount = impostCount;
        return true;
    }

    public bool TrySplitUniformWithVolume(int impostCount, double impostWidthMm) =>
        TrySplitUniformWithVolume(SplitOrientation.Vertical, impostCount, impostWidthMm);

    public bool CanAcceptUniformSplit(
        SplitOrientation orientation,
        int impostCount,
        double impostWidthMm,
        double frameProfileInset = 0)
    {
        if (!IsLeaf || impostCount < 1)
        {
            return false;
        }

        var impostWidth = impostWidthMm > CadMath.Epsilon
            ? impostWidthMm
            : DefaultImpostProfileMm;

        return TryComputeUniformLayout(
            orientation,
            impostCount,
            impostWidth,
            frameProfileInset,
            out _,
            out _);
    }

    public static bool TryComputeUniformLayout(
        SplitOrientation orientation,
        double totalWidth,
        double totalHeight,
        int impostCount,
        double impostWidthMm,
        double frameProfileInset,
        out double singleGlassSize)
    {
        singleGlassSize = 0;
        var innerSize = orientation switch
        {
            SplitOrientation.Vertical => frameProfileInset > CadMath.Epsilon
                ? totalWidth - frameProfileInset * 2
                : totalWidth,
            SplitOrientation.Horizontal => frameProfileInset > CadMath.Epsilon
                ? totalHeight - frameProfileInset * 2
                : totalHeight,
            _ => 0
        };

        if (innerSize <= CadMath.Epsilon || impostCount < 1)
        {
            return false;
        }

        var impostWidth = impostWidthMm > CadMath.Epsilon
            ? impostWidthMm
            : DefaultImpostProfileMm;
        var totalImpostsWidth = impostCount * impostWidth;
        var netGlassSize = innerSize - totalImpostsWidth;
        if (netGlassSize <= CadMath.Epsilon)
        {
            return false;
        }

        singleGlassSize = CadMath.RoundMm(netGlassSize / (impostCount + 1));
        return singleGlassSize >= MinSplitSizeMm;
    }

    private bool TryComputeUniformLayout(
        SplitOrientation orientation,
        int impostCount,
        double impostWidth,
        double frameProfileInset,
        out double innerStart,
        out double singleGlassSize)
    {
        innerStart = Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0;
        return TryComputeUniformLayout(
            orientation,
            Width,
            Height,
            impostCount,
            impostWidth,
            Parent == null ? frameProfileInset : 0,
            out singleGlassSize);
    }

    /// <summary>
    /// Двойной вертикальный импост: 1 импост по центру (совместимость).
    /// </summary>
    public bool TrySplitDoubleVerticalCentered(double frameProfileWidth, double impostWidthMm) =>
        TrySplitUniformWithVolume(
            SplitOrientation.Vertical,
            impostCount: 1,
            impostWidthMm,
            frameProfileWidth,
            ImpostLineStyle.Double);

    /// <summary>
    /// Двойной горизонтальный импост: 1 импост по центру (совместимость).
    /// </summary>
    public bool TrySplitDoubleHorizontalCentered(double frameProfileWidth, double impostWidthMm) =>
        TrySplitUniformWithVolume(
            SplitOrientation.Horizontal,
            impostCount: 1,
            impostWidthMm,
            frameProfileWidth,
            ImpostLineStyle.Double);

    public bool TrySplitVerticalAt(double splitLocalX, ImpostLineStyle lineStyle, double impostWidthMm)
    {
        if (!IsLeaf)
        {
            return false;
        }

        var thickness = ResolveImpostWidth(lineStyle, impostWidthMm);
        var centerLocal = ClampVerticalSplitCenter(splitLocalX, thickness);
        if (!TryComputeBinaryVerticalChildren(centerLocal, thickness, out var leftWidth, out var childBStartLocal, out var rightWidth))
        {
            return false;
        }

        ChildA = CreateChild(X, Y, leftWidth, Height);
        ChildB = CreateChild(X + childBStartLocal, Y, rightWidth, Height);
        ClearUniformSplitState();
        ImpostThicknessMm = thickness;
        IsLeaf = false;
        SplitOrientation = SplitOrientation.Vertical;
        LineStyle = lineStyle;
        return true;
    }

    public bool TrySplitHorizontalAt(double splitLocalY, ImpostLineStyle lineStyle, double impostWidthMm)
    {
        if (!IsLeaf)
        {
            return false;
        }

        var thickness = ResolveImpostWidth(lineStyle, impostWidthMm);
        var centerLocal = ClampHorizontalSplitCenter(splitLocalY, thickness, Height);
        if (!TryComputeBinaryHorizontalChildren(centerLocal, thickness, Height, out var bottomHeight, out var childBStartLocal, out var topHeight))
        {
            return false;
        }

        ChildA = CreateChild(X, Y, Width, bottomHeight);
        ChildB = CreateChild(X, Y + childBStartLocal, Width, topHeight);
        ClearUniformSplitState();
        ImpostThicknessMm = thickness;
        IsLeaf = false;
        SplitOrientation = SplitOrientation.Horizontal;
        LineStyle = lineStyle;
        return true;
    }

    /// <summary>
    /// Толщина импоста узла (мм).
    /// </summary>
    public double GetResolvedImpostThicknessMm() =>
        ImpostThicknessMm > CadMath.Epsilon
            ? ImpostThicknessMm
            : DefaultImpostProfileMm;

    /// <summary>
    /// Мировая X-координата оси вертикального импоста (совпадает с засечкой размера).
    /// </summary>
    public bool TryGetVerticalImpostAxisWorldX(out double axisWorldX)
    {
        axisWorldX = 0;
        if (IsLeaf || IsUniformSplit || SplitOrientation != SplitOrientation.Vertical
            || ChildA is null || ChildB is null)
        {
            return false;
        }

        axisWorldX = CadMath.RoundMm(ChildA.X + ChildA.Width + GetResolvedImpostThicknessMm() / 2.0);
        return true;
    }

    /// <summary>
    /// Мировая Y-координата оси горизонтального импоста (совпадает с засечкой размера).
    /// </summary>
    public bool TryGetHorizontalImpostAxisWorldY(out double axisWorldY)
    {
        axisWorldY = 0;
        if (IsLeaf || IsUniformSplit || SplitOrientation != SplitOrientation.Horizontal
            || ChildA is null || ChildB is null)
        {
            return false;
        }

        axisWorldY = CadMath.RoundMm(ChildA.Y + ChildA.Height + GetResolvedImpostThicknessMm() / 2.0);
        return true;
    }

    /// <summary>
    /// Ось импоста между ячейками uniform-разбиения (индекс зазора 0..N-1).
    /// </summary>
    public bool TryGetUniformImpostAxisWorld(
        SplitOrientation orientation,
        int gapIndex,
        out double axisWorld)
    {
        axisWorld = 0;
        if (!IsUniformSplit || SplitOrientation != orientation || gapIndex < 0 || gapIndex >= _uniformCells.Count - 1)
        {
            return false;
        }

        var leftCell = _uniformCells[gapIndex];
        var half = GetResolvedImpostThicknessMm() / 2.0;
        axisWorld = orientation switch
        {
            SplitOrientation.Vertical => CadMath.RoundMm(leftCell.X + leftCell.Width + half),
            SplitOrientation.Horizontal => CadMath.RoundMm(leftCell.Y + leftCell.Height + half),
            _ => 0
        };
        return true;
    }

    public bool TrySetVerticalImpostAxisWorldX(double axisWorldX)
    {
        if (IsLeaf || IsUniformSplit || SplitOrientation != SplitOrientation.Vertical
            || ChildA is null || ChildB is null)
        {
            return false;
        }

        var centerLocal = axisWorldX - X;
        var thickness = GetResolvedImpostThicknessMm();
        centerLocal = ClampVerticalSplitCenter(centerLocal, thickness);
        if (!TryComputeBinaryVerticalChildren(centerLocal, thickness, out var leftWidth, out var childBStartLocal, out var rightWidth))
        {
            return false;
        }

        ChildA.X = X;
        ChildA.Y = Y;
        ChildA.Width = leftWidth;
        ChildA.Height = Height;

        ChildB.X = X + childBStartLocal;
        ChildB.Y = Y;
        ChildB.Width = rightWidth;
        ChildB.Height = Height;
        return true;
    }

    public bool TrySetHorizontalImpostAxisWorldY(double axisWorldY)
    {
        if (IsLeaf || IsUniformSplit || SplitOrientation != SplitOrientation.Horizontal
            || ChildA is null || ChildB is null)
        {
            return false;
        }

        var centerLocal = axisWorldY - Y;
        var thickness = GetResolvedImpostThicknessMm();
        var splitExtent = GetHorizontalSplitExtentLocal();
        centerLocal = ClampHorizontalSplitCenter(centerLocal, thickness, splitExtent);
        if (!TryComputeBinaryHorizontalChildren(centerLocal, thickness, splitExtent, out var bottomHeight, out var childBStartLocal, out var topHeight))
        {
            return false;
        }

        ChildA.X = X;
        ChildA.Y = Y;
        ChildA.Width = Width;
        ChildA.Height = bottomHeight;

        ChildB.X = X;
        ChildB.Y = Y + childBStartLocal;
        ChildB.Width = Width;
        ChildB.Height = topHeight;
        return true;
    }

    /// <summary>
    /// Ширины световых проёмов слева/справа от вертикального импоста (между внутренними фальцами).
    /// </summary>
    public bool TryGetVerticalLightOpeningWidths(out double leftWidth, out double rightWidth)
    {
        leftWidth = rightWidth = 0;
        if (IsLeaf || IsUniformSplit || SplitOrientation != SplitOrientation.Vertical
            || ChildA is null || ChildB is null
            || !TryGetVerticalImpostAxisWorldX(out var axisWorldX))
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        leftWidth = CadMath.RoundMm((axisWorldX - X) - half);
        rightWidth = CadMath.RoundMm((X + Width - axisWorldX) - half);
        return leftWidth > CadMath.Epsilon && rightWidth > CadMath.Epsilon;
    }

    public bool TrySetVerticalLightOpeningLeftWidth(double leftWidth)
    {
        if (leftWidth <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        return TrySetVerticalImpostAxisWorldX(X + half + leftWidth);
    }

    public bool TrySetVerticalLightOpeningRightWidth(double rightWidth)
    {
        if (rightWidth <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        return TrySetVerticalImpostAxisWorldX(X + Width - half - rightWidth);
    }

    public bool TrySetUniformVerticalImpostGapAxisWorldX(int gapIndex, double axisWorldX)
    {
        if (!IsUniformSplit || SplitOrientation != SplitOrientation.Vertical
            || gapIndex < 0 || gapIndex >= _uniformCells.Count - 1)
        {
            return false;
        }

        var thickness = GetResolvedImpostThicknessMm();
        var half = thickness / 2.0;
        var leftCell = _uniformCells[gapIndex];
        var rightCell = _uniformCells[gapIndex + 1];

        var minAxis = leftCell.X + MinSplitSizeMm + half;
        var maxAxis = rightCell.X + rightCell.Width - MinSplitSizeMm - half;
        if (maxAxis < minAxis)
        {
            return false;
        }

        axisWorldX = Math.Clamp(CadMath.RoundMm(axisWorldX), minAxis, maxAxis);
        var newLeftWidth = CadMath.RoundMm(axisWorldX - leftCell.X - half);
        leftCell.Width = newLeftWidth;

        var rightEdge = rightCell.X + rightCell.Width;
        rightCell.X = CadMath.RoundMm(leftCell.X + newLeftWidth + thickness);
        rightCell.Width = CadMath.RoundMm(rightEdge - rightCell.X);
        if (rightCell.Width < MinSplitSizeMm)
        {
            return false;
        }

        var cursorX = rightCell.X + rightCell.Width + thickness;
        for (var i = gapIndex + 2; i < _uniformCells.Count; i++)
        {
            var cell = _uniformCells[i];
            cell.X = CadMath.RoundMm(cursorX);
            cursorX = cell.X + cell.Width + thickness;
        }

        return true;
    }

    /// <summary>
    /// Высоты световых проёмов снизу/сверху от горизонтального импоста.
    /// </summary>
    public bool TryGetHorizontalLightOpeningHeights(out double bottomHeight, out double topHeight)
    {
        bottomHeight = topHeight = 0;
        if (IsLeaf || IsUniformSplit || SplitOrientation != SplitOrientation.Horizontal
            || ChildA is null || ChildB is null
            || !TryGetHorizontalImpostAxisWorldY(out var axisWorldY))
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        var splitExtent = GetHorizontalSplitExtentLocal();
        bottomHeight = CadMath.RoundMm((axisWorldY - Y) - half);
        topHeight = CadMath.RoundMm((Y + splitExtent - axisWorldY) - half);
        return bottomHeight > CadMath.Epsilon && topHeight > CadMath.Epsilon;
    }

    public bool TrySetHorizontalLightOpeningBottomHeight(double bottomHeight)
    {
        if (bottomHeight <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        return TrySetHorizontalImpostAxisWorldY(Y + half + bottomHeight);
    }

    public bool TrySetHorizontalLightOpeningTopHeight(double topHeight)
    {
        if (topHeight <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        return TrySetHorizontalImpostAxisWorldY(Y + GetHorizontalSplitExtentLocal() - half - topHeight);
    }

    public bool TrySetUniformHorizontalImpostGapAxisWorldY(int gapIndex, double axisWorldY)
    {
        if (!IsUniformSplit || SplitOrientation != SplitOrientation.Horizontal
            || gapIndex < 0 || gapIndex >= _uniformCells.Count - 1)
        {
            return false;
        }

        var thickness = GetResolvedImpostThicknessMm();
        var half = thickness / 2.0;
        var bottomCell = _uniformCells[gapIndex];
        var topCell = _uniformCells[gapIndex + 1];

        var splitExtent = GetHorizontalSplitExtentLocal();
        var extentTopWorld = Y + splitExtent;
        var minAxis = bottomCell.Y + MinSplitSizeMm + half;
        var maxAxis = extentTopWorld - MinSplitSizeMm - half;
        if (maxAxis < minAxis)
        {
            return false;
        }

        axisWorldY = Math.Clamp(CadMath.RoundMm(axisWorldY), minAxis, maxAxis);
        var newBottomHeight = CadMath.RoundMm(axisWorldY - bottomCell.Y - half);
        bottomCell.Height = newBottomHeight;

        var topEdge = gapIndex >= _uniformCells.Count - 2
            ? extentTopWorld
            : topCell.Y + topCell.Height;
        topCell.Y = CadMath.RoundMm(bottomCell.Y + newBottomHeight + thickness);
        topCell.Height = CadMath.RoundMm(topEdge - topCell.Y);
        if (topCell.Height < MinSplitSizeMm)
        {
            return false;
        }

        var cursorY = topCell.Y + topCell.Height + thickness;
        for (var i = gapIndex + 2; i < _uniformCells.Count; i++)
        {
            var cell = _uniformCells[i];
            cell.Y = CadMath.RoundMm(cursorY);
            cursorY = cell.Y + cell.Height + thickness;
        }

        return true;
    }

    private bool TryComputeBinaryVerticalChildren(
        double centerLocal,
        double thickness,
        out double leftWidth,
        out double childBStartLocal,
        out double rightWidth)
    {
        var half = thickness / 2.0;
        leftWidth = CadMath.RoundMm(centerLocal - half);
        childBStartLocal = CadMath.RoundMm(centerLocal + half);
        rightWidth = CadMath.RoundMm(Width - childBStartLocal);

        return leftWidth >= MinSplitSizeMm && rightWidth >= MinSplitSizeMm;
    }

    private bool TryComputeBinaryHorizontalChildren(
        double centerLocal,
        double thickness,
        double splitExtentLocal,
        out double bottomHeight,
        out double childBStartLocal,
        out double topHeight)
    {
        var half = thickness / 2.0;
        bottomHeight = CadMath.RoundMm(centerLocal - half);
        childBStartLocal = CadMath.RoundMm(centerLocal + half);
        topHeight = CadMath.RoundMm(splitExtentLocal - childBStartLocal);

        return bottomHeight >= MinSplitSizeMm && topHeight >= MinSplitSizeMm;
    }

    public bool CanAccept(WindowToolMode tool, double impostWidthMm = CadSystemThickness.DoubleImpostMm)
    {
        var thickness = ResolveImpostWidthForTool(tool, impostWidthMm);
        return tool switch
        {
            WindowToolMode.FakeVerticalImpost or WindowToolMode.FakeHorizontalImpost
                or WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost
                or WindowToolMode.SlantedImpost =>
                IsLeaf && Width >= MinSplitSizeMm * 2 + thickness && Height >= MinSplitSizeMm * 2,
            WindowToolMode.DoubleVerticalImpost =>
                IsLeaf && Width >= MinSplitSizeMm * 2 + thickness && Height >= MinSplitSizeMm * 2,
            WindowToolMode.DoubleHorizontalImpost =>
                IsLeaf && Height >= MinSplitSizeMm * 2 + thickness && Width >= MinSplitSizeMm * 2,
            WindowToolMode.RealFrame or WindowToolMode.FakeFrame => Parent is null,
            _ => IsLeaf
        };
    }

    /// <summary>
    /// Изменяет внешние габариты коробки, пропорционально пересчитывая дочерние ячейки и импосты.
    /// </summary>
    public bool TryResizeOuter(double newWidth, double newHeight)
    {
        if (newWidth < MinSplitSizeMm * 2 || newHeight < MinSplitSizeMm * 2)
        {
            return false;
        }

        if (Width <= CadMath.Epsilon || Height <= CadMath.Epsilon)
        {
            return false;
        }

        var scaleX = newWidth / Width;
        var scaleY = newHeight / Height;
        var originX = X;
        var originY = Y;

        if (!IsLeaf)
        {
            ScaleSubtreeFromOrigin(originX, originY, scaleX, scaleY);
        }

        Width = CadMath.RoundMm(newWidth);
        Height = CadMath.RoundMm(newHeight);
        if (Parent is null)
        {
            EnsureTopProfile();
        }

        return true;
    }

    /// <summary>Создаёт или синхронизирует верхний профиль коробки с текущими габаритами.</summary>
    public void EnsureTopProfile()
    {
        if (Parent is not null)
        {
            return;
        }

        TopProfile ??= ProfileSegment.CreateTopRail(X, Y, Width, Height);
        TopProfile.SyncTopRailGeometry(X, Y, Width, Height);
    }

    private void ScaleSubtreeFromOrigin(double originX, double originY, double scaleX, double scaleY)
    {
        if (IsUniformSplit)
        {
            foreach (var cell in _uniformCells)
            {
                cell.X = CadMath.RoundMm(originX + (cell.X - originX) * scaleX);
                cell.Y = CadMath.RoundMm(originY + (cell.Y - originY) * scaleY);
                cell.Width = CadMath.RoundMm(cell.Width * scaleX);
                cell.Height = CadMath.RoundMm(cell.Height * scaleY);
                if (!cell.IsLeaf)
                {
                    cell.ScaleSubtreeFromOrigin(originX, originY, scaleX, scaleY);
                }
            }

            return;
        }

        if (ChildA != null)
        {
            ChildA.X = CadMath.RoundMm(originX + (ChildA.X - originX) * scaleX);
            ChildA.Y = CadMath.RoundMm(originY + (ChildA.Y - originY) * scaleY);
            ChildA.Width = CadMath.RoundMm(ChildA.Width * scaleX);
            ChildA.Height = CadMath.RoundMm(ChildA.Height * scaleY);
            if (!ChildA.IsLeaf)
            {
                ChildA.ScaleSubtreeFromOrigin(originX, originY, scaleX, scaleY);
            }
        }

        if (ChildB != null)
        {
            ChildB.X = CadMath.RoundMm(originX + (ChildB.X - originX) * scaleX);
            ChildB.Y = CadMath.RoundMm(originY + (ChildB.Y - originY) * scaleY);
            ChildB.Width = CadMath.RoundMm(ChildB.Width * scaleX);
            ChildB.Height = CadMath.RoundMm(ChildB.Height * scaleY);
            if (!ChildB.IsLeaf)
            {
                ChildB.ScaleSubtreeFromOrigin(originX, originY, scaleX, scaleY);
            }
        }
    }

    public IEnumerable<FrameNode> EnumerateInternalNodes()
    {
        if (IsUniformSplit)
        {
            yield return this;
            foreach (var cell in _uniformCells)
            {
                foreach (var node in cell.EnumerateInternalNodes())
                {
                    yield return node;
                }
            }

            yield break;
        }

        if (!IsLeaf)
        {
            yield return this;
            if (ChildA != null)
            {
                foreach (var node in ChildA.EnumerateInternalNodes())
                {
                    yield return node;
                }
            }

            if (ChildB != null)
            {
                foreach (var node in ChildB.EnumerateInternalNodes())
                {
                    yield return node;
                }
            }
        }
    }

    /// <summary>
    /// Снимает последний импост с узла: дочерние ячейки схлопываются обратно в один лист.
    /// Геометрия родителя (X, Y, Width, Height) сохраняется.
    /// </summary>
    public bool TryRemoveImpostSplit()
    {
        if (IsLeaf)
        {
            return false;
        }

        ChildA = null;
        ChildB = null;
        ClearUniformSplitState();
        IsLeaf = true;
        SplitOrientation = SplitOrientation.None;
        ImpostThicknessMm = 0;
        LineStyle = ImpostLineStyle.Dashed;
        return true;
    }

    /// <summary>Находит самый глубокий внутренний узел (последний импост при отсутствии стека истории).</summary>
    public static FrameNode? FindDeepestInternalNode(FrameNode root)
    {
        FrameNode? deepest = null;
        var maxDepth = -1;
        Walk(root, 0);
        return deepest;

        void Walk(FrameNode node, int depth)
        {
            if (node.IsLeaf)
            {
                return;
            }

            if (depth > maxDepth)
            {
                maxDepth = depth;
                deepest = node;
            }

            if (node.IsUniformSplit)
            {
                foreach (var cell in node.UniformCells)
                {
                    Walk(cell, depth + 1);
                }

                return;
            }

            if (node.ChildA != null)
            {
                Walk(node.ChildA, depth + 1);
            }

            if (node.ChildB != null)
            {
                Walk(node.ChildB, depth + 1);
            }
        }
    }

    private double ClampVerticalSplitCenter(double splitLocalX, double thickness)
    {
        var minCenter = MinSplitSizeMm + thickness / 2;
        var maxCenter = Width - MinSplitSizeMm - thickness / 2;
        if (maxCenter < minCenter)
        {
            return Width / 2;
        }

        return Math.Clamp(splitLocalX, minCenter, maxCenter);
    }

    private double ClampHorizontalSplitCenter(double splitLocalY, double thickness, double splitExtentLocal)
    {
        var minCenter = MinSplitSizeMm + thickness / 2;
        var maxCenter = splitExtentLocal - MinSplitSizeMm - thickness / 2;
        if (maxCenter < minCenter)
        {
            return splitExtentLocal / 2;
        }

        return Math.Clamp(splitLocalY, minCenter, maxCenter);
    }

    private static double ResolveImpostWidth(ImpostLineStyle lineStyle, double impostWidthMm)
    {
        if (impostWidthMm > CadMath.Epsilon)
        {
            return impostWidthMm;
        }

        return lineStyle == ImpostLineStyle.Double
            ? CadSystemThickness.DefaultImpostProfileMm
            : DefaultImpostProfileMm;
    }

    private static double ResolveImpostWidthForTool(WindowToolMode tool, double impostWidthMm) =>
        tool is WindowToolMode.DoubleVerticalImpost or WindowToolMode.DoubleHorizontalImpost
            ? ResolveImpostWidth(ImpostLineStyle.Double, impostWidthMm)
            : ResolveImpostWidth(ImpostLineStyle.Solid, impostWidthMm);

    private FrameNode CreateChild(double x, double y, double width, double height) =>
        new()
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Parent = this,
            IsLeaf = true
        };

    public static FrameNode CreateRoot(double x, double y, double width, double height)
    {
        var root = new FrameNode { X = x, Y = y, Width = width, Height = height };
        root.EnsureTopProfile();
        return root;
    }

    /// <summary>Глубокий снимок узла и всех потомков для undo/redo дерева рамы.</summary>
    public TreeSnapshot CaptureTreeSnapshot()
    {
        if (IsUniformSplit)
        {
            return new TreeSnapshot
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                IsLeaf = IsLeaf,
                IsUniformSplit = IsUniformSplit,
                SplitOrientation = SplitOrientation,
                LineStyle = LineStyle,
                ImpostThicknessMm = ImpostThicknessMm,
                UniformImpostCount = UniformImpostCount,
                UniformCells = UniformCells.Select(cell => cell.CaptureTreeSnapshot()).ToList(),
                TopProfile = Parent is null ? TopProfile?.CaptureSnapshot() : null
            };
        }

        if (!IsLeaf && ChildA is not null && ChildB is not null)
        {
            return new TreeSnapshot
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                IsLeaf = IsLeaf,
                IsUniformSplit = IsUniformSplit,
                SplitOrientation = SplitOrientation,
                LineStyle = LineStyle,
                ImpostThicknessMm = ImpostThicknessMm,
                UniformImpostCount = UniformImpostCount,
                ChildA = ChildA.CaptureTreeSnapshot(),
                ChildB = ChildB.CaptureTreeSnapshot(),
                TopProfile = Parent is null ? TopProfile?.CaptureSnapshot() : null
            };
        }

        return new TreeSnapshot
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            IsLeaf = IsLeaf,
            IsUniformSplit = IsUniformSplit,
            SplitOrientation = SplitOrientation,
            LineStyle = LineStyle,
            ImpostThicknessMm = ImpostThicknessMm,
            UniformImpostCount = UniformImpostCount,
            TopProfile = Parent is null ? TopProfile?.CaptureSnapshot() : null
        };
    }

    /// <summary>Снимок узла рамы для восстановления дерева из сериализованного состояния.</summary>
    public sealed class TreeSnapshot
    {
        public double X { get; init; }
        public double Y { get; init; }
        public double Width { get; init; }
        public double Height { get; init; }
        public bool IsLeaf { get; init; } = true;
        public bool IsUniformSplit { get; init; }
        public SplitOrientation SplitOrientation { get; init; } = SplitOrientation.None;
        public ImpostLineStyle LineStyle { get; init; } = ImpostLineStyle.Dashed;
        public double ImpostThicknessMm { get; init; }
        public int UniformImpostCount { get; init; }
        public IReadOnlyList<TreeSnapshot> UniformCells { get; init; } = [];
        public TreeSnapshot? ChildA { get; init; }
        public TreeSnapshot? ChildB { get; init; }
        public ProfileSegmentSnapshot? TopProfile { get; init; }
    }

    public static FrameNode? RestoreTree(TreeSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        var root = CreateRoot(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        root.ApplyTreeSnapshot(snapshot);
        if (snapshot.TopProfile is not null)
        {
            root.TopProfile ??= ProfileSegment.CreateTopRail(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
            root.TopProfile.ApplySnapshot(snapshot.TopProfile);
        }

        return root;
    }

    private void ApplyTreeSnapshot(TreeSnapshot snapshot)
    {
        X = snapshot.X;
        Y = snapshot.Y;
        Width = snapshot.Width;
        Height = snapshot.Height;
        IsLeaf = snapshot.IsLeaf;
        IsUniformSplit = snapshot.IsUniformSplit;
        SplitOrientation = snapshot.SplitOrientation;
        LineStyle = snapshot.LineStyle;
        ImpostThicknessMm = snapshot.ImpostThicknessMm;
        UniformImpostCount = snapshot.UniformImpostCount;

        _uniformCells.Clear();
        ChildA = null;
        ChildB = null;

        if (snapshot.IsUniformSplit)
        {
            foreach (var cellSnapshot in snapshot.UniformCells)
            {
                var cell = new FrameNode { Parent = this };
                cell.ApplyTreeSnapshot(cellSnapshot);
                _uniformCells.Add(cell);
            }

            IsLeaf = false;
            return;
        }

        if (!snapshot.IsLeaf && snapshot.ChildA is not null && snapshot.ChildB is not null)
        {
            ChildA = new FrameNode { Parent = this };
            ChildA.ApplyTreeSnapshot(snapshot.ChildA);
            ChildB = new FrameNode { Parent = this };
            ChildB.ApplyTreeSnapshot(snapshot.ChildB);
            return;
        }

        ClearUniformSplitState();
        IsLeaf = true;
        SplitOrientation = SplitOrientation.None;
    }

    public void ClearSplits()
    {
        ChildA = null;
        ChildB = null;
        ClearUniformSplitState();
        IsLeaf = true;
        SplitOrientation = SplitOrientation.None;
        ImpostThicknessMm = 0;
        LineStyle = ImpostLineStyle.Dashed;
    }

    private void ClearUniformSplitState()
    {
        _uniformCells.Clear();
        IsUniformSplit = false;
        UniformImpostCount = 0;
    }

    public void ResetGeometry(double x, double y, double width, double height)
    {
        ClearSplits();
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}