using NurSapat.Cad.Core.Geometry;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.Core.Models;

public readonly struct EdgeLightOpeningInsets
{
    public double Left { get; init; }
    public double Right { get; init; }
    public double Bottom { get; init; }
    public double Top { get; init; }
}

public enum SplitOrientation
{
    None,
    Vertical,
    Horizontal
}

/// <summary>Правило расчёта равномерного разбиения проёма.</summary>
public enum UniformSplitDistribution
{
    /// <summary>Оси импостов равноудалены по внешнему габариту контейнера.</summary>
    CenterToCenter,

    /// <summary>Все световые проёмы равны; толщина импостов вычитается из света.</summary>
    ClearOpening
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
    public bool HasChildCells =>
        IsUniformSplit && _uniformCells.Count > 0 || ChildA is not null || ChildB is not null;
    public bool IsTerminalLeaf => IsLeaf && !HasChildCells;
    public bool IsUniformSplit { get; private set; }
    public SplitOrientation SplitOrientation { get; private set; } = SplitOrientation.None;
    public ImpostLineStyle LineStyle { get; private set; } = ImpostLineStyle.Dashed;
    public double ImpostThicknessMm { get; private set; }
    public int UniformImpostCount { get; private set; }

    /// <summary>
    /// Относительная позиция оси бинарного импоста (0..1) вдоль оси разбиения.
    /// 0.5 — деление пополам; пересчитывается при изменении габаритов узла.
    /// </summary>
    public double? SplitRelativeCenter { get; private set; }
    public IReadOnlyList<FrameNode> UniformCells => _uniformCells;
    public FrameNode? ChildA { get; private set; }
    public FrameNode? ChildB { get; private set; }
    public FrameNode? Parent { get; set; }

    /// <summary>Корневой узел фиктивной габаритной коробки — разрешает вложение реальных рам в ячейки.</summary>
    public bool IsDummy { get; set; }

    public bool IsFictitiousContainer
    {
        get => IsDummy;
        set => IsDummy = value;
    }

    /// <summary>Уникальный идентификатор узла (для крайних импостов и сериализации).</summary>
    public Guid NodeId { get; private set; } = Guid.NewGuid();

    public IReadOnlyList<ExtremeImpostSlot> ExtremeImposts => _extremeImposts;

    /// <summary>
    /// Стыки физического деления профилей коробки (рейки). Хранятся только на корне.
    /// Не создают импост в световом проёме.
    /// </summary>
    public IReadOnlyList<FrameProfileRailJoint> ProfileRailJoints =>
        Parent is null ? _profileRailJoints : GetRoot()._profileRailJoints;

    /// <summary>
    /// Внешние углы коробки (только корень). null = совпадает с AABB.
    /// При деформации хранятся все четыре явно.
    /// </summary>
    private CadPoint? _outerBottomLeft;
    private CadPoint? _outerBottomRight;
    private CadPoint? _outerTopLeft;
    private CadPoint? _outerTopRight;

    /// <summary>Верхний профиль коробки (только у корневого узла).</summary>
    public ProfileSegment? TopProfile { get; set; }

    /// <summary>Геометрическая форма коробки (только у корневого узла).</summary>
    public ConstructionShape Shape { get; set; } = ConstructionShape.Rectangle;

    /// <summary>Подоконник у нижнего профиля (только у корневого узла).</summary>
    public SillElement? AttachedSill { get; set; }

    /// <summary>Подоконник установлен на корневой раме.</summary>
    public bool HasSill => AttachedSill is not null;

    /// <summary>Корневой узел конструкции для подоконника и формы.</summary>
    public FrameNode GetConstructionRoot() => GetRoot();

    private readonly List<FrameNode> _uniformCells = [];
    private readonly List<ExtremeImpostSlot> _extremeImposts = [];
    private readonly List<FrameProfileRailJoint> _profileRailJoints = [];

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
        var arcHeight = profile?.SegmentType == SegmentType.Arc &&
                        profile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon
            ? profile.ArcHeight
            : 0;

        if (arcHeight <= CadMath.Epsilon)
        {
            return extent;
        }

        var frameTop = frameRoot.Y + frameRoot.Height;
        if (!TouchesFrameTopEdge(splitNode, frameTop) &&
            splitNode.Y + splitNode.Height < frameTop + arcHeight - CadMath.MmPrecision)
        {
            return extent;
        }

