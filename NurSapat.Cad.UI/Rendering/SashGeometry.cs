using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

internal static class SashGeometry
{
    public const double OverlapMm = 8.0;
    public const double FrameOverlapMm = 18.0;
    public const double ProfileThicknessMm = 60.0;

    public struct InnerOpeningBounds
    {
        public double Left;
        public double Bottom;
        public double Right;
        public double Top;
        public bool PinsFrameLeft;
        public bool PinsFrameRight;
        public bool PinsFrameBottom;
        public bool PinsFrameTop;

        public double Width => Right - Left;
        public double Height => Top - Bottom;
    }

    public struct OuterBounds
    {
        public double Left;
        public double Bottom;
        public double Right;
        public double Top;

        public double Width => Right - Left;
        public double Height => Top - Bottom;
    }

    public static (double X, double Y, double Width, double Height) FromElement(GeometryElement element)
    {
        var inner = InnerOpeningBoundsFromElement(element);
        return (inner.Left, inner.Bottom, inner.Width, inner.Height);
    }

    public static InnerOpeningBounds InnerOpeningBoundsFromElement(GeometryElement element)
    {
        var halfW = (double)element.WidthMm / 2.0;
        var halfH = (double)element.HeightMm / 2.0;
        var cx = (double)element.X;
        var cy = (double)element.Y;
        return RoundBounds(new InnerOpeningBounds
        {
            Left = cx - halfW,
            Bottom = cy - halfH,
            Right = cx + halfW,
            Top = cy + halfH
        });
    }

    /// <summary>
    /// Внешний контур: 8 мм на импост, 18 мм на внешнюю раму (если край привязан к frameInner).
    /// </summary>
    public static OuterBounds ExpandInnerBoundsToOuter(in InnerOpeningBounds inner, CadEditorViewModel? editor = null)
    {
        var overlapLeft = inner.PinsFrameLeft ? FrameOverlapMm : OverlapMm;
        var overlapRight = inner.PinsFrameRight ? FrameOverlapMm : OverlapMm;
        var overlapBottom = inner.PinsFrameBottom ? FrameOverlapMm : OverlapMm;
        var overlapTop = inner.PinsFrameTop ? FrameOverlapMm : OverlapMm;

        if (editor?.FrameRoot is not null)
        {
            var inset = FrameProfileGeometry.ResolveInnerInset(
                editor.FrameRoot.Width,
                editor.FrameRoot.Height,
                editor.FrameThickness);

            var frameInnerLeft = editor.FrameRoot.X + inset;
            var frameInnerBottom = editor.FrameRoot.Y + inset;
            var frameInnerRight = editor.FrameRoot.X + editor.FrameRoot.Width - inset;
            var frameInnerTop = editor.FrameRoot.Y + editor.FrameRoot.Height - inset;

            if (!inner.PinsFrameLeft && CadMath.NearlyEqual(inner.Left, frameInnerLeft))
            {
                overlapLeft = FrameOverlapMm;
            }

            if (!inner.PinsFrameRight && CadMath.NearlyEqual(inner.Right, frameInnerRight))
            {
                overlapRight = FrameOverlapMm;
            }

            if (!inner.PinsFrameBottom && CadMath.NearlyEqual(inner.Bottom, frameInnerBottom))
            {
                overlapBottom = FrameOverlapMm;
            }

            if (!inner.PinsFrameTop && CadMath.NearlyEqual(inner.Top, frameInnerTop))
            {
                overlapTop = FrameOverlapMm;
            }
        }

        return RoundOuter(new OuterBounds
        {
            Left = inner.Left - overlapLeft,
            Bottom = inner.Bottom - overlapBottom,
            Right = inner.Right + overlapRight,
            Top = inner.Top + overlapTop
        });
    }

    public static (double X, double Y, double Width, double Height) ExpandToOuter(
        double innerX,
        double innerY,
        double innerWidth,
        double innerHeight,
        CadEditorViewModel? editor = null)
    {
        var outer = ExpandInnerBoundsToOuter(new InnerOpeningBounds
        {
            Left = innerX,
            Bottom = innerY,
            Right = innerX + innerWidth,
            Top = innerY + innerHeight
        }, editor);
        return (outer.Left, outer.Bottom, outer.Width, outer.Height);
    }

