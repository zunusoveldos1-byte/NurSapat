using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Parametric;

public sealed class WindowSection
{
    private readonly List<WindowSection> _children = new();

    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid? ParentId { get; init; }

    public CadRect Bounds { get; private set; }

    public ImpostDefinition? SplitImpost { get; private set; }

    public double? SplitCoordinate { get; private set; }

    public double SplitAngleDegrees { get; private set; } = 45;

    public IReadOnlyList<WindowSection> Children => _children;

    public SectionFill Fill { get; } = new();

    public bool IsLeaf => _children.Count == 0;

    public WindowSection(CadRect bounds, Guid? parentId = null)
    {
        Bounds = bounds;
        ParentId = parentId;
    }

    public IEnumerable<WindowSection> Flatten()
    {
        yield return this;
        foreach (var child in _children.SelectMany(c => c.Flatten()))
        {
            yield return child;
        }
    }

    public WindowSection? HitTest(CadPoint point)
    {
        if (!Bounds.Contains(point))
        {
            return null;
        }

        foreach (var child in _children)
        {
            var hit = child.HitTest(point);
            if (hit != null)
            {
                return hit;
            }
        }

        return this;
    }

    public void SplitVertical(double coordinateX, double profileWidth)
    {
        EnsureLeaf();
        var x = Math.Clamp(coordinateX, Bounds.X + profileWidth / 2, Bounds.X + Bounds.Width - profileWidth / 2);
        var leftWidth = x - profileWidth / 2 - Bounds.X;
        var rightX = x + profileWidth / 2;
        var rightWidth = Bounds.X + Bounds.Width - rightX;

        SplitImpost = new ImpostDefinition(
            SectionSplitOrientation.Vertical,
            profileWidth,
            new CadLine(new CadPoint(x, Bounds.Y), new CadPoint(x, Bounds.Y - Bounds.Height)));
        SplitCoordinate = x;

        _children.Add(new WindowSection(new CadRect(Bounds.X, Bounds.Y, leftWidth, Bounds.Height), Id));
        _children.Add(new WindowSection(new CadRect(rightX, Bounds.Y, rightWidth, Bounds.Height), Id));
    }

    public void SplitHorizontal(double coordinateY, double profileWidth)
    {
        EnsureLeaf();
        var y = Math.Clamp(coordinateY, Bounds.Y - Bounds.Height + profileWidth / 2, Bounds.Y - profileWidth / 2);
        var topHeight = Bounds.Y - y - profileWidth / 2;
        var bottomY = y - profileWidth / 2;
        var bottomHeight = bottomY - (Bounds.Y - Bounds.Height);

        SplitImpost = new ImpostDefinition(
            SectionSplitOrientation.Horizontal,
            profileWidth,
            new CadLine(new CadPoint(Bounds.X, y), new CadPoint(Bounds.X + Bounds.Width, y)));
        SplitCoordinate = y;

        _children.Add(new WindowSection(new CadRect(Bounds.X, Bounds.Y, Bounds.Width, topHeight), Id));
        _children.Add(new WindowSection(new CadRect(Bounds.X, bottomY, Bounds.Width, bottomHeight), Id));
    }

    public void SplitAngled(double angleDegrees, double profileWidth)
    {
        EnsureLeaf();
        var center = new CadPoint(Bounds.X + Bounds.Width / 2, Bounds.Y - Bounds.Height / 2);
        var radians = angleDegrees * Math.PI / 180.0;
        var direction = new CadVector(Math.Cos(radians), Math.Sin(radians));
        var intersections = IntersectRayWithRect(center, direction, Bounds).ToArray();

        if (intersections.Length < 2)
        {
            throw new InvalidOperationException("Angled impost does not intersect section bounds.");
        }

        SplitImpost = new ImpostDefinition(
            SectionSplitOrientation.Angled,
            profileWidth,
            new CadLine(intersections[0], intersections[1]),
            angleDegrees,
            180 - angleDegrees);
        SplitAngleDegrees = angleDegrees;
    }