        return extent + arcHeight;
    }

    private double GetHorizontalSplitExtentLocal() =>
        ResolveHorizontalSplitExtentLocal(this, GetRoot());

    private static bool TouchesFrameTopEdge(FrameNode node, double frameTop) =>
        CadMath.NearlyEqual(node.Y + node.Height, frameTop)
        || node.Y + node.Height >= frameTop - CadMath.MmPrecision;

    public bool Contains(double x, double y) =>
        x >= X && x <= X + Width && y >= Y && y <= Y + Height;

    /// <summary>Находит узел по идентификатору в поддереве (включая корень).</summary>
    public FrameNode? FindNodeById(Guid nodeId)
    {
        if (NodeId == nodeId)
        {
            return this;
        }

        if (IsUniformSplit)
        {
            foreach (var cell in _uniformCells)
            {
                var found = cell.FindNodeById(nodeId);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        if (ChildA is not null)
        {
            var found = ChildA.FindNodeById(nodeId);
            if (found is not null)
            {
                return found;
            }
        }

        if (ChildB is not null)
        {
            return ChildB.FindNodeById(nodeId);
        }

        return null;
    }

    private bool IsPointInsideTriangleTopology(double x, double y)
    {
        var root = GetRoot();
        if (root.Shape != ConstructionShape.Triangle)
        {
            return true;
        }

        if (Parent is not null)
        {
            return true;
        }

        return FrameTriangleGeometry.ContainsPointInOuterTriangle(root, x, y);
    }

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

        // Логика для бинарного сплита (ChildA / ChildB) — всегда раньше проверки IsLeaf,
        // чтобы контейнер после импоста не считался конечной ячейкой.
        if (ChildA != null && ChildB != null)
        {
            if (ChildA.Contains(x, y))
            {
                return ChildA.FindLeafAt(x, y);
            }

            if (ChildB.Contains(x, y))
            {
                return ChildB.FindLeafAt(x, y);
            }

            // Если точка зависла ровно на импосте, определяем по полуплоскостям
            if (SplitOrientation == SplitOrientation.Vertical)
            {
                return x <= (ChildA.X + ChildA.Width + ChildB.X) / 2.0
                    ? ChildA.FindLeafAt(x, y)
                    : ChildB.FindLeafAt(x, y);
            }

            if (SplitOrientation == SplitOrientation.Horizontal)
            {
                return y <= (ChildA.Y + ChildA.Height + ChildB.Y) / 2.0
                    ? ChildA.FindLeafAt(x, y)
                    : ChildB.FindLeafAt(x, y);
            }
        }

        if (!IsTerminalLeaf)
        {
            return null;
        }

        if (!IsPointInsideTriangleTopology(x, y))
        {
            return null;
        }

        return this;
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

    public bool TrySplitVerticalHalf(ImpostLineStyle lineStyle = ImpostLineStyle.Dashed, double impostWidthMm = 0)
    {
        var (axisLeft, axisRight) = ResolveVerticalCellAxisBoundsWorld();
        var targetWorldX = axisLeft + (axisRight - axisLeft) / 2.0;
        return TrySplitVerticalAt(
            CadMath.RoundMm(targetWorldX - X),
            lineStyle,
            impostWidthMm);
    }

    public bool TrySplitHorizontalHalf(ImpostLineStyle lineStyle = ImpostLineStyle.Dashed, double impostWidthMm = 0)
    {
        var splitExtent = ResolveHorizontalSplitExtentLocal(this, GetRoot());
        if (!TrySplitHorizontalAt(ResolveInsetAdjustedHorizontalSplitCenterLocal(splitExtent), lineStyle, impostWidthMm))
        {
            return false;
        }

        SplitRelativeCenter = 0.5;
        return true;
    }

    /// <summary>
    /// N-split engine: равномерное деление с физическим объёмом каждого импоста.
    /// По умолчанию размерная цепь строится по осям (Center-to-Center); для
    /// одинакового света укажите <see cref="UniformSplitDistribution.ClearOpening"/>.
    /// </summary>
    public bool TrySplitUniformWithVolume(
        SplitOrientation orientation,
        int impostCount,
        double impostWidthMm,
        double frameProfileInset = 0,
        ImpostLineStyle lineStyle = ImpostLineStyle.Solid,
        UniformSplitDistribution distribution = UniformSplitDistribution.CenterToCenter)
    {
        if (!IsLeaf || impostCount < 1)
        {
            return false;
        }

        var impostWidth = ResolveUniformImpostWidth(impostWidthMm, lineStyle);
        var edgeInsets = ResolveEdgeLightOpeningInsets();

        if (!TryComputeUniformCellSizes(
                orientation,
                impostCount,
                impostWidth,
                frameProfileInset,
                edgeInsets,
                distribution,
                out var innerStart,
                out var cellSizes))
        {
            return false;
        }

        _uniformCells.Clear();
        var used = 0.0;
        for (var i = 0; i <= impostCount; i++)
        {
            var isLast = i == impostCount;
            var cellWidth = cellSizes[i];

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
        BecomingSplitParent();
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
        double frameProfileInset = 0,
        UniformSplitDistribution distribution = UniformSplitDistribution.CenterToCenter)
    {
        if (!IsLeaf || impostCount < 1)
        {
            return false;
        }

        var impostWidth = impostWidthMm <= CadMath.Epsilon && frameProfileInset <= CadMath.Epsilon
            ? 0
            : impostWidthMm > CadMath.Epsilon
                ? impostWidthMm
                : DefaultImpostProfileMm;

        return TryComputeUniformCellSizes(
            orientation,
            impostCount,
            impostWidth,
            frameProfileInset,
            ResolveEdgeLightOpeningInsets(),
            distribution,
            out _,
            out _);
    }

    private bool TryComputeUniformCellSizes(
        SplitOrientation orientation,
        int impostCount,
        double impostWidth,
        double frameProfileInset,
        EdgeLightOpeningInsets edgeInsets,
        UniformSplitDistribution distribution,
        out double innerStart,
        out IReadOnlyList<double> cellSizes)
    {
        if (distribution == UniformSplitDistribution.ClearOpening)
        {
            if (!TryComputeUniformLayout(
                    orientation,
                    impostCount,
                    impostWidth,
                    frameProfileInset,
                    edgeInsets,
                    out innerStart,
                    out var singleGlassSize))
            {
                cellSizes = Array.Empty<double>();
                return false;
            }

            var trailingInset = (Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0)
                + (orientation == SplitOrientation.Vertical ? edgeInsets.Right : edgeInsets.Top);
            var innerSpan = orientation == SplitOrientation.Vertical
                ? Width - innerStart - trailingInset
                : Height - innerStart - trailingInset;
            var sizes = new List<double>(impostCount + 1);
            var used = 0.0;
            for (var i = 0; i <= impostCount; i++)
            {
                var isLast = i == impostCount;
                var size = isLast
                    ? CadMath.RoundMm(innerSpan - used)
                    : CadMath.RoundMm(singleGlassSize);
                sizes.Add(size);
                used += size + (isLast ? 0 : impostWidth);
            }

            cellSizes = sizes;
            return true;
        }

        return TryComputeCenterToCenterCellSizes(
            orientation,
            impostCount,
            impostWidth,
            frameProfileInset,
            edgeInsets,
            out innerStart,
            out cellSizes);
    }

    /// <summary>
    /// Центры импостов лежат на равных долях внешнего габарита: для W=3000 и
    /// трёх секций это строго X=1000 и X=2000. Толщина профиля влияет только
    /// на границы световых ячеек, но никогда не сдвигает размерную ось.
    /// </summary>
    private bool TryComputeCenterToCenterCellSizes(
        SplitOrientation orientation,
        int impostCount,
        double impostWidth,
        double frameProfileInset,
        EdgeLightOpeningInsets edgeInsets,
        out double innerStart,
        out IReadOnlyList<double> cellSizes)
    {
        var leadingFrameInset = Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0;
        var trailingFrameInset = Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0;
        innerStart = leadingFrameInset
            + (orientation == SplitOrientation.Vertical ? edgeInsets.Left : edgeInsets.Bottom);
        var trailingInset = trailingFrameInset
            + (orientation == SplitOrientation.Vertical ? edgeInsets.Right : edgeInsets.Top);
        var totalSpan = orientation == SplitOrientation.Vertical ? Width : Height;
        var innerEnd = totalSpan - trailingInset;
        var sectionCount = impostCount + 1;
        var currentStart = innerStart;
        var sizes = new List<double>(sectionCount);
        var axisStart = 0.0;
        var axisSpan = totalSpan;
        if (orientation == SplitOrientation.Vertical)
        {
            var (axisLeft, axisRight) = ResolveVerticalCellAxisBoundsWorld();
            axisStart = axisLeft - X;
            axisSpan = axisRight - axisLeft;
        }

        for (var section = 1; section < sectionCount; section++)
        {
            // Center-to-center division is based on the logical section
            // boundaries (outer frame edge / neighbouring mullion axes), not
            // on the physical clear body stored in FrameNode.Width.
            var axis = axisStart + axisSpan * section / sectionCount;
            var profileStart = axis - impostWidth / 2.0;
            var size = CadMath.RoundMm(profileStart - currentStart);
            if (size < MinSplitSizeMm)
            {
                cellSizes = Array.Empty<double>();
                return false;
            }

            sizes.Add(size);
            currentStart = profileStart + impostWidth;
        }

        var lastSize = CadMath.RoundMm(innerEnd - currentStart);
        if (lastSize < MinSplitSizeMm)
        {
            cellSizes = Array.Empty<double>();
            return false;
        }

        sizes.Add(lastSize);
        cellSizes = sizes;
        return true;
    }

    private bool TryComputeUniformLayout(
        SplitOrientation orientation,
        int impostCount,
        double impostWidth,
        double frameProfileInset,
        EdgeLightOpeningInsets edgeInsets,
        out double innerStart,
        out double singleGlassSize)
    {
        var leadingFrameInset = Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0;
        var trailingFrameInset = Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0;

        innerStart = leadingFrameInset
            + (orientation == SplitOrientation.Vertical ? edgeInsets.Left : edgeInsets.Bottom);

        var trailingInset = trailingFrameInset
            + (orientation == SplitOrientation.Vertical ? edgeInsets.Right : edgeInsets.Top);

        var innerSpan = orientation switch
        {
            SplitOrientation.Vertical => Width - innerStart - trailingInset,
            SplitOrientation.Horizontal => Height - innerStart - trailingInset,
            _ => 0
        };

        if (innerSpan <= CadMath.Epsilon)
        {
            singleGlassSize = 0;
            return false;
        }

        // Размер ячеек считаем по фактическому световому проёму (innerSpan), а не по внешней коробке.
        return TryComputeUniformLayout(
            orientation,
            orientation == SplitOrientation.Vertical ? innerSpan : Width,
            orientation == SplitOrientation.Horizontal ? innerSpan : Height,
            impostCount,
            impostWidth,
            0,
            out singleGlassSize);
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

        double impostWidth;
        if (impostWidthMm <= CadMath.Epsilon && frameProfileInset <= CadMath.Epsilon)
        {
            impostWidth = 0;
        }
        else if (impostWidthMm > CadMath.Epsilon)
        {
            impostWidth = impostWidthMm;
        }
        else
        {
            impostWidth = DefaultImpostProfileMm;
        }

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
        out double singleGlassSize) =>
        TryComputeUniformLayout(
            orientation,
            impostCount,
            impostWidth,
            frameProfileInset,
            ResolveEdgeLightOpeningInsets(),
            out innerStart,
            out singleGlassSize);

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
        BecomingSplitParent();
        ClearUniformSplitState();
        ImpostThicknessMm = thickness;
        IsLeaf = false;
        SplitOrientation = SplitOrientation.Vertical;
        LineStyle = lineStyle;
        SplitRelativeCenter = Width > CadMath.Epsilon ? centerLocal / Width : 0.5;
        return true;
    }

    public bool TrySplitHorizontalAt(double splitLocalY, ImpostLineStyle lineStyle, double impostWidthMm)
    {
        if (!IsLeaf)
        {
            return false;
        }

        var thickness = ResolveImpostWidth(lineStyle, impostWidthMm);
        var splitExtent = ResolveHorizontalSplitExtentLocal(this, GetRoot());
        var centerLocal = ClampHorizontalSplitCenter(splitLocalY, thickness, splitExtent);
        if (!TryComputeBinaryHorizontalChildren(centerLocal, thickness, splitExtent, out var bottomHeight, out var childBStartLocal, out var topHeight))
        {
            return false;
        }

        ChildA = CreateChild(X, Y, Width, bottomHeight);
        ChildB = CreateChild(X, Y + childBStartLocal, Width, topHeight);
        BecomingSplitParent();
        ClearUniformSplitState();
        ImpostThicknessMm = thickness;
        IsLeaf = false;
        SplitOrientation = SplitOrientation.Horizontal;
        LineStyle = lineStyle;
        SplitRelativeCenter = splitExtent > CadMath.Epsilon ? centerLocal / splitExtent : 0.5;
        return true;
    }

    /// <summary>
    /// Проверяет возможность вертикального деления в локальной координате X без изменения дерева.
    /// </summary>
    public bool CanSplitVerticalAt(double splitLocalX, ImpostLineStyle lineStyle, double impostWidthMm)
    {
        if (!IsLeaf)
        {
            return false;
        }

        var thickness = ResolveImpostWidth(lineStyle, impostWidthMm);
        var centerLocal = ClampVerticalSplitCenter(splitLocalX, thickness);
        return TryComputeBinaryVerticalChildren(centerLocal, thickness, out _, out _, out _);
    }

    /// <summary>
    /// Проверяет возможность горизонтального деления в локальной координате Y без изменения дерева.
    /// </summary>
    public bool CanSplitHorizontalAt(double splitLocalY, ImpostLineStyle lineStyle, double impostWidthMm)
    {
        if (!IsLeaf)
        {
            return false;
        }

        var thickness = ResolveImpostWidth(lineStyle, impostWidthMm);
        var splitExtent = ResolveHorizontalSplitExtentLocal(this, GetRoot());
        var centerLocal = ClampHorizontalSplitCenter(splitLocalY, thickness, splitExtent);
        return TryComputeBinaryHorizontalChildren(centerLocal, thickness, splitExtent, out _, out _, out _);
    }

    /// <summary>
    /// Толщина импоста узла (мм).
    /// </summary>
    public double GetResolvedImpostThicknessMm() =>
        ImpostThicknessMm > CadMath.Epsilon
            ? ImpostThicknessMm
            : LineStyle == ImpostLineStyle.Dashed
                ? 0
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

        var preservedHorizontalAxes = CaptureHorizontalImpostAxes();

        ChildA.X = X;
        ChildA.Y = Y;
        ChildA.Width = leftWidth;
        ChildA.Height = Height;

        ChildB.X = X + childBStartLocal;
        ChildB.Y = Y;
        ChildB.Width = rightWidth;
        ChildB.Height = Height;
        SplitRelativeCenter = Width > CadMath.Epsilon ? centerLocal / Width : 0.5;
        ChildA.RelayoutSubdivisionTree(0);
        ChildB.RelayoutSubdivisionTree(0);
        RestoreHorizontalImpostAxes(preservedHorizontalAxes);
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
        SplitRelativeCenter = splitExtent > CadMath.Epsilon ? centerLocal / splitExtent : 0.5;
        ChildA.RelayoutSubdivisionTree(0);
        ChildB.RelayoutSubdivisionTree(0);
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
        var edgeInsets = ResolveEdgeLightOpeningInsets();
        var innerLeft = X + edgeInsets.Left;
        var innerRight = X + Width - edgeInsets.Right;
        leftWidth = CadMath.RoundMm((axisWorldX - half) - innerLeft);
        rightWidth = CadMath.RoundMm(innerRight - (axisWorldX + half));
        return leftWidth > CadMath.Epsilon && rightWidth > CadMath.Epsilon;
    }

    public bool TrySetVerticalLightOpeningLeftWidth(double leftWidth)
    {
        if (leftWidth <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        var innerLeft = X + ResolveEdgeLightOpeningInsets().Left;
        return TrySetVerticalImpostAxisWorldX(innerLeft + half + leftWidth);
    }

    public bool TrySetVerticalLightOpeningRightWidth(double rightWidth)
    {
        if (rightWidth <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        var innerRight = X + Width - ResolveEdgeLightOpeningInsets().Right;
        return TrySetVerticalImpostAxisWorldX(innerRight - half - rightWidth);
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

        var preservedHorizontalAxes = CaptureHorizontalImpostAxes();
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

        foreach (var cell in _uniformCells)
        {
            cell.RelayoutSubdivisionTree(0);
        }

        RestoreHorizontalImpostAxes(preservedHorizontalAxes);
        return true;
    }

    /// <summary>
    /// Horizontal impost axes are absolute world-Y coordinates. An X-only
    /// relayout may change segment widths, but must never redistribute their
    /// rows or return a manually shifted uniform split to its default center.
    /// </summary>
    private List<HorizontalImpostAxisSnapshot> CaptureHorizontalImpostAxes()
    {
        var snapshots = new List<HorizontalImpostAxisSnapshot>();
        foreach (var node in EnumerateInternalNodes())
        {
            if (node.SplitOrientation != SplitOrientation.Horizontal)
            {
                continue;
            }

            if (node.IsUniformSplit)
            {
                for (var gapIndex = 0; gapIndex < node._uniformCells.Count - 1; gapIndex++)
                {
                    if (node.TryGetUniformImpostAxisWorld(
                            SplitOrientation.Horizontal,
                            gapIndex,
                            out var axisWorldY))
                    {
                        snapshots.Add(new HorizontalImpostAxisSnapshot(
                            node,
                            gapIndex,
                            axisWorldY));
                    }
                }

                continue;
            }

            if (node.TryGetHorizontalImpostAxisWorldY(out var binaryAxisWorldY))
            {
                snapshots.Add(new HorizontalImpostAxisSnapshot(
                    node,
                    UniformGapIndex: null,
                    binaryAxisWorldY));
            }
        }

        return snapshots;
    }

    private static void RestoreHorizontalImpostAxes(
        IReadOnlyList<HorizontalImpostAxisSnapshot> snapshots)
    {
        // EnumerateInternalNodes returns parents before descendants. Restoring
        // in the same order ensures a parent relayout cannot overwrite a
        // descendant axis after that descendant has already been restored.
        foreach (var snapshot in snapshots)
        {
            if (snapshot.UniformGapIndex is int gapIndex)
            {
                snapshot.Node.TrySetUniformHorizontalImpostGapAxisWorldY(
                    gapIndex,
                    snapshot.AxisWorldY);
            }
            else
            {
                snapshot.Node.TrySetHorizontalImpostAxisWorldY(snapshot.AxisWorldY);
            }
        }
    }

    private sealed record HorizontalImpostAxisSnapshot(
        FrameNode Node,
        int? UniformGapIndex,
        double AxisWorldY);

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
        var edgeInsets = ResolveEdgeLightOpeningInsets();
        var innerBottom = Y + edgeInsets.Bottom;
        var innerTop = Y + splitExtent - edgeInsets.Top;
        bottomHeight = CadMath.RoundMm((axisWorldY - half) - innerBottom);
        topHeight = CadMath.RoundMm(innerTop - (axisWorldY + half));
        return bottomHeight > CadMath.Epsilon && topHeight > CadMath.Epsilon;
    }

    public bool TrySetHorizontalLightOpeningBottomHeight(double bottomHeight)
    {
        if (bottomHeight <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        var innerBottom = Y + ResolveEdgeLightOpeningInsets().Bottom;
        return TrySetHorizontalImpostAxisWorldY(innerBottom + half + bottomHeight);
    }

    public bool TrySetHorizontalLightOpeningTopHeight(double topHeight)
    {
        if (topHeight <= CadMath.Epsilon)
        {
            return false;
        }

        var half = GetResolvedImpostThicknessMm() / 2.0;
        var innerTop = Y + GetHorizontalSplitExtentLocal() - ResolveEdgeLightOpeningInsets().Top;
        return TrySetHorizontalImpostAxisWorldY(innerTop - half - topHeight);
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

        foreach (var cell in _uniformCells)
        {
            cell.RelayoutSubdivisionTree(0);
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
            WindowToolMode.RealFrame => CanAcceptNestedRealFrame(),
            WindowToolMode.AdapterProfile => CanAcceptNestedRealFrame(),
            WindowToolMode.FakeFrame => Parent is null,
            _ => IsLeaf
        };
    }

    private bool CanAcceptNestedRealFrame()
    {
        if (!IsLeaf)
        {
            return false;
        }

        if (GetRoot().IsFictitiousContainer)
        {
            return true;
        }

        return Parent is null;
    }

    /// <summary>
    /// Изменяет внешние габариты коробки и пересчитывает дочерние ячейки и оси импостов.
    /// </summary>
    public bool TryResizeOuter(double newWidth, double newHeight, double frameProfileInset = 0)
    {
        if (newWidth < MinSplitSizeMm * 2 || newHeight < MinSplitSizeMm * 2)
        {
            return false;
        }

        if (Width <= CadMath.Epsilon || Height <= CadMath.Epsilon)
        {
            return false;
        }

        var preserveContour = Parent is null
                              && (HasStoredOuterCorners()
                                  || _profileRailJoints.Any(joint => joint.HasFixedAnchor));
        var oldWidth = Width;
        var oldHeight = Height;
        var oldCorners = preserveContour ? GetOuterCorners() : default;
        var currentContour = preserveContour ? GetOuterContourPoints() : [];
        var currentOverallWidth = preserveContour
            ? currentContour.Max(point => point.X) - currentContour.Min(point => point.X)
            : oldWidth;
        var currentOverallHeight = preserveContour
            ? currentContour.Max(point => point.Y) - currentContour.Min(point => point.Y)
            : oldHeight;
        var widthScale = currentOverallWidth > CadMath.Epsilon
            ? newWidth / currentOverallWidth
            : 1.0;
        var heightDelta = newHeight - currentOverallHeight;

        Width = CadMath.RoundMm(preserveContour
            ? oldWidth + (newWidth - currentOverallWidth)
            : newWidth);
        Height = CadMath.RoundMm(preserveContour
            ? oldHeight + heightDelta
            : newHeight);

        if (preserveContour)
        {
            CadPoint ScaleX(CadPoint point) => new(
                CadMath.RoundMm(X + (point.X - X) * widthScale),
                point.Y);

            var bottomLeft = ScaleX(oldCorners.BottomLeft);
            var bottomRight = ScaleX(oldCorners.BottomRight);
            var topLeft = ScaleX(oldCorners.TopLeft);
            var topRight = ScaleX(oldCorners.TopRight);
            _outerBottomLeft = bottomLeft;
            _outerBottomRight = bottomRight;
            _outerTopLeft = new CadPoint(topLeft.X, CadMath.RoundMm(topLeft.Y + heightDelta));
            _outerTopRight = new CadPoint(topRight.X, CadMath.RoundMm(topRight.Y + heightDelta));

            foreach (var joint in _profileRailJoints.Where(joint => joint.HasFixedAnchor))
            {
                joint.AnchorLocalX = CadMath.RoundMm(joint.AnchorLocalX * widthScale);
                if (joint.Rail == FrameProfileRail.Top)
                {
                    joint.AnchorLocalY = CadMath.RoundMm(joint.AnchorLocalY + heightDelta);
                }
            }
        }
        else if (Parent is null)
        {
            ClearOuterCorners();
        }

        if (!IsLeaf)
        {
            RelayoutSubdivisionTree(frameProfileInset);
        }

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

    private void RelayoutSubdivisionTree(double frameProfileInset)
    {
        if (IsLeaf)
        {
            return;
        }

        RelayoutHorizontalSubdivisions(frameProfileInset);
        RelayoutVerticalSubdivisions(frameProfileInset);
    }

    private void RelayoutHorizontalSubdivisions(double frameProfileInset)
    {
        if (IsLeaf)
        {
            return;
        }

        var rootInset = Parent is null ? frameProfileInset : 0;
        if (IsUniformSplit && SplitOrientation == SplitOrientation.Horizontal)
        {
            RelayoutUniformCells(rootInset);
        }
        else if (!IsUniformSplit && SplitOrientation == SplitOrientation.Horizontal)
        {
            RelayoutBinaryHorizontalSplit();
        }

        if (IsUniformSplit)
        {
            foreach (var cell in _uniformCells)
            {
                cell.RelayoutHorizontalSubdivisions(0);
            }

            return;
        }

        ChildA?.RelayoutHorizontalSubdivisions(0);
        ChildB?.RelayoutHorizontalSubdivisions(0);
    }

    private void RelayoutVerticalSubdivisions(double frameProfileInset)
    {
        if (IsLeaf)
        {
            return;
        }

        var rootInset = Parent is null ? frameProfileInset : 0;
        if (IsUniformSplit && SplitOrientation == SplitOrientation.Vertical)
        {
            RelayoutUniformCells(rootInset);
        }
        else if (!IsUniformSplit && SplitOrientation == SplitOrientation.Vertical)
        {
            RelayoutBinaryVerticalSplit();
        }

        if (IsUniformSplit)
        {
            foreach (var cell in _uniformCells)
            {
                cell.RelayoutVerticalSubdivisions(0);
            }

            return;
        }

        ChildA?.RelayoutVerticalSubdivisions(0);
        ChildB?.RelayoutVerticalSubdivisions(0);
    }

    /// <summary>
    /// Восстанавливает равномерный шаг импостов uniform-разбиения без изменения топологии.
    /// </summary>
    public bool TryRealignUniformImposts(double frameProfileInset = 0)
    {
        if (!IsUniformSplit || UniformImpostCount < 1 || _uniformCells.Count < 2)
        {
            return false;
        }

        RelayoutUniformCells(frameProfileInset);
        foreach (var cell in _uniformCells)
        {
            cell.RelayoutSubdivisionTree(0);
        }

        return true;
    }

    private void RelayoutUniformCells(double frameProfileInset)
    {
        if (!IsUniformSplit || UniformImpostCount < 1)
        {
            return;
        }

        var impostWidth = GetResolvedImpostThicknessMm();
        if (!TryComputeUniformLayout(
                SplitOrientation,
                UniformImpostCount,
                impostWidth,
                frameProfileInset,
                out var innerStart,
                out var singleGlassSize))
        {
            return;
        }

        var trailingInset = Parent == null && frameProfileInset > CadMath.Epsilon ? frameProfileInset : 0;
        var innerSpan = SplitOrientation switch
        {
            SplitOrientation.Vertical => Width - innerStart - trailingInset,
            SplitOrientation.Horizontal => Height - innerStart - trailingInset,
            _ => 0
        };

        var used = 0.0;
        for (var i = 0; i <= UniformImpostCount; i++)
        {
            if (i >= _uniformCells.Count)
            {
                break;
            }

            var isLast = i == UniformImpostCount;
            var cellWidth = isLast
                ? CadMath.RoundMm(innerSpan - used)
                : CadMath.RoundMm(singleGlassSize);

            var offset = innerStart + used;
            var cell = _uniformCells[i];
            if (SplitOrientation == SplitOrientation.Vertical)
            {
                cell.X = CadMath.RoundMm(X + offset);
                cell.Y = Y;
                cell.Width = cellWidth;
                cell.Height = Height;
            }
            else
            {
                cell.X = X;
                cell.Y = CadMath.RoundMm(Y + offset);
                cell.Width = Width;
                cell.Height = cellWidth;
            }

            used += cellWidth + (isLast ? 0 : impostWidth);
        }
    }

    private void RelayoutBinaryVerticalSplit()
    {
        if (ChildA is null || ChildB is null)
        {
            return;
        }

        var thickness = GetResolvedImpostThicknessMm();
        var relative = ResolveBinarySplitRelativeCenter();
        var centerLocal = ClampVerticalSplitCenter(relative * Width, thickness);
        if (!TryComputeBinaryVerticalChildren(centerLocal, thickness, out var leftWidth, out var childBStartLocal, out var rightWidth))
        {
            return;
        }

        ChildA.X = X;
        ChildA.Y = Y;
        ChildA.Width = leftWidth;
        ChildA.Height = Height;

        ChildB.X = CadMath.RoundMm(X + childBStartLocal);
        ChildB.Y = Y;
        ChildB.Width = rightWidth;
        ChildB.Height = Height;
    }

    private void RelayoutBinaryHorizontalSplit()
    {
        if (ChildA is null || ChildB is null)
        {
            return;
        }

        var thickness = GetResolvedImpostThicknessMm();
        var splitExtent = GetHorizontalSplitExtentLocal();
        var relative = ResolveBinarySplitRelativeCenter();
        var centerLocal = ClampHorizontalSplitCenter(relative * splitExtent, thickness, splitExtent);
        if (!TryComputeBinaryHorizontalChildren(centerLocal, thickness, splitExtent, out var bottomHeight, out var childBStartLocal, out var topHeight))
        {
            return;
        }

        ChildA.X = X;
        ChildA.Y = Y;
        ChildA.Width = Width;
        ChildA.Height = bottomHeight;

        ChildB.X = X;
        ChildB.Y = CadMath.RoundMm(Y + childBStartLocal);
        ChildB.Width = Width;
        ChildB.Height = topHeight;
    }

    private double ResolveBinarySplitRelativeCenter()
    {
        const double centerTolerance = 0.02;

        if (SplitRelativeCenter is { } stored)
        {
            if (CadMath.NearlyEqual(stored, 0.5, centerTolerance))
            {
                return 0.5;
            }

            return Math.Clamp(stored, 0, 1);
        }

        if (SplitOrientation == SplitOrientation.Vertical)
        {
            if (TryGetVerticalLightOpeningWidths(out var left, out var right)
                && CadMath.NearlyEqual(left, right, 1.0))
            {
                return 0.5;
            }

            if (ChildA is null || Width <= CadMath.Epsilon)
            {
                return 0.5;
            }

            var half = GetResolvedImpostThicknessMm() / 2.0;
            return Math.Clamp((ChildA.Width + half) / Width, 0, 1);
        }

        if (SplitOrientation == SplitOrientation.Horizontal)
        {
            if (TryGetHorizontalLightOpeningHeights(out var bottom, out var top)
                && CadMath.NearlyEqual(bottom, top, 1.0))
            {
                return 0.5;
            }

            var extent = GetHorizontalSplitExtentLocal();
            if (ChildA is null || extent <= CadMath.Epsilon)
            {
                return 0.5;
            }

            var half = GetResolvedImpostThicknessMm() / 2.0;
            return Math.Clamp((ChildA.Height + half) / extent, 0, 1);
        }

        return 0.5;
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
        var edge = ResolveEdgeLightOpeningInsets();
        var minCenter = edge.Left + MinSplitSizeMm + thickness / 2;
        var maxCenter = Width - edge.Right - MinSplitSizeMm - thickness / 2;
        if (maxCenter < minCenter)
        {
            return ResolveInsetAdjustedVerticalSplitCenterLocal();
        }

        return Math.Clamp(splitLocalX, minCenter, maxCenter);
    }

    private double ClampHorizontalSplitCenter(double splitLocalY, double thickness, double splitExtentLocal)
    {
        var edge = ResolveEdgeLightOpeningInsets();
        var minCenter = edge.Bottom + MinSplitSizeMm + thickness / 2;
        var maxCenter = splitExtentLocal - edge.Top - MinSplitSizeMm - thickness / 2;
        if (maxCenter < minCenter)
        {
            return ResolveInsetAdjustedHorizontalSplitCenterLocal(splitExtentLocal);
        }

        return Math.Clamp(splitLocalY, minCenter, maxCenter);
    }

    private static double ResolveImpostWidth(ImpostLineStyle lineStyle, double impostWidthMm)
    {
        if (impostWidthMm > CadMath.Epsilon)
        {
            return impostWidthMm;
        }

        if (lineStyle == ImpostLineStyle.Dashed)
        {
            return 0;
        }

        return lineStyle == ImpostLineStyle.Double
            ? CadSystemThickness.DefaultImpostProfileMm
            : DefaultImpostProfileMm;
    }

    private static double ResolveUniformImpostWidth(double impostWidthMm, ImpostLineStyle lineStyle) =>
        impostWidthMm > CadMath.Epsilon
            ? impostWidthMm
            : lineStyle == ImpostLineStyle.Dashed
                ? 0
                : DefaultImpostProfileMm;

    private static double ResolveImpostWidthForTool(WindowToolMode tool, double impostWidthMm) =>
        tool is WindowToolMode.DoubleVerticalImpost
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

    public static FrameNode CreateRoot(double x, double y, double width, double height, bool isFictitiousContainer = false)
    {
        var root = new FrameNode
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            IsFictitiousContainer = isFictitiousContainer,
            Shape = ConstructionShape.Rectangle,
            AttachedSill = null
        };

        if (!isFictitiousContainer)
        {
            root.EnsureTopProfile();
        }

        return root;
    }

    /// <summary>Сдвигает узел и всех потомков на заданное смещение (мм).</summary>
    public void TranslateSubtree(double deltaX, double deltaY)
    {
        if (Math.Abs(deltaX) <= CadMath.Epsilon && Math.Abs(deltaY) <= CadMath.Epsilon)
        {
            return;
        }

        X = CadMath.RoundMm(X + deltaX);
        Y = CadMath.RoundMm(Y + deltaY);
        TranslateOuterCorners(deltaX, deltaY);

        if (IsUniformSplit)
        {
            foreach (var cell in _uniformCells)
            {
                cell.TranslateSubtree(deltaX, deltaY);
            }

            return;
        }

        if (!IsLeaf)
        {
            ChildA?.TranslateSubtree(deltaX, deltaY);
            ChildB?.TranslateSubtree(deltaX, deltaY);
        }
    }

    private void TranslateOuterCorners(double deltaX, double deltaY)
    {
        if (Parent is not null)
        {
            return;
        }

        if (_outerBottomLeft is { } bl)
        {
            _outerBottomLeft = new CadPoint(CadMath.RoundMm(bl.X + deltaX), CadMath.RoundMm(bl.Y + deltaY));
        }

        if (_outerBottomRight is { } br)
        {
            _outerBottomRight = new CadPoint(CadMath.RoundMm(br.X + deltaX), CadMath.RoundMm(br.Y + deltaY));
        }

        if (_outerTopLeft is { } tl)
        {
            _outerTopLeft = new CadPoint(CadMath.RoundMm(tl.X + deltaX), CadMath.RoundMm(tl.Y + deltaY));
        }

        if (_outerTopRight is { } tr)
        {
            _outerTopRight = new CadPoint(CadMath.RoundMm(tr.X + deltaX), CadMath.RoundMm(tr.Y + deltaY));
        }
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
                NodeId = NodeId,
                IsDummy = IsDummy,
                ExtremeImposts = CaptureExtremeImpostSnapshots(),
                TopProfile = Parent is null ? TopProfile?.CaptureSnapshot() : null,
                Shape = Parent is null ? Shape : ConstructionShape.Rectangle,
                Sill = Parent is null ? CaptureSillSnapshot() : null,
                ProfileRailJoints = Parent is null ? CaptureProfileRailJointSnapshots() : [],
                OuterCorners = Parent is null ? CaptureOuterCornerSnapshot() : null
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
                SplitRelativeCenter = SplitRelativeCenter,
                NodeId = NodeId,
                IsDummy = IsDummy,
                ExtremeImposts = CaptureExtremeImpostSnapshots(),
                TopProfile = Parent is null ? TopProfile?.CaptureSnapshot() : null,
                Shape = Parent is null ? Shape : ConstructionShape.Rectangle,
                Sill = Parent is null ? CaptureSillSnapshot() : null,
                ProfileRailJoints = Parent is null ? CaptureProfileRailJointSnapshots() : [],
                OuterCorners = Parent is null ? CaptureOuterCornerSnapshot() : null
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
            NodeId = NodeId,
            IsDummy = IsDummy,
            ExtremeImposts = CaptureExtremeImpostSnapshots(),
            TopProfile = Parent is null ? TopProfile?.CaptureSnapshot() : null,
            Shape = Parent is null ? Shape : ConstructionShape.Rectangle,
            Sill = Parent is null ? CaptureSillSnapshot() : null,
            ProfileRailJoints = Parent is null ? CaptureProfileRailJointSnapshots() : [],
            OuterCorners = Parent is null ? CaptureOuterCornerSnapshot() : null
        };
    }

    private SillElementSnapshot? CaptureSillSnapshot() =>
        AttachedSill is null
            ? null
            : new SillElementSnapshot
            {
                Id = AttachedSill.Id,
                WidthMm = AttachedSill.WidthMm,
                DepthMm = AttachedSill.DepthMm,
                ThicknessMm = AttachedSill.ThicknessMm,
                LeftOverhangMm = AttachedSill.LeftOverhangMm,
                RightOverhangMm = AttachedSill.RightOverhangMm,
                MaterialId = AttachedSill.MaterialId,
                Color = AttachedSill.Color
            };

    private IReadOnlyList<ExtremeImpostSlotSnapshot> CaptureExtremeImpostSnapshots() =>
        _extremeImposts.Select(slot => new ExtremeImpostSlotSnapshot
        {
            Id = slot.Id,
            Orientation = slot.Orientation,
            EdgeAlignment = slot.EdgeAlignment,
            OffsetMm = slot.OffsetMm,
            ThicknessMm = slot.ThicknessMm,
            SpanStartMm = slot.SpanStartMm,
            SpanEndMm = slot.SpanEndMm,
            LightOpeningInsetMm = slot.LightOpeningInsetMm
        }).ToList();

    private IReadOnlyList<FrameProfileRailJointSnapshot> CaptureProfileRailJointSnapshots() =>
        _profileRailJoints.Select(joint => new FrameProfileRailJointSnapshot
        {
            Id = joint.Id,
            Rail = joint.Rail,
            RelativeT = joint.RelativeT,
            NormalOffsetMm = joint.NormalOffsetMm,
            LockHorizontalAxis = joint.LockHorizontalAxis,
            HasFixedAnchor = joint.HasFixedAnchor,
            AnchorLocalX = joint.AnchorLocalX,
            AnchorLocalY = joint.AnchorLocalY,
            Kind = joint.Kind
        }).ToList();

    private OuterCornerSnapshot? CaptureOuterCornerSnapshot()
    {
        if (!HasStoredOuterCorners())
        {
            return null;
        }

        var corners = GetOuterCorners();
        return new OuterCornerSnapshot
        {
            BottomLeftX = corners.BottomLeft.X,
            BottomLeftY = corners.BottomLeft.Y,
            BottomRightX = corners.BottomRight.X,
            BottomRightY = corners.BottomRight.Y,
            TopLeftX = corners.TopLeft.X,
            TopLeftY = corners.TopLeft.Y,
            TopRightX = corners.TopRight.X,
            TopRightY = corners.TopRight.Y
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
        public double? SplitRelativeCenter { get; init; }
        public bool IsDummy { get; init; }
        public Guid NodeId { get; init; }
        public IReadOnlyList<ExtremeImpostSlotSnapshot> ExtremeImposts { get; init; } = [];
        public ProfileSegmentSnapshot? TopProfile { get; init; }
        public ConstructionShape Shape { get; init; } = ConstructionShape.Rectangle;
        public SillElementSnapshot? Sill { get; init; }
        public IReadOnlyList<FrameProfileRailJointSnapshot> ProfileRailJoints { get; init; } = [];
        public OuterCornerSnapshot? OuterCorners { get; init; }
    }

    public sealed class OuterCornerSnapshot
    {
        public double BottomLeftX { get; init; }
        public double BottomLeftY { get; init; }
        public double BottomRightX { get; init; }
        public double BottomRightY { get; init; }
        public double TopLeftX { get; init; }
        public double TopLeftY { get; init; }
        public double TopRightX { get; init; }
        public double TopRightY { get; init; }
    }

    public sealed class SillElementSnapshot
    {
        public Guid Id { get; init; }
        public double WidthMm { get; init; }
        public double DepthMm { get; init; }
        public double ThicknessMm { get; init; }
        public double LeftOverhangMm { get; init; } = SillElement.DefaultSideOverhangMm;
        public double RightOverhangMm { get; init; } = SillElement.DefaultSideOverhangMm;
        public Guid? MaterialId { get; init; }
        public string? Color { get; init; }
    }

    public sealed class ExtremeImpostSlotSnapshot
    {
        public Guid Id { get; init; }
        public SplitOrientation Orientation { get; init; }
        public ImpostEdgeAlignment EdgeAlignment { get; init; }
        public double OffsetMm { get; init; }
        public double ThicknessMm { get; init; }
        public double SpanStartMm { get; init; }
        public double SpanEndMm { get; init; }
        public double LightOpeningInsetMm { get; init; }
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

        root.Shape = snapshot.Shape;
        root.RestoreProfileRailJoints(snapshot.ProfileRailJoints);
        root.RestoreOuterCorners(snapshot.OuterCorners);
        if (snapshot.Sill is { } sillSnapshot)
        {
            root.AttachedSill = new SillElement
            {
                Id = sillSnapshot.Id,
                WidthMm = sillSnapshot.WidthMm,
                DepthMm = sillSnapshot.DepthMm,
                ThicknessMm = sillSnapshot.ThicknessMm,
                LeftOverhangMm = sillSnapshot.LeftOverhangMm,
                RightOverhangMm = sillSnapshot.RightOverhangMm,
                MaterialId = sillSnapshot.MaterialId,
                Color = sillSnapshot.Color ?? SillElement.DefaultColorHex
            };
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
        SplitRelativeCenter = snapshot.SplitRelativeCenter;
        IsDummy = snapshot.IsDummy;
        if (snapshot.NodeId != Guid.Empty)
        {
            NodeId = snapshot.NodeId;
        }

        RestoreExtremeImposts(snapshot.ExtremeImposts);

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

    private void RestoreExtremeImposts(IReadOnlyList<ExtremeImpostSlotSnapshot> snapshots)
    {
        _extremeImposts.Clear();
        foreach (var snapshot in snapshots)
        {
            _extremeImposts.Add(new ExtremeImpostSlot
            {
                Id = snapshot.Id,
                Orientation = snapshot.Orientation,
                EdgeAlignment = snapshot.EdgeAlignment,
                OffsetMm = snapshot.OffsetMm,
                ThicknessMm = snapshot.ThicknessMm,
                SpanStartMm = snapshot.SpanStartMm,
                SpanEndMm = snapshot.SpanEndMm,
                LightOpeningInsetMm = snapshot.LightOpeningInsetMm
            });
        }
    }

    private void RestoreProfileRailJoints(IReadOnlyList<FrameProfileRailJointSnapshot> snapshots)
    {
        _profileRailJoints.Clear();
        foreach (var snapshot in snapshots)
        {
            var lockHorizontal = snapshot.LockHorizontalAxis
                || (snapshot.Rail == FrameProfileRail.Top
                    && Math.Abs(snapshot.RelativeT - 0.5) <= CadMath.Epsilon);
            var hasAnchor = snapshot.HasFixedAnchor || lockHorizontal;
            _profileRailJoints.Add(new FrameProfileRailJoint
            {
                Id = snapshot.Id == Guid.Empty ? Guid.NewGuid() : snapshot.Id,
                Rail = snapshot.Rail,
                RelativeT = snapshot.RelativeT,
                NormalOffsetMm = snapshot.NormalOffsetMm,
                LockHorizontalAxis = lockHorizontal,
                HasFixedAnchor = hasAnchor,
                AnchorLocalX = snapshot.HasFixedAnchor
                    ? snapshot.AnchorLocalX
                    : Width * snapshot.RelativeT,
                AnchorLocalY = snapshot.HasFixedAnchor
                    ? snapshot.AnchorLocalY
                    : Height,
                Kind = snapshot.Kind
            });
        }
    }

    private void RestoreOuterCorners(OuterCornerSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            ClearOuterCorners();
            return;
        }

        _outerBottomLeft = new CadPoint(snapshot.BottomLeftX, snapshot.BottomLeftY);
        _outerBottomRight = new CadPoint(snapshot.BottomRightX, snapshot.BottomRightY);
        _outerTopLeft = new CadPoint(snapshot.TopLeftX, snapshot.TopLeftY);
        _outerTopRight = new CadPoint(snapshot.TopRightX, snapshot.TopRightY);
    }

    public bool HasStoredOuterCorners() =>
        Parent is null
        && (_outerBottomLeft is not null
            || _outerBottomRight is not null
            || _outerTopLeft is not null
            || _outerTopRight is not null);

    public bool HasDeformedOuterContour()
    {
        var root = GetRoot();
        if (!root.HasStoredOuterCorners())
        {
            return false;
        }

        var aabb = FrameProfileRailGeometry.ResolveAabbCorners(root.X, root.Y, root.Width, root.Height);
        var corners = root.GetOuterCorners();
        const double tol = CadMath.MmPrecision;
        return !corners.BottomLeft.IsNear(aabb.BottomLeft, tol)
               || !corners.BottomRight.IsNear(aabb.BottomRight, tol)
               || !corners.TopLeft.IsNear(aabb.TopLeft, tol)
               || !corners.TopRight.IsNear(aabb.TopRight, tol);
    }

    public FrameProfileRailGeometry.OuterCorners GetOuterCorners()
    {
        var root = GetRoot();
        var aabb = FrameProfileRailGeometry.ResolveAabbCorners(root.X, root.Y, root.Width, root.Height);
        if (!root.HasStoredOuterCorners())
        {
            return aabb;
        }

        return new FrameProfileRailGeometry.OuterCorners(
            root._outerBottomLeft ?? aabb.BottomLeft,
            root._outerBottomRight ?? aabb.BottomRight,
            root._outerTopLeft ?? aabb.TopLeft,
            root._outerTopRight ?? aabb.TopRight);
    }

    public CadPoint GetOuterCorner(FrameOuterCornerKind kind)
    {
        var corners = GetOuterCorners();
        return kind switch
        {
            FrameOuterCornerKind.BottomLeft => corners.BottomLeft,
            FrameOuterCornerKind.BottomRight => corners.BottomRight,
            FrameOuterCornerKind.TopLeft => corners.TopLeft,
            _ => corners.TopRight
        };
    }

    public void ClearOuterCorners()
    {
        var root = GetRoot();
        root._outerBottomLeft = null;
        root._outerBottomRight = null;
        root._outerTopLeft = null;
        root._outerTopRight = null;
    }

    /// <summary>
    /// Задаёт позицию внешнего угла. Габаритный AABB расширяется, если точка выходит за него;
    /// при опускании угла внутрь габарит сохраняется (перепад высоты).
    /// </summary>
    public bool TrySetOuterCorner(FrameOuterCornerKind kind, CadPoint point, double minSizeMm = 40)
    {
        var root = GetRoot();
        if (root.Shape != ConstructionShape.Rectangle
            || root.TopProfile?.SegmentType == SegmentType.Arc
               && root.TopProfile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return false;
        }

        var corners = root.GetOuterCorners();
        var next = kind switch
        {
            FrameOuterCornerKind.BottomLeft => corners with
            {
                BottomLeft = new CadPoint(CadMath.RoundMm(point.X), CadMath.RoundMm(point.Y))
            },
            FrameOuterCornerKind.BottomRight => corners with
            {
                BottomRight = new CadPoint(CadMath.RoundMm(point.X), CadMath.RoundMm(point.Y))
            },
            FrameOuterCornerKind.TopLeft => corners with
            {
                TopLeft = new CadPoint(CadMath.RoundMm(point.X), CadMath.RoundMm(point.Y))
            },
            _ => corners with
            {
                TopRight = new CadPoint(CadMath.RoundMm(point.X), CadMath.RoundMm(point.Y))
            }
        };

        if (!IsValidOuterQuad(next, minSizeMm))
        {
            return false;
        }

        // Расширяем AABB только наружу; внутрь — сохраняем полномерный габарит.
        var minX = Math.Min(
            Math.Min(next.BottomLeft.X, next.BottomRight.X),
            Math.Min(next.TopLeft.X, next.TopRight.X));
        var maxX = Math.Max(
            Math.Max(next.BottomLeft.X, next.BottomRight.X),
            Math.Max(next.TopLeft.X, next.TopRight.X));
        var minY = Math.Min(
            Math.Min(next.BottomLeft.Y, next.BottomRight.Y),
            Math.Min(next.TopLeft.Y, next.TopRight.Y));
        var maxY = Math.Max(
            Math.Max(next.BottomLeft.Y, next.BottomRight.Y),
            Math.Max(next.TopLeft.Y, next.TopRight.Y));

        root.X = CadMath.RoundMm(Math.Min(root.X, minX));
        root.Y = CadMath.RoundMm(Math.Min(root.Y, minY));
        var right = Math.Max(root.X + root.Width, maxX);
        var top = Math.Max(root.Y + root.Height, maxY);
        root.Width = CadMath.RoundMm(right - root.X);
        root.Height = CadMath.RoundMm(top - root.Y);

        root._outerBottomLeft = next.BottomLeft;
        root._outerBottomRight = next.BottomRight;
        root._outerTopLeft = next.TopLeft;
        root._outerTopRight = next.TopRight;
        return true;
    }

    private static bool IsValidOuterQuad(FrameProfileRailGeometry.OuterCorners corners, double minSizeMm)
    {
        var widthBottom = corners.BottomLeft.DistanceTo(corners.BottomRight);
        var widthTop = corners.TopLeft.DistanceTo(corners.TopRight);
        var heightLeft = corners.BottomLeft.DistanceTo(corners.TopLeft);
        var heightRight = corners.BottomRight.DistanceTo(corners.TopRight);
        if (widthBottom < minSizeMm || widthTop < minSizeMm || heightLeft < minSizeMm || heightRight < minSizeMm)
        {
            return false;
        }

        return QuadAreaPositive(corners);
    }

    private static bool QuadAreaPositive(FrameProfileRailGeometry.OuterCorners c)
    {
        // Shoelace CCW: BL→BR→TR→TL
        var area =
            c.BottomLeft.X * c.BottomRight.Y - c.BottomRight.X * c.BottomLeft.Y
            + c.BottomRight.X * c.TopRight.Y - c.TopRight.X * c.BottomRight.Y
            + c.TopRight.X * c.TopLeft.Y - c.TopLeft.X * c.TopRight.Y
            + c.TopLeft.X * c.BottomLeft.Y - c.BottomLeft.X * c.TopLeft.Y;
        return area > CadMath.Epsilon;
    }

    public IEnumerable<(FrameOuterCornerKind Kind, CadPoint Point)> EnumerateOuterCorners()
    {
        yield return (FrameOuterCornerKind.BottomLeft, GetOuterCorner(FrameOuterCornerKind.BottomLeft));
        yield return (FrameOuterCornerKind.BottomRight, GetOuterCorner(FrameOuterCornerKind.BottomRight));
        yield return (FrameOuterCornerKind.TopLeft, GetOuterCorner(FrameOuterCornerKind.TopLeft));
        yield return (FrameOuterCornerKind.TopRight, GetOuterCorner(FrameOuterCornerKind.TopRight));
    }

    public FrameProfileRailGeometry.RailBand GetRailBand(FrameProfileRail rail, double profileThickness)
    {
        var root = GetRoot();
        return FrameProfileRailGeometry.ResolveRailBand(
            root.GetOuterCorners(),
            root.Width,
            root.Height,
            profileThickness,
            rail);
    }

    /// <summary>
    /// Делит выбранную рейку коробки пополам (стык 90° на середине внешней длины).
    /// Не затрагивает дерево ячеек / импосты.
    /// </summary>
    public bool TrySplitProfileRailInHalf(FrameProfileRail rail)
    {
        var root = GetRoot();
        if (root.HasProfileRailJoints(rail))
        {
            return false;
        }

        return root.TrySplitProfileRailSegmentInHalf(rail, 0);
    }

    /// <summary>
    /// Возвращает конечные ячейки дерева в геометрическом порядке. В отличие от
    /// <see cref="FindLeafAt"/>, метод не выбирает ячейку по точке и потому может
    /// использоваться для одного действия над всеми секциями, пересекаемыми импостом.
    /// </summary>
    public IEnumerable<FrameNode> EnumerateTerminalLeaves()
    {
        if (IsTerminalLeaf)
        {
            yield return this;
            yield break;
        }

        if (IsUniformSplit)
        {
            foreach (var cell in _uniformCells)
            {
                foreach (var leaf in cell.EnumerateTerminalLeaves())
                {
                    yield return leaf;
                }
            }

            yield break;
        }

        if (ChildA is not null)
        {
            foreach (var leaf in ChildA.EnumerateTerminalLeaves())
            {
                yield return leaf;
            }
        }

        if (ChildB is not null)
        {
            foreach (var leaf in ChildB.EnumerateTerminalLeaves())
            {
                yield return leaf;
            }
        }
    }

    /// <summary>
    /// Делит выбранный интервал рейки между соседними стыками пополам.
    /// Существующие стыки и геометрия остальных сегментов не изменяются.
    /// </summary>
    public bool TrySplitProfileRailSegmentInHalf(FrameProfileRail rail, int segmentIndex)
    {
        var root = GetRoot();
        var (start, end) = FrameProfileRailGeometry.ResolveOuterEdge(root.GetOuterCorners(), rail);
        var span = start.DistanceTo(end);
        var joints = root.GetProfileRailJoints(rail);
        var points = root.GetOuterRailPolylinePoints(rail);
        if (segmentIndex < 0 || segmentIndex >= points.Count - 1)
        {
            return false;
        }

        var segmentStart = points[segmentIndex];
        var segmentEnd = points[segmentIndex + 1];
        if (segmentStart.DistanceTo(segmentEnd) < MinSplitSizeMm * 2 || span <= CadMath.Epsilon)
        {
            return false;
        }

        var leftT = segmentIndex == 0 ? 0.0 : joints[segmentIndex - 1].RelativeT;
        var rightT = segmentIndex == joints.Count ? 1.0 : joints[segmentIndex].RelativeT;
        var halfT = (leftT + rightT) / 2.0;
        var midpoint = new CadPoint(
            CadMath.RoundMm((segmentStart.X + segmentEnd.X) / 2.0),
            CadMath.RoundMm((segmentStart.Y + segmentEnd.Y) / 2.0));
        var isCentralTopJoint = rail == FrameProfileRail.Top
                                && Math.Abs(halfT - 0.5) <= CadMath.Epsilon;

        root._profileRailJoints.Add(new FrameProfileRailJoint
        {
            Rail = rail,
            RelativeT = halfT,
            LockHorizontalAxis = isCentralTopJoint,
            HasFixedAnchor = rail == FrameProfileRail.Top,
            AnchorLocalX = rail == FrameProfileRail.Top ? midpoint.X - root.X : 0,
            AnchorLocalY = rail == FrameProfileRail.Top ? midpoint.Y - root.Y : 0,
            Kind = FrameRailJointKind.Butt90
        });
        return true;
    }

    public IReadOnlyList<FrameProfileRailJoint> GetProfileRailJoints(FrameProfileRail rail) =>
        GetRoot()._profileRailJoints
            .Where(j => j.Rail == rail)
            .OrderBy(j => j.RelativeT)
            .ToList();

    public bool HasProfileRailJoints(FrameProfileRail rail) =>
        GetRoot()._profileRailJoints.Any(j => j.Rail == rail);

    public IReadOnlyList<double> GetOuterRailSegmentLengthsMm(FrameProfileRail rail)
    {
        var points = GetOuterRailPolylinePoints(rail);
        return points.Zip(points.Skip(1), (a, b) => CadMath.RoundMm(a.DistanceTo(b))).ToList();
    }

    public IReadOnlyList<double> GetOuterRailSegmentProjectionsMm(FrameProfileRail rail)
    {
        var points = GetOuterRailPolylinePoints(rail);
        var horizontal = rail is FrameProfileRail.Top or FrameProfileRail.Bottom;
        return points.Zip(
                points.Skip(1),
                (a, b) => CadMath.RoundMm(horizontal ? Math.Abs(b.X - a.X) : Math.Abs(b.Y - a.Y)))
            .ToList();
    }

    public IReadOnlyList<double> GetOuterRailBoundaryCoordinates(FrameProfileRail rail)
    {
        var horizontal = rail is FrameProfileRail.Top or FrameProfileRail.Bottom;
        return GetOuterRailPolylinePoints(rail)
            .Select(point => CadMath.RoundMm(horizontal ? point.X : point.Y))
            .ToList();
    }

    public IReadOnlyList<CadPoint> GetOuterRailPolylinePoints(FrameProfileRail rail)
    {
        var root = GetRoot();
        var corners = root.GetOuterCorners();
        var (start, end) = FrameProfileRailGeometry.ResolveOuterEdge(corners, rail);
        var points = new List<CadPoint> { start };
        points.AddRange(root.GetProfileRailJoints(rail).Select(joint =>
            root.ResolveProfileRailJointOuterPoint(joint)));
        points.Add(end);
        return points;
    }

    public CadPoint ResolveProfileRailJointOuterPoint(FrameProfileRailJoint joint)
    {
        var root = GetRoot();
        if (joint.HasFixedAnchor)
        {
            return new CadPoint(
                CadMath.RoundMm(root.X + joint.AnchorLocalX),
                CadMath.RoundMm(root.Y + joint.AnchorLocalY));
        }

        return FrameProfileRailGeometry.ResolveJointOuterPoint(
            root.GetOuterCorners(),
            joint.Rail,
            joint.RelativeT,
            joint.NormalOffsetMm);
    }

    /// <summary>Полный внешний контур с учётом свободно смещённых стыков, обход CCW.</summary>
    public IReadOnlyList<CadPoint> GetOuterContourPoints()
    {
        var bottom = GetOuterRailPolylinePoints(FrameProfileRail.Bottom);
        var right = GetOuterRailPolylinePoints(FrameProfileRail.Right);
        var top = GetOuterRailPolylinePoints(FrameProfileRail.Top).Reverse().ToList();
        var left = GetOuterRailPolylinePoints(FrameProfileRail.Left).Reverse().ToList();
        return bottom
            .Concat(right.Skip(1))
            .Concat(top.Skip(1))
            .Concat(left.Skip(1).SkipLast(1))
            .ToList();
    }

    public bool TrySetProfileRailJointT(Guid jointId, double relativeT)
    {
        var root = GetRoot();
        var joint = root._profileRailJoints.FirstOrDefault(j => j.Id == jointId);
        if (joint is null)
        {
            return false;
        }

        joint.RelativeT = Math.Clamp(relativeT, 0.05, 0.95);
        return true;
    }

    public bool TrySetProfileRailJointPosition(
        Guid jointId,
        CadPoint worldPoint,
        bool overrideLockedAxis = false)
    {
        var root = GetRoot();
        var joint = root._profileRailJoints.FirstOrDefault(item => item.Id == jointId);
        if (joint is null)
        {
            return false;
        }

        var currentPoint = root.ResolveProfileRailJointOuterPoint(joint);
        if (!overrideLockedAxis
            && (joint.LockHorizontalAxis
                || (joint.Rail == FrameProfileRail.Top
                    && Math.Abs(joint.RelativeT - 0.5) <= CadMath.Epsilon)))
        {
            worldPoint = new CadPoint(currentPoint.X, worldPoint.Y);
        }

        var projected = FrameProfileRailGeometry.ProjectPointOntoRail(
            root.GetOuterCorners(),
            joint.Rail,
            worldPoint);
        var previousT = joint.RelativeT;
        var previousOffset = joint.NormalOffsetMm;
        if (Math.Abs(previousT - projected.RelativeT) <= CadMath.Epsilon
            && Math.Abs(previousOffset - projected.NormalOffsetMm) <= CadMath.Epsilon)
        {
            return false;
        }

        joint.RelativeT = projected.RelativeT;
        joint.NormalOffsetMm = projected.NormalOffsetMm;
        if (joint.HasFixedAnchor)
        {
            joint.AnchorLocalX = CadMath.RoundMm(worldPoint.X - root.X);
            joint.AnchorLocalY = CadMath.RoundMm(worldPoint.Y - root.Y);
        }

        var points = root.GetOuterRailPolylinePoints(joint.Rail);
        if (points.Zip(points.Skip(1), (a, b) => a.DistanceTo(b)).Any(length => length < MinSplitSizeMm))
        {
            joint.RelativeT = previousT;
            joint.NormalOffsetMm = previousOffset;
            if (joint.HasFixedAnchor)
            {
                joint.AnchorLocalX = CadMath.RoundMm(currentPoint.X - root.X);
                joint.AnchorLocalY = CadMath.RoundMm(currentPoint.Y - root.Y);
            }
            return false;
        }

        return true;
    }

    private void BecomingSplitParent()
    {
        // Крайние импосты (нащельник) остаются на узле-родителе и наследуются дочерними ячейками.
    }

    public bool TryUpsertExtremeImpost(
        SplitOrientation orientation,
        ImpostEdgeAlignment edgeAlignment,
        double thicknessMm,
        double offsetMm = 0,
        double spanStartMm = 0,
        double spanEndMm = 0,
        double lightOpeningInsetMm = 0)
    {
        if (!IsLeaf || thicknessMm <= CadMath.Epsilon)
        {
            return false;
        }

        var existing = _extremeImposts.FirstOrDefault(slot =>
            slot.EdgeAlignment == edgeAlignment
            && CadMath.NearlyEqual(slot.SpanStartMm, spanStartMm)
            && CadMath.NearlyEqual(slot.SpanEndMm, spanEndMm));
        if (existing is not null)
        {
            existing.OffsetMm = CadMath.RoundMm(Math.Max(0, offsetMm));
            existing.ThicknessMm = thicknessMm;
            existing.SpanStartMm = CadMath.RoundMm(spanStartMm);
            existing.SpanEndMm = CadMath.RoundMm(spanEndMm);
            existing.LightOpeningInsetMm = Math.Max(0, lightOpeningInsetMm);
            return true;
        }

        var overlapping = _extremeImposts.FirstOrDefault(slot =>
            slot.EdgeAlignment == edgeAlignment
            && SpansOverlap(slot.SpanStartMm, slot.SpanEndMm, spanStartMm, spanEndMm));
        if (overlapping is not null)
        {
            overlapping.OffsetMm = CadMath.RoundMm(Math.Max(0, offsetMm));
            overlapping.ThicknessMm = thicknessMm;
            overlapping.SpanStartMm = CadMath.RoundMm(spanStartMm);
            overlapping.SpanEndMm = CadMath.RoundMm(spanEndMm);
            overlapping.LightOpeningInsetMm = Math.Max(0, lightOpeningInsetMm);
            return true;
        }

        _extremeImposts.Add(new ExtremeImpostSlot
        {
            Orientation = orientation,
            EdgeAlignment = edgeAlignment,
            ThicknessMm = thicknessMm,
            OffsetMm = CadMath.RoundMm(Math.Max(0, offsetMm)),
            SpanStartMm = CadMath.RoundMm(spanStartMm),
            SpanEndMm = CadMath.RoundMm(spanEndMm),
            LightOpeningInsetMm = Math.Max(0, lightOpeningInsetMm)
        });
        return true;
    }

    /// <summary>
    /// Суммарные отступы светового проёма от крайних импостов (нащельник и т.п.) по краям ячейки.
    /// Учитывает слоты предков, если их край совпадает с соответствующим краем этой ячейки.
    /// </summary>
    public EdgeLightOpeningInsets ResolveEdgeLightOpeningInsets()
    {
        double left = 0, right = 0, bottom = 0, top = 0;
        for (var node = this; node is not null; node = node.Parent)
        {
            var nodeLeft = node.X;
            var nodeRight = node.X + node.Width;
            var nodeBottom = node.Y;
            var nodeTop = node.Y + node.Height;

            foreach (var slot in node.ExtremeImposts)
            {
                var inset = slot.LightOpeningInsetMm > CadMath.Epsilon
                    ? slot.LightOpeningInsetMm
                    : slot.ThicknessMm;
                inset += slot.OffsetMm;

                if (slot.EdgeAlignment == ImpostEdgeAlignment.Left
                    && CadMath.NearlyEqual(X, nodeLeft))
                {
                    left = Math.Max(left, inset);
                }
                else if (slot.EdgeAlignment == ImpostEdgeAlignment.Right
                         && CadMath.NearlyEqual(X + Width, nodeRight))
                {
                    right = Math.Max(right, inset);
                }
                else if (slot.EdgeAlignment == ImpostEdgeAlignment.Bottom
                         && CadMath.NearlyEqual(Y, nodeBottom))
                {
                    bottom = Math.Max(bottom, inset);
                }
                else if (slot.EdgeAlignment == ImpostEdgeAlignment.Top
                         && CadMath.NearlyEqual(Y + Height, nodeTop))
                {
                    top = Math.Max(top, inset);
                }
            }
        }

        return new EdgeLightOpeningInsets
        {
            Left = CadMath.RoundMm(left),
            Right = CadMath.RoundMm(right),
            Bottom = CadMath.RoundMm(bottom),
            Top = CadMath.RoundMm(top)
        };
    }

    /// <summary>
    /// Габариты вложенной коробки/заполнения внутри ячейки с учётом нащельника на краях.
    /// </summary>
    public (double OriginX, double OriginY, double Width, double Height) ResolveNestedOpeningBounds()
    {
        var edge = ResolveEdgeLightOpeningInsets();
        return (
            CadMath.RoundMm(X + edge.Left),
            CadMath.RoundMm(Y + edge.Bottom),
            CadMath.RoundMm(Math.Max(0, Width - edge.Left - edge.Right)),
            CadMath.RoundMm(Math.Max(0, Height - edge.Bottom - edge.Top)));
    }

    /// <summary>
    /// Габариты размещения внутри квадранта пунктирной сетки с учётом нащельника на совпадающих краях.
    /// </summary>
    public (double OriginX, double OriginY, double Width, double Height) ResolvePlacementBoundsForCell(
        double cellLeft,
        double cellBottom,
        double cellRight,
        double cellTop)
    {
        var edge = ResolveEdgeLightOpeningInsets();
        var originX = cellLeft;
        var originY = cellBottom;
        var width = cellRight - cellLeft;
        var height = cellTop - cellBottom;

        if (CadMath.NearlyEqual(cellLeft, X))
        {
            originX += edge.Left;
            width -= edge.Left;
        }

        if (CadMath.NearlyEqual(cellRight, X + Width))
        {
            width -= edge.Right;
        }

        if (CadMath.NearlyEqual(cellBottom, Y))
        {
            originY += edge.Bottom;
            height -= edge.Bottom;
        }

        if (CadMath.NearlyEqual(cellTop, Y + Height))
        {
            height -= edge.Top;
        }

        return (
            CadMath.RoundMm(originX),
            CadMath.RoundMm(originY),
            CadMath.RoundMm(Math.Max(0, width)),
            CadMath.RoundMm(Math.Max(0, height)));
    }

    public double ResolveInsetAdjustedVerticalSplitCenterLocal()
    {
        var edge = ResolveEdgeLightOpeningInsets();
        var innerWidth = Width - edge.Left - edge.Right;
        return CadMath.RoundMm(edge.Left + innerWidth / 2.0);
    }

    /// <summary>
    /// Logical center-line bounds of this vertical section. Child geometry is
    /// clipped to mullion faces, while dimensions and centered re-splits run
    /// from outer edge to mullion axis (or between two neighbouring axes).
    /// </summary>
    public (double Left, double Right) ResolveVerticalCellAxisBoundsWorld()
    {
        if (Parent is null)
        {
            return (CadMath.RoundMm(X), CadMath.RoundMm(X + Width));
        }

        var parentBounds = Parent.ResolveVerticalCellAxisBoundsWorld();
        if (Parent.SplitOrientation != SplitOrientation.Vertical)
        {
            return parentBounds;
        }

        if (Parent.IsUniformSplit)
        {
            var cellIndex = Parent._uniformCells.IndexOf(this);
            if (cellIndex < 0)
            {
                return (CadMath.RoundMm(X), CadMath.RoundMm(X + Width));
            }

            var left = parentBounds.Left;
            var right = parentBounds.Right;
            if (cellIndex > 0
                && Parent.TryGetUniformImpostAxisWorld(
                    SplitOrientation.Vertical,
                    cellIndex - 1,
                    out var leftAxis))
            {
                left = leftAxis;
            }

            if (cellIndex < Parent._uniformCells.Count - 1
                && Parent.TryGetUniformImpostAxisWorld(
                    SplitOrientation.Vertical,
                    cellIndex,
                    out var rightAxis))
            {
                right = rightAxis;
            }

            return (CadMath.RoundMm(left), CadMath.RoundMm(right));
        }

        if (Parent.TryGetVerticalImpostAxisWorldX(out var parentAxis))
        {
            if (ReferenceEquals(Parent.ChildA, this))
            {
                return (parentBounds.Left, CadMath.RoundMm(parentAxis));
            }

            if (ReferenceEquals(Parent.ChildB, this))
            {
                return (CadMath.RoundMm(parentAxis), parentBounds.Right);
            }
        }

        return (CadMath.RoundMm(X), CadMath.RoundMm(X + Width));
    }

    public double ResolveInsetAdjustedHorizontalSplitCenterLocal(double splitExtentLocal)
    {
        var edge = ResolveEdgeLightOpeningInsets();
        var innerHeight = splitExtentLocal - edge.Bottom - edge.Top;
        return CadMath.RoundMm(edge.Bottom + innerHeight / 2.0);
    }

    public bool TrySetExtremeImpostOffset(Guid slotId, double offsetMm)
    {
        var slot = _extremeImposts.FirstOrDefault(s => s.Id == slotId);
        if (slot is null)
        {
            return false;
        }

        slot.OffsetMm = CadMath.RoundMm(Math.Max(0, offsetMm));
        return true;
    }

    public bool TryRemoveExtremeImpost(Guid slotId) =>
        IsLeaf && _extremeImposts.RemoveAll(slot => slot.Id == slotId) > 0;

    public ExtremeImpostSlot? FindExtremeImpost(Guid slotId) =>
        _extremeImposts.FirstOrDefault(s => s.Id == slotId);

    public IEnumerable<(FrameNode Leaf, ExtremeImpostSlot Slot)> EnumerateExtremeImposts()
    {
        foreach (var slot in _extremeImposts)
        {
            yield return (this, slot);
        }

        if (IsLeaf)
        {
            yield break;
        }

        if (IsUniformSplit)
        {
            foreach (var cell in _uniformCells)
            {
                foreach (var pair in cell.EnumerateExtremeImposts())
                {
                    yield return pair;
                }
            }

            yield break;
        }

        if (ChildA is not null)
        {
            foreach (var pair in ChildA.EnumerateExtremeImposts())
            {
                yield return pair;
            }
        }

        if (ChildB is not null)
        {
            foreach (var pair in ChildB.EnumerateExtremeImposts())
            {
                yield return pair;
            }
        }
    }

    private void ClearExtremeImposts() => _extremeImposts.Clear();

    public void ClearSplits()
    {
        ChildA = null;
        ChildB = null;
        ClearUniformSplitState();
        IsLeaf = true;
        SplitOrientation = SplitOrientation.None;
        ImpostThicknessMm = 0;
        LineStyle = ImpostLineStyle.Dashed;
        SplitRelativeCenter = null;
        ClearExtremeImposts();
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
        ClearOuterCorners();
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    private static bool SpansOverlap(double startA, double endA, double startB, double endB) =>
        endA > startB + CadMath.MmPrecision && endB > startA + CadMath.MmPrecision;
}
