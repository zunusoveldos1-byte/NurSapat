namespace NurSapat.Cad.Core.Models;

/// <summary>
/// Узел бинарного дерева просветов конструкции (мм).
/// </summary>
public sealed class OpeningNode
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public OpeningNode? Parent { get; set; }
    public OpeningNode? Left { get; set; }
    public OpeningNode? Right { get; set; }
    public OpeningNode? Top { get; set; }
    public OpeningNode? Bottom { get; set; }

    public bool Contains(double x, double y) =>
        x >= X && x <= X + Width && y >= Y && y <= Y + Height;

    public OpeningNode? FindLeafAt(double x, double y)
    {
        if (!Contains(x, y))
        {
            return null;
        }

        foreach (var child in Children())
        {
            var leaf = child.FindLeafAt(x, y);
            if (leaf != null)
            {
                return leaf;
            }
        }

        return this;
    }

    public OpeningNode SplitVertical(double x, double thickness)
    {
        var splitX = Math.Clamp(x - thickness / 2, X, X + Width - thickness);
        var left = new OpeningNode
        {
            X = X,
            Y = Y,
            Width = splitX - X,
            Height = Height,
            Parent = this
        };
        var right = new OpeningNode
        {
            X = splitX + thickness,
            Y = Y,
            Width = X + Width - (splitX + thickness),
            Height = Height,
            Parent = this
        };
        Left = left;
        Right = right;
        return right;
    }

    public OpeningNode SplitHorizontal(double y, double thickness)
    {
        var splitY = Math.Clamp(y - thickness / 2, Y, Y + Height - thickness);
        var top = new OpeningNode
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = splitY - Y,
            Parent = this
        };
        var bottom = new OpeningNode
        {
            X = X,
            Y = splitY + thickness,
            Width = Width,
            Height = Y + Height - (splitY + thickness),
            Parent = this
        };
        Top = top;
        Bottom = bottom;
        return bottom;
    }

    public bool CanAccept(WindowToolMode tool) => tool switch
    {
        WindowToolMode.RealFrame or WindowToolMode.FakeFrame => Parent is null,
        WindowToolMode.FakeVerticalImpost or WindowToolMode.FakeHorizontalImpost => Width > 0 && Height > 0,
        WindowToolMode.VerticalImpost or WindowToolMode.HorizontalImpost or WindowToolMode.DiagonalImpost => Width > 0 && Height > 0,
        WindowToolMode.Sash or WindowToolMode.Glass or WindowToolMode.GlassWithBead => Width > 0 && Height > 0,
        _ => true
    };

    public IEnumerable<OpeningNode> Children()
    {
        if (Left != null) yield return Left;
        if (Right != null) yield return Right;
        if (Top != null) yield return Top;
        if (Bottom != null) yield return Bottom;
    }

    public static OpeningNode CreateRoot(double x, double y, double width, double height) =>
        new() { X = x, Y = y, Width = width, Height = height };
}