    public void MoveSplit(double coordinate)
    {
        if (SplitImpost == null)
        {
            return;
        }

        var orientation = SplitImpost.Orientation;
        var profileWidth = SplitImpost.ProfileWidth;
        _children.Clear();
        SplitImpost = null;

        if (orientation == SectionSplitOrientation.Vertical)
        {
            SplitVertical(coordinate, profileWidth);
        }
        else if (orientation == SectionSplitOrientation.Horizontal)
        {
            SplitHorizontal(coordinate, profileWidth);
        }
    }

    public void ApplyFill(SectionFillKind kind)
    {
        EnsureLeaf();
        Fill.Kind = kind;
    }

    public void Recalculate(CadRect bounds)
    {
        Bounds = bounds;
        if (SplitImpost == null || _children.Count == 0)
        {
            return;
        }

        var orientation = SplitImpost.Orientation;
        var profileWidth = SplitImpost.ProfileWidth;
        var coordinate = SplitCoordinate;
        var angle = SplitAngleDegrees;
        _children.Clear();
        SplitImpost = null;

        if (orientation == SectionSplitOrientation.Vertical)
        {
            SplitVertical(coordinate ?? Bounds.X + Bounds.Width / 2, profileWidth);
        }
        else if (orientation == SectionSplitOrientation.Horizontal)
        {
            SplitHorizontal(coordinate ?? Bounds.Y - Bounds.Height / 2, profileWidth);
        }
        else
        {
            SplitAngled(angle, profileWidth);
        }
    }

    public string GetFactoryLabel(WindowOptions options)
    {
        var glass = GetGlassRect(options);
        return Fill.Kind switch
        {
            SectionFillKind.Glazing => $"Стеклопакет {Fill.GlassFormula} ({glass.Width:0}x{glass.Height:0})",
            SectionFillKind.Sash => $"Створка {Fill.ProfileSystem} / {Fill.GlassFormula} ({Bounds.Width:0}x{Bounds.Height:0})",
            SectionFillKind.Corrugation => $"Гофра ({glass.Width:0}x{glass.Height:0})",
            _ => $"{Bounds.Width:0}x{Bounds.Height:0}"
        };
    }

    public CadRect GetGlassRect(WindowOptions options)
    {
        var offset = Fill.HasSash
            ? options.SashProfileWidth + options.GlassGap
            : options.GlassGap;
        return Inflate(Bounds, -offset);
    }

    public CadRect GetSashRect(WindowOptions options) => Inflate(Bounds, options.SashOverlap);

    private static CadRect Inflate(CadRect rect, double amount)
    {
        return new CadRect(
            rect.X - amount,
            rect.Y + amount,
            Math.Max(1, rect.Width + amount * 2),
            Math.Max(1, rect.Height + amount * 2));
    }

    private void EnsureLeaf()
    {
        if (!IsLeaf)
        {
            throw new InvalidOperationException("Only leaf sections can be modified.");
        }
    }

    private static IEnumerable<CadPoint> IntersectRayWithRect(CadPoint center, CadVector direction, CadRect rect)
    {
        var left = rect.X;
        var right = rect.X + rect.Width;
        var top = rect.Y;
        var bottom = rect.Y - rect.Height;
        var edges = new[]
        {
            new CadLine(new CadPoint(left, top), new CadPoint(right, top)),
            new CadLine(new CadPoint(right, top), new CadPoint(right, bottom)),
            new CadLine(new CadPoint(right, bottom), new CadPoint(left, bottom)),
            new CadLine(new CadPoint(left, bottom), new CadPoint(left, top))
        };
        var ray = new CadLine(center - direction * 100000, center + direction * 100000);
        return edges.Select(e => CadMath.Intersect(ray, e)).OfType<CadPoint>().Distinct();
    }
}