    public static bool TryResolveInnerBounds(
        CadEditorViewModel editor,
        GeometryElement element,
        out InnerOpeningBounds inner)
    {
        inner = default;
        if (editor.FrameRoot is null || element.Kind != GeometryElementKind.Sash)
        {
            return false;
        }

        var leaf = editor.FrameRoot.FindLeafAt((double)element.X, (double)element.Y);
        if (leaf is null)
        {
            return false;
        }

        inner = ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness);
        return inner.Width > 0 && inner.Height > 0;
    }

    public static (double X, double Y, double Width, double Height) ResolveLeafInnerOpening(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness)
    {
        var inner = ResolveLeafInnerOpeningBounds(leaf, frameRoot, frameThickness);
        return (inner.Left, inner.Bottom, inner.Width, inner.Height);
    }

    public static InnerOpeningBounds ResolveLeafInnerOpeningBounds(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness)
    {
        var inner = new InnerOpeningBounds
        {
            Left = leaf.X,
            Bottom = leaf.Y,
            Right = leaf.X + leaf.Width,
            Top = leaf.Y + leaf.Height
        };

        ApplyImpostAxisEdges(leaf, ref inner);

        if (frameRoot is not null)
        {
            PinFrameInnerEdges(leaf, frameRoot, frameThickness, ref inner);
        }

        return RoundBounds(inner);
    }

    public static double ResolveVerticalImpostCenterX(FrameNode splitNode)
    {
        if (splitNode.TryGetVerticalImpostAxisWorldX(out var axisWorldX))
        {
            return axisWorldX;
        }

        if (splitNode.ChildA is null || splitNode.ChildB is null)
        {
            return 0;
        }

        return CadMath.RoundMm(
            (splitNode.ChildA.X + splitNode.ChildA.Width + splitNode.ChildB.X) / 2.0);
    }

    public static double ResolveHorizontalImpostCenterY(FrameNode splitNode)
    {
        if (splitNode.TryGetHorizontalImpostAxisWorldY(out var axisWorldY))
        {
            return axisWorldY;
        }

        if (splitNode.ChildA is null || splitNode.ChildB is null)
        {
            return 0;
        }

        return CadMath.RoundMm(
            (splitNode.ChildA.Y + splitNode.ChildA.Height + splitNode.ChildB.Y) / 2.0);
    }

    public static (double Left, double Bottom, double Right, double Top) ResolveInnerGlassFromOuter(
        double outerX,
        double outerY,
        double outerWidth,
        double outerHeight)
    {
        var inset = ResolveSafeGlassInset(outerWidth, outerHeight);
        return (
            outerX + inset,
            outerY + inset,
            outerX + outerWidth - inset,
            outerY + outerHeight - inset);
    }

    public static double ResolveSafeGlassInset(double outerWidth, double outerHeight)
    {
        var maxInset = Math.Min(outerWidth, outerHeight) / 2.0 - 1.0;
        if (maxInset <= 0)
        {
            return 0;
        }

        return Math.Min(ProfileThicknessMm, maxInset);
    }

    public static bool ContainsOuterBounds(GeometryElement element, double worldX, double worldY, CadEditorViewModel? editor = null)
    {
        var inner = InnerOpeningBoundsFromElement(element);
        var outer = ExpandInnerBoundsToOuter(inner, editor);
        return worldX >= outer.Left && worldX <= outer.Right
            && worldY >= outer.Bottom && worldY <= outer.Top;
    }

    private static void PinFrameInnerEdges(
        FrameNode leaf,
        FrameNode frameRoot,
        double frameThickness,
        ref InnerOpeningBounds inner)
    {
        var inset = FrameProfileGeometry.ResolveInnerInset(
            frameRoot.Width,
            frameRoot.Height,
            frameThickness);

        var frameLeft = frameRoot.X;
        var frameBottom = frameRoot.Y;
        var frameRight = frameRoot.X + frameRoot.Width;
        var frameTop = frameRoot.Y + frameRoot.Height;
        var frameInnerLeft = CadMath.RoundMm(frameLeft + inset);
        var frameInnerBottom = CadMath.RoundMm(frameBottom + inset);
        var frameInnerRight = CadMath.RoundMm(frameRight - inset);
        var frameInnerTop = CadMath.RoundMm(frameTop - inset);

        if (OccupiesFrameLeftEdge(leaf, frameLeft, frameInnerLeft))
        {
            inner.Left = frameInnerLeft;
            inner.PinsFrameLeft = true;
        }

        if (OccupiesFrameRightEdge(leaf, frameRight, frameInnerRight))
        {
            inner.Right = frameInnerRight;
            inner.PinsFrameRight = true;
        }

        if (OccupiesFrameBottomEdge(leaf, frameBottom, frameInnerBottom))
        {
            inner.Bottom = frameInnerBottom;
            inner.PinsFrameBottom = true;
        }

        if (OccupiesFrameTopEdge(leaf, frameTop, frameInnerTop))
        {
            inner.Top = frameInnerTop;
            inner.PinsFrameTop = true;
        }
    }

    private static bool OccupiesFrameLeftEdge(FrameNode leaf, double frameLeft, double frameInnerLeft) =>
        CadMath.NearlyEqual(leaf.X, frameLeft) || CadMath.NearlyEqual(leaf.X, frameInnerLeft);

    private static bool OccupiesFrameRightEdge(FrameNode leaf, double frameRight, double frameInnerRight) =>
        CadMath.NearlyEqual(leaf.X + leaf.Width, frameRight)
        || CadMath.NearlyEqual(leaf.X + leaf.Width, frameInnerRight)
        || leaf.X + leaf.Width >= frameInnerRight - CadMath.MmPrecision;

    private static bool OccupiesFrameBottomEdge(FrameNode leaf, double frameBottom, double frameInnerBottom) =>
        CadMath.NearlyEqual(leaf.Y, frameBottom) || CadMath.NearlyEqual(leaf.Y, frameInnerBottom);

    private static bool OccupiesFrameTopEdge(FrameNode leaf, double frameTop, double frameInnerTop) =>
        CadMath.NearlyEqual(leaf.Y + leaf.Height, frameTop)
        || CadMath.NearlyEqual(leaf.Y + leaf.Height, frameInnerTop)
        || leaf.Y + leaf.Height >= frameInnerTop - CadMath.MmPrecision;

    private static void ApplyImpostAxisEdges(FrameNode leaf, ref InnerOpeningBounds inner)
    {
        for (var node = leaf; node.Parent is not null; node = node.Parent)
        {
            var parent = node.Parent;
            if (parent.IsUniformSplit)
            {
                ApplyUniformImpostAxisEdges(parent, leaf, ref inner);
                continue;
            }

            if (parent.IsLeaf || parent.ChildA is null || parent.ChildB is null)
            {
                continue;
            }

            var halfThickness = parent.GetResolvedImpostThicknessMm() / 2.0;

            if (parent.SplitOrientation == SplitOrientation.Vertical)
            {
                var centerX = ResolveVerticalImpostCenterX(parent);
                if (IsInSubtree(leaf, parent.ChildA))
                {
                    inner.Right = CadMath.RoundMm(centerX - halfThickness);
                    inner.PinsFrameRight = false;
                }
                else if (IsInSubtree(leaf, parent.ChildB))
                {
                    inner.Left = CadMath.RoundMm(centerX + halfThickness);
                    inner.PinsFrameLeft = false;
                }
            }
            else if (parent.SplitOrientation == SplitOrientation.Horizontal)
            {
                var centerY = ResolveHorizontalImpostCenterY(parent);
                if (IsInSubtree(leaf, parent.ChildA))
                {
                    inner.Top = CadMath.RoundMm(centerY - halfThickness);
                    inner.PinsFrameTop = false;
                }
                else if (IsInSubtree(leaf, parent.ChildB))
                {
                    inner.Bottom = CadMath.RoundMm(centerY + halfThickness);
                    inner.PinsFrameBottom = false;
                }
            }
        }
    }

    private static void ApplyUniformImpostAxisEdges(
        FrameNode parent,
        FrameNode leaf,
        ref InnerOpeningBounds inner)
    {
        var cells = parent.UniformCells;
        var halfThickness = parent.GetResolvedImpostThicknessMm() / 2.0;

        for (var i = 0; i < cells.Count; i++)
        {
            if (!IsInSubtree(leaf, cells[i]))
            {
                continue;
            }

            if (parent.SplitOrientation == SplitOrientation.Vertical)
            {
                if (i > 0
                    && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i - 1, out var leftAxis))
                {
                    inner.Left = CadMath.RoundMm(leftAxis + halfThickness);
                    inner.PinsFrameLeft = false;
                }

                if (i < cells.Count - 1
                    && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var rightAxis))
                {
                    inner.Right = CadMath.RoundMm(rightAxis - halfThickness);
                    inner.PinsFrameRight = false;
                }
            }
            else if (parent.SplitOrientation == SplitOrientation.Horizontal)
            {
                if (i > 0
                    && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i - 1, out var bottomAxis))
                {
                    inner.Bottom = CadMath.RoundMm(bottomAxis + halfThickness);
                    inner.PinsFrameBottom = false;
                }

                if (i < cells.Count - 1
                    && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var topAxis))
                {
                    inner.Top = CadMath.RoundMm(topAxis - halfThickness);
                    inner.PinsFrameTop = false;
                }
            }

            return;
        }
    }

    private static InnerOpeningBounds RoundBounds(InnerOpeningBounds inner)
    {
        inner.Left = CadMath.RoundMm(inner.Left);
        inner.Bottom = CadMath.RoundMm(inner.Bottom);
        inner.Right = CadMath.RoundMm(inner.Right);
        inner.Top = CadMath.RoundMm(inner.Top);
        return inner;
    }

    private static OuterBounds RoundOuter(OuterBounds outer)
    {
        outer.Left = CadMath.RoundMm(outer.Left);
        outer.Bottom = CadMath.RoundMm(outer.Bottom);
        outer.Right = CadMath.RoundMm(outer.Right);
        outer.Top = CadMath.RoundMm(outer.Top);
        return outer;
    }

    private static bool IsInSubtree(FrameNode leaf, FrameNode? ancestor)
    {
        if (ancestor is null)
        {
            return false;
        }

        for (var node = leaf; node is not null; node = node.Parent)
        {
            if (ReferenceEquals(node, ancestor))
            {
                return true;
            }
        }

        return false;
    }
}