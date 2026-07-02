using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Linq;

namespace NurSapat.Cad.UI.Rendering;

internal static class SashGeometry
{
    public const double OverlapMm = 8.0;
    public const double FrameOverlapMm = 18.0;
    public const double ProfileThicknessMm = 60.0;
    public const double SashDrawProfileThicknessMm = ProfileThicknessMm * 2.0;
    private const double GlassBeadGapMm = 4.0;
    public const double BeadProfileDrawThicknessMm = 14.0;
    private const double HardwareArcTransitionMarginMm = 12.0;

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

    public readonly struct GlassOpeningBounds
    {
        public double Left { get; init; }
        public double Bottom { get; init; }
        public double Right { get; init; }
        public double Top { get; init; }
        public bool IsArched { get; init; }
        public CadPoint ArcCenter { get; init; }
        public double ArcRadius { get; init; }
        public CadPoint BottomLeft { get; init; }
        public CadPoint BottomRight { get; init; }
        public CadPoint SpringingLeft { get; init; }
        public CadPoint SpringingRight { get; init; }

        public double Width => Right - Left;
        public double Height => Top - Bottom;

        public static GlassOpeningBounds FromRect(double left, double bottom, double right, double top) =>
            new()
            {
                Left = left,
                Bottom = bottom,
                Right = right,
                Top = top,
                IsArched = false,
                BottomLeft = new CadPoint(left, bottom),
                BottomRight = new CadPoint(right, bottom)
            };

        public double ResolveTopAtX(double worldX) =>
            IsArched
                ? ProfileSegment.GetPointOnUpperArc(ArcCenter, ArcRadius, worldX).Y
                : Top;

        public void Deconstruct(out double left, out double bottom, out double right, out double top)
        {
            left = Left;
            bottom = Bottom;
            right = Right;
            top = Top;
        }

        public ProfileContourLoop BuildOuterContourLoop()
        {
            if (!IsArched)
            {
                return ProfileContourLoop.FromEdges(
                    ProfileEdge.Line(BottomLeft, new CadPoint(Left, Top)),
                    ProfileEdge.Line(new CadPoint(Left, Top), new CadPoint(Right, Top)),
                    ProfileEdge.Line(new CadPoint(Right, Top), BottomRight),
                    ProfileEdge.Line(BottomRight, BottomLeft));
            }

            var startAngle = CircularArc.AngleRad(ArcCenter, SpringingLeft);
            var endAngle = CircularArc.AngleRad(ArcCenter, SpringingRight);
            return ProfileContourLoop.FromEdges(
                ProfileEdge.Line(BottomLeft, SpringingLeft),
                ProfileEdge.Arc(ArcCenter, ArcRadius, startAngle, endAngle),
                ProfileEdge.Line(SpringingRight, BottomRight),
                ProfileEdge.Line(BottomRight, BottomLeft));
        }

        public bool TryBuildInnerContourLoop(double insetMm, out ProfileContourLoop innerLoop)
        {
            innerLoop = null!;
            if (insetMm <= CadMath.Epsilon || Width <= insetMm * 2 || Height <= insetMm * 2)
            {
                return false;
            }

            if (!IsArched)
            {
                var innerLeft = Left + insetMm;
                var innerBottom = Bottom + insetMm;
                var innerRight = Right - insetMm;
                var innerTop = Top - insetMm;
                innerLoop = ProfileContourLoop.FromEdges(
                    ProfileEdge.Line(new CadPoint(innerLeft, innerBottom), new CadPoint(innerLeft, innerTop)),
                    ProfileEdge.Line(new CadPoint(innerLeft, innerTop), new CadPoint(innerRight, innerTop)),
                    ProfileEdge.Line(new CadPoint(innerRight, innerTop), new CadPoint(innerRight, innerBottom)),
                    ProfileEdge.Line(new CadPoint(innerRight, innerBottom), new CadPoint(innerLeft, innerBottom)));
                return true;
            }

            var x0 = Left + insetMm;
            var x1 = Right - insetMm;
            var y0 = Bottom + insetMm;
            var rInner = Math.Max(ArcRadius - insetMm, CadMath.Epsilon);
            if (x1 <= x0)
            {
                return false;
            }

            var springingLeft = ProfileSegment.GetPointOnUpperArc(ArcCenter, rInner, x0);
            var springingRight = ProfileSegment.GetPointOnUpperArc(ArcCenter, rInner, x1);
            if (springingLeft.Y - y0 <= insetMm)
            {
                return false;
            }

            var startAngle = CircularArc.AngleRad(ArcCenter, springingLeft);
            var endAngle = CircularArc.AngleRad(ArcCenter, springingRight);
            innerLoop = ProfileContourLoop.FromEdges(
                ProfileEdge.Line(new CadPoint(x0, y0), springingLeft),
                ProfileEdge.Arc(ArcCenter, rInner, startAngle, endAngle),
                ProfileEdge.Line(springingRight, new CadPoint(x1, y0)),
                ProfileEdge.Line(new CadPoint(x1, y0), new CadPoint(x0, y0)));
            return true;
        }
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

        var leaf = FindLeafForSash(editor, element)
            ?? editor.FrameRoot.FindLeafAt((double)element.X, (double)element.Y);
        if (leaf is null)
        {
            return false;
        }

        inner = ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness);
        inner = UpgradeInnerTopForArcGlazing(editor, inner);
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
        double frameThickness) =>
        ResolveNodeInnerOpeningBounds(leaf, frameRoot, frameThickness);

    /// <summary>
    /// Внутренний световой контур ячейки (листа или узла перед сплитом) с учётом фальца рамы и осей импостов-предков.
    /// </summary>
    public static InnerOpeningBounds ResolveNodeInnerOpeningBounds(
        FrameNode node,
        FrameNode? frameRoot,
        double frameThickness)
    {
        var inner = new InnerOpeningBounds
        {
            Left = node.X,
            Bottom = node.Y,
            Right = node.X + node.Width,
            Top = node.Y + node.Height
        };

        ApplyImpostAxisEdges(node, ref inner);

        if (frameRoot is not null)
        {
            PinFrameInnerEdges(node, frameRoot, frameThickness, ref inner);
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

    public static bool TryResolveGlassOpeningBounds(
        CadEditorViewModel editor,
        in InnerOpeningBounds inner,
        in OuterBounds outer,
        double profileThickness,
        out GlassOpeningBounds glass)
    {
        var (rectLeft, rectBottom, rectRight, rectTop) = ResolveInnerGlassFromOuter(
            outer.Left,
            outer.Bottom,
            outer.Width,
            outer.Height);

        if (!TryBuildArchedSashContours(
                editor,
                inner,
                outer,
                profileThickness,
                out _,
                out _,
                out _,
                out var innerBottomLeft,
                out _,
                out var innerBottomRight,
                out _,
                out _,
                out _,
                out _))
        {
            glass = GlassOpeningBounds.FromRect(rectLeft, rectBottom, rectRight, rectTop);
            return false;
        }

        if (editor.FrameRoot is null ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(
                editor.FrameRoot,
                editor.FrameThickness,
                out var frameArc))
        {
            glass = GlassOpeningBounds.FromRect(rectLeft, rectBottom, rectRight, rectTop);
            return false;
        }

        var chordY = editor.FrameRoot.Y + editor.FrameRoot.Height;
        var boxHeight = outer.Bottom >= chordY - CadMath.Epsilon
            ? Math.Max(inner.Top - outer.Bottom, outer.Height)
            : chordY - outer.Bottom;
        var profileWall = FrameProfileGeometry.ResolveInnerInset(outer.Width, boxHeight, profileThickness);
        var gapFromInner = Math.Max(GlassBeadGapMm, rectLeft - innerBottomLeft.X);
        var x0 = CadMath.RoundMm(innerBottomLeft.X + gapFromInner);
        var x1 = CadMath.RoundMm(innerBottomRight.X - gapFromInner);
        var y0 = CadMath.RoundMm(innerBottomLeft.Y + gapFromInner);
        if (x1 <= x0 + CadMath.Epsilon)
        {
            glass = GlassOpeningBounds.FromRect(rectLeft, rectBottom, rectRight, rectTop);
            return false;
        }

        var overlapTop = inner.PinsFrameTop ? FrameOverlapMm : OverlapMm;
        var rOuter = frameArc.InnerRadius + overlapTop;
        var rInner = Math.Max(rOuter - profileWall, CadMath.Epsilon);
        var rGlass = Math.Max(rInner - gapFromInner, CadMath.Epsilon);

        var springingLeft = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rGlass, x0);
        var springingRight = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rGlass, x1);
        if (springingLeft.Y - y0 <= gapFromInner)
        {
            glass = GlassOpeningBounds.FromRect(rectLeft, rectBottom, rectRight, rectTop);
            return false;
        }

        var leftY = springingLeft.Y;
        var rightY = springingRight.Y;
        var centerY = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rGlass, (x0 + x1) / 2.0).Y;
        var topY = CadMath.RoundMm(Math.Max(leftY, Math.Max(rightY, centerY)));

        glass = new GlassOpeningBounds
        {
            Left = x0,
            Bottom = y0,
            Right = x1,
            Top = topY,
            IsArched = true,
            ArcCenter = frameArc.Center,
            ArcRadius = rGlass,
            BottomLeft = new CadPoint(x0, y0),
            BottomRight = new CadPoint(x1, y0),
            SpringingLeft = springingLeft,
            SpringingRight = springingRight
        };
        return true;
    }

    public static bool TryResolveGlassForSash(
        CadEditorViewModel editor,
        GeometryElement sash,
        out GlassOpeningBounds glass,
        out OuterBounds outer,
        out InnerOpeningBounds inner)
    {
        inner = TryResolveInnerBounds(editor, sash, out var liveInner)
            ? liveInner
            : InnerOpeningBoundsFromElement(sash);
        outer = ExpandInnerBoundsToOuter(inner, editor);
        glass = default;
        if (inner.Width <= 0 || inner.Height <= 0 || outer.Width <= 0 || outer.Height <= 0)
        {
            return false;
        }

        TryResolveGlassOpeningBounds(editor, inner, outer, SashDrawProfileThicknessMm, out glass);
        return glass.Width > 0 && glass.Height > 0;
    }

    public static bool TryResolveFixedFrameGlassOpeningBounds(
        CadEditorViewModel editor,
        FrameNode leaf,
        out GlassOpeningBounds glass)
    {
        glass = default;
        if (editor.FrameRoot is null)
        {
            return false;
        }

        var inner = ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness);
        inner = UpgradeInnerTopForArcGlazing(editor, inner);
        if (inner.Width <= CadMath.Epsilon || inner.Height <= CadMath.Epsilon)
        {
            return false;
        }

        var gap = GlassBeadGapMm;
        var x0 = CadMath.RoundMm(inner.Left + gap);
        var x1 = CadMath.RoundMm(inner.Right - gap);
        var y0 = CadMath.RoundMm(inner.Bottom + gap);
        if (x1 <= x0 + CadMath.Epsilon)
        {
            return false;
        }

        if (ShouldUseArchedGlazingTop(editor, inner) &&
            TryBuildArchedGlassFromInnerOpening(editor, inner, gap, out glass))
        {
            return true;
        }

        return TryBuildRectangularFixedFrameGlass(inner, gap, x0, x1, y0, out glass);
    }

    /// <summary>
    /// Верх остекления следует дуге: полный проём под аркой или верхний сегмент над импостом в зоне арки.
    /// </summary>
    public static bool ShouldUseArchedGlazingTop(CadEditorViewModel editor, in InnerOpeningBounds inner)
    {
        if (editor.FrameRoot?.TopProfile?.SegmentType != SegmentType.Arc ||
            editor.FrameRoot.TopProfile.ArcHeight <= ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return false;
        }

        if (ShouldDrawArchedSash(editor, inner))
        {
            return true;
        }

        var chordY = editor.FrameRoot.Y + editor.FrameRoot.Height;
        if (inner.PinsFrameBottom)
        {
            return false;
        }

        return inner.Bottom >= chordY - CadMath.MmPrecision &&
               inner.Top > chordY + CadMath.MmPrecision;
    }

    public static InnerOpeningBounds UpgradeInnerTopForArcGlazing(
        CadEditorViewModel editor,
        InnerOpeningBounds inner)
    {
        if (!ShouldUseArchedGlazingTop(editor, inner) || editor.FrameRoot is null)
        {
            return inner;
        }

        if (TryResolveArcAwareFrameInnerTopForSpan(
                editor.FrameRoot,
                editor.FrameThickness,
                inner.Left,
                inner.Right,
                inner.Top,
                out var arcTop))
        {
            inner.Top = CadMath.RoundMm(Math.Max(inner.Top, arcTop));
        }

        return inner;
    }

    private static bool TryBuildArchedGlassFromInnerOpening(
        CadEditorViewModel editor,
        in InnerOpeningBounds inner,
        double gap,
        out GlassOpeningBounds glass)
    {
        glass = default;
        if (editor.FrameRoot is null ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(
                editor.FrameRoot,
                editor.FrameThickness,
                out var frameArc))
        {
            return false;
        }

        var chordY = editor.FrameRoot.Y + editor.FrameRoot.Height;
        var x0 = CadMath.RoundMm(inner.Left + gap);
        var x1 = CadMath.RoundMm(inner.Right - gap);
        var y0 = CadMath.RoundMm(inner.Bottom + gap);
        if (x1 <= x0 + CadMath.Epsilon)
        {
            return false;
        }

        if (y0 > chordY + CadMath.MmPrecision &&
            FrameProfileGeometry.TryResolveInnerArcHorizontalSpanAtY(
                frameArc,
                y0,
                inner.Left + gap,
                inner.Right - gap,
                out var arcLeft,
                out var arcRight))
        {
            x0 = CadMath.RoundMm(Math.Max(x0, arcLeft));
            x1 = CadMath.RoundMm(Math.Min(x1, arcRight));
            if (x1 <= x0 + CadMath.Epsilon)
            {
                return false;
            }
        }

        var rGlass = Math.Max(frameArc.InnerRadius - gap, CadMath.Epsilon);
        var springingLeft = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rGlass, x0);
        var springingRight = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rGlass, x1);
        if (springingLeft.Y - y0 <= gap)
        {
            return false;
        }

        var centerY = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rGlass, (x0 + x1) / 2.0).Y;
        var arcTopY = Math.Max(springingLeft.Y, Math.Max(springingRight.Y, centerY));
        var openingTop = inner.Top - gap;
        if (openingTop <= y0 + CadMath.Epsilon)
        {
            return false;
        }

        var topY = CadMath.RoundMm(Math.Min(arcTopY, openingTop));
        if (topY <= y0 + CadMath.Epsilon)
        {
            return false;
        }

        glass = new GlassOpeningBounds
        {
            Left = x0,
            Bottom = y0,
            Right = x1,
            Top = topY,
            IsArched = true,
            ArcCenter = frameArc.Center,
            ArcRadius = rGlass,
            BottomLeft = new CadPoint(x0, y0),
            BottomRight = new CadPoint(x1, y0),
            SpringingLeft = springingLeft,
            SpringingRight = springingRight
        };
        return true;
    }

    private static bool TryBuildRectangularFixedFrameGlass(
        InnerOpeningBounds inner,
        double gap,
        double x0,
        double x1,
        double y0,
        out GlassOpeningBounds glass)
    {
        glass = default;
        var yTop = CadMath.RoundMm(inner.Top - gap);
        if (yTop <= y0 + CadMath.Epsilon)
        {
            return false;
        }

        glass = GlassOpeningBounds.FromRect(x0, y0, x1, yTop);
        return true;
    }

    public static IEnumerable<FrameNode> EnumerateFrameLeaves(FrameNode root) => EnumerateLeaves(root);

    public static void GetStandaloneGlazingOverlapBounds(
        GeometryElement glazing,
        out double left,
        out double bottom,
        out double right,
        out double top)
    {
        var halfW = (double)glazing.WidthMm / 2.0;
        var halfH = (double)glazing.HeightMm / 2.0;
        var cx = (double)glazing.X;
        var cy = (double)glazing.Y;
        left = cx - halfW;
        right = cx + halfW;
        bottom = cy - halfH;
        top = cy + halfH;
    }

    public static bool TryMeasureLeafGlazingOverlap(
        CadEditorViewModel editor,
        FrameNode leaf,
        GeometryElement glazing,
        out double overlapArea)
    {
        overlapArea = 0;
        if (editor.FrameRoot is null || glazing.HostSashId is not null)
        {
            return false;
        }

        GetStandaloneGlazingOverlapBounds(glazing, out var gLeft, out var gBottom, out var gRight, out var gTop);
        var inner = ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness);
        var iLeft = Math.Max(inner.Left, gLeft);
        var iRight = Math.Min(inner.Right, gRight);
        var iBottom = Math.Max(inner.Bottom, gBottom);
        var iTop = Math.Min(inner.Top, gTop);
        if (iRight <= iLeft + CadMath.Epsilon || iTop <= iBottom + CadMath.Epsilon)
        {
            return false;
        }

        overlapArea = (iRight - iLeft) * (iTop - iBottom);
        return overlapArea > CadMath.Epsilon;
    }

    public static IReadOnlyList<FrameNode> FindLeavesHostingStandaloneGlazing(
        CadEditorViewModel editor,
        GeometryElement glazing)
    {
        if (editor.FrameRoot is null || glazing.HostSashId is not null)
        {
            return [];
        }

        var matches = new List<FrameNode>();
        foreach (var leaf in EnumerateLeaves(editor.FrameRoot))
        {
            if (!leaf.IsLeaf)
            {
                continue;
            }

            if (TryMeasureLeafGlazingOverlap(editor, leaf, glazing, out _))
            {
                matches.Add(leaf);
            }
        }

        return matches;
    }

    public static FrameNode? FindLeafForGlazing(CadEditorViewModel editor, GeometryElement glazing)
    {
        if (editor.FrameRoot is null)
        {
            return null;
        }

        if (glazing.HostSashId is null)
        {
            FrameNode? bestLeaf = null;
            var bestOverlap = 0.0;
            foreach (var leaf in FindLeavesHostingStandaloneGlazing(editor, glazing))
            {
                if (!TryMeasureLeafGlazingOverlap(editor, leaf, glazing, out var overlap))
                {
                    continue;
                }

                if (overlap > bestOverlap)
                {
                    bestOverlap = overlap;
                    bestLeaf = leaf;
                }
            }

            if (bestLeaf is not null)
            {
                return bestLeaf;
            }
        }

        var cx = (double)glazing.X;
        var cy = (double)glazing.Y;
        return editor.FrameRoot.FindLeafAt(cx, cy);
    }

    public static void GetSashOverlapBounds(
        GeometryElement sash,
        out double left,
        out double bottom,
        out double right,
        out double top) =>
        GetStandaloneGlazingOverlapBounds(sash, out left, out bottom, out right, out top);

    public static bool TryMeasureLeafSashOverlap(
        CadEditorViewModel editor,
        FrameNode leaf,
        GeometryElement sash,
        out double overlapArea)
    {
        overlapArea = 0;
        if (editor.FrameRoot is null || sash.Kind != GeometryElementKind.Sash)
        {
            return false;
        }

        GetSashOverlapBounds(sash, out var sLeft, out var sBottom, out var sRight, out var sTop);
        var inner = ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness);
        var iLeft = Math.Max(inner.Left, sLeft);
        var iRight = Math.Min(inner.Right, sRight);
        var iBottom = Math.Max(inner.Bottom, sBottom);
        var iTop = Math.Min(inner.Top, sTop);
        if (iRight <= iLeft + CadMath.Epsilon || iTop <= iBottom + CadMath.Epsilon)
        {
            return false;
        }

        overlapArea = (iRight - iLeft) * (iTop - iBottom);
        return overlapArea > CadMath.Epsilon;
    }

    public static IReadOnlyList<FrameNode> FindLeavesHostingSash(
        CadEditorViewModel editor,
        GeometryElement sash)
    {
        if (editor.FrameRoot is null || sash.Kind != GeometryElementKind.Sash)
        {
            return [];
        }

        var matches = new List<FrameNode>();
        foreach (var leaf in EnumerateLeaves(editor.FrameRoot))
        {
            if (!leaf.IsLeaf)
            {
                continue;
            }

            if (TryMeasureLeafSashOverlap(editor, leaf, sash, out _))
            {
                matches.Add(leaf);
            }
        }

        return matches;
    }

    public static FrameNode? FindLeafForSash(CadEditorViewModel editor, GeometryElement sash)
    {
        if (editor.FrameRoot is null || sash.Kind != GeometryElementKind.Sash)
        {
            return null;
        }

        FrameNode? bestLeaf = null;
        var bestOverlap = 0.0;
        foreach (var leaf in FindLeavesHostingSash(editor, sash))
        {
            if (!TryMeasureLeafSashOverlap(editor, leaf, sash, out var overlap))
            {
                continue;
            }

            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                bestLeaf = leaf;
            }
        }

        return bestLeaf ?? editor.FrameRoot.FindLeafAt((double)sash.X, (double)sash.Y);
    }

    private static IEnumerable<FrameNode> EnumerateLeaves(FrameNode node)
    {
        if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                foreach (var leaf in EnumerateLeaves(cell))
                {
                    yield return leaf;
                }
            }

            yield break;
        }

        if (node.IsLeaf)
        {
            yield return node;
            yield break;
        }

        if (node.ChildA is not null)
        {
            foreach (var leaf in EnumerateLeaves(node.ChildA))
            {
                yield return leaf;
            }
        }

        if (node.ChildB is not null)
        {
            foreach (var leaf in EnumerateLeaves(node.ChildB))
            {
                yield return leaf;
            }
        }
    }

    public static bool TryResolveGlassForGlazingElement(
        CadEditorViewModel editor,
        GeometryElement glazing,
        IReadOnlyList<GeometryElement> elements,
        out GlassOpeningBounds glass)
    {
        glass = default;
        if (glazing.Kind != GeometryElementKind.Glazing)
        {
            return false;
        }

        if (glazing.HostSashId is Guid sashId)
        {
            var sash = elements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
            if (sash is not null && TryResolveGlassForSash(editor, sash, out glass, out _, out _))
            {
                return true;
            }
        }
        else if (editor.FrameRoot is not null)
        {
            var leaf = FindLeafForGlazing(editor, glazing);
            if (leaf is not null &&
                TryResolveFixedFrameGlassOpeningBounds(editor, leaf, out glass))
            {
                return glass.Width > CadMath.Epsilon && glass.Height > CadMath.Epsilon;
            }
        }

        var halfW = (double)glazing.WidthMm / 2.0;
        var halfH = (double)glazing.HeightMm / 2.0;
        var cx = (double)glazing.X;
        var cy = (double)glazing.Y;
        glass = GlassOpeningBounds.FromRect(cx - halfW, cy - halfH, cx + halfW, cy + halfH);
        return halfW > CadMath.Epsilon && halfH > CadMath.Epsilon;
    }

    public static double ResolveArcSpringingChordY(CadEditorViewModel editor) =>
        editor.FrameRoot is null ? 0 : editor.FrameRoot.Y + editor.FrameRoot.Height;

    public static double ResolveStraightVerticalZoneTopY(
        CadEditorViewModel editor,
        in InnerOpeningBounds inner,
        in OuterBounds outer)
    {
        if (ShouldDrawArchedSash(editor, inner))
        {
            return ResolveArcSpringingChordY(editor) - HardwareArcTransitionMarginMm;
        }

        return outer.Top;
    }

    public static double ClampHandleCenterY(
        CadEditorViewModel editor,
        in InnerOpeningBounds inner,
        in OuterBounds outer,
        in GlassOpeningBounds glass,
        double axisX,
        double preferredCenterY,
        double handleHalfHeight)
    {
        var minCenterY = glass.Bottom + handleHalfHeight;
        var glassTopAtAxis = glass.ResolveTopAtX(axisX);
        var preferred = Math.Min(preferredCenterY, (glass.Bottom + glassTopAtAxis) / 2.0);
        var maxCenterY = ResolveStraightVerticalZoneTopY(editor, inner, outer) - handleHalfHeight;
        if (maxCenterY < minCenterY)
        {
            return minCenterY;
        }

        return Math.Clamp(preferred, minCenterY, maxCenterY);
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

    public static bool ContainsSashProfileAtPoint(
        CadEditorViewModel editor,
        GeometryElement element,
        CadPoint point)
    {
        if (element.Kind != GeometryElementKind.Sash)
        {
            return false;
        }

        var inner = TryResolveInnerBounds(editor, element, out var liveInner)
            ? liveInner
            : InnerOpeningBoundsFromElement(element);
        var outer = ExpandInnerBoundsToOuter(inner, editor);
        if (outer.Width <= 0 || outer.Height <= 0)
        {
            return false;
        }

        var profileThickness = SashDrawProfileThicknessMm;
        if (TryBuildArchedSashContours(
                editor,
                inner,
                outer,
                profileThickness,
                out var outerLoop,
                out var innerLoop,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            return ProfileBandHitTest.ContainsPoint(outerLoop, innerLoop, point);
        }

        var halfThickness = FrameProfileGeometry.ResolveProfileThickness(outer.Width, outer.Height, profileThickness) / 2.0;
        if (halfThickness <= CadMath.Epsilon)
        {
            return ContainsOuterBounds(element, point.X, point.Y, editor);
        }

        return ProfileBandHitTest.ContainsPointInRectangularBand(
            outer.Left,
            outer.Bottom,
            outer.Right,
            outer.Top,
            outer.Left + halfThickness,
            outer.Bottom + halfThickness,
            outer.Right - halfThickness,
            outer.Top - halfThickness,
            point);
    }

    public static bool SashProfileIntersectsRect(
        CadEditorViewModel editor,
        GeometryElement element,
        CadRect selection)
    {
        if (element.Kind != GeometryElementKind.Sash)
        {
            return false;
        }

        var inner = TryResolveInnerBounds(editor, element, out var liveInner)
            ? liveInner
            : InnerOpeningBoundsFromElement(element);
        var outer = ExpandInnerBoundsToOuter(inner, editor);
        if (outer.Width <= 0 || outer.Height <= 0)
        {
            return false;
        }

        var profileThickness = SashDrawProfileThicknessMm;
        if (TryBuildArchedSashContours(
                editor,
                inner,
                outer,
                profileThickness,
                out var outerLoop,
                out var innerLoop,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            return ProfileBandHitTest.IntersectsRect(outerLoop, innerLoop, selection);
        }

        var halfThickness = FrameProfileGeometry.ResolveProfileThickness(outer.Width, outer.Height, profileThickness) / 2.0;
        if (halfThickness <= CadMath.Epsilon)
        {
            return selection.Intersects(new CadRect(outer.Left, outer.Top, outer.Width, outer.Height));
        }

        return ProfileBandHitTest.RectIntersectsRectangularBand(
            selection,
            outer.Left,
            outer.Bottom,
            outer.Right,
            outer.Top,
            outer.Left + halfThickness,
            outer.Bottom + halfThickness,
            outer.Right - halfThickness,
            outer.Top - halfThickness);
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
            if (TryResolveArcAwareFrameInnerTop(
                    frameRoot,
                    frameThickness,
                    inner.Left,
                    inner.Right,
                    frameInnerTop,
                    out var arcAwareTop))
            {
                inner.Top = arcAwareTop;
            }
            else
            {
                inner.Top = frameInnerTop;
            }

            inner.PinsFrameTop = true;
        }
    }

    public static bool TryResolveArcAwareFrameInnerTopForSpan(
        FrameNode frameRoot,
        double frameThickness,
        double innerLeft,
        double innerRight,
        double flatInnerTop,
        out double topY) =>
        TryResolveArcAwareFrameInnerTop(
            frameRoot,
            frameThickness,
            innerLeft,
            innerRight,
            flatInnerTop,
            out topY);

    private static bool TryResolveArcAwareFrameInnerTop(
        FrameNode frameRoot,
        double frameThickness,
        double innerLeft,
        double innerRight,
        double flatInnerTop,
        out double topY)
    {
        topY = flatInnerTop;
        if (!FrameProfileGeometry.TryGetArchedFrameInnerArc(frameRoot, frameThickness, out var arc))
        {
            return false;
        }

        var leftY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, innerLeft).Y;
        var rightY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, innerRight).Y;
        var centerY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, (innerLeft + innerRight) / 2.0).Y;
        var arcY = Math.Max(leftY, Math.Max(rightY, centerY));
        topY = CadMath.RoundMm(Math.Max(flatInnerTop, arcY));
        return true;
    }

    public static bool ShouldDrawArchedSash(CadEditorViewModel editor, in InnerOpeningBounds inner) =>
        inner.PinsFrameTop &&
        editor.FrameRoot?.TopProfile?.SegmentType == SegmentType.Arc &&
        editor.FrameRoot.TopProfile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon;

    public static bool ShouldUseArchedSashProfile(CadEditorViewModel editor, in InnerOpeningBounds inner) =>
        ShouldUseArchedGlazingTop(editor, inner);

    public static bool TryBuildArchedSashContours(
        CadEditorViewModel editor,
        in InnerOpeningBounds lightInner,
        in OuterBounds outer,
        double profileThickness,
        out ProfileContourLoop outerLoop,
        out ProfileContourLoop innerLoop,
        out CadPoint outerBottomLeft,
        out CadPoint innerBottomLeft,
        out CadPoint outerBottomRight,
        out CadPoint innerBottomRight,
        out CadPoint outerSpringingLeft,
        out CadPoint innerSpringingLeft,
        out CadPoint outerSpringingRight,
        out CadPoint innerSpringingRight)
    {
        outerLoop = innerLoop = null!;
        outerBottomLeft = innerBottomLeft = outerBottomRight = innerBottomRight = default;
        outerSpringingLeft = innerSpringingLeft = outerSpringingRight = innerSpringingRight = default;

        if (editor.FrameRoot is null ||
            !ShouldUseArchedSashProfile(editor, lightInner) ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(
                editor.FrameRoot,
                editor.FrameThickness,
                out var frameArc))
        {
            return false;
        }

        var chordY = editor.FrameRoot.Y + editor.FrameRoot.Height;
        if (outer.Width <= CadMath.Epsilon)
        {
            return false;
        }

        var upgradedInner = UpgradeInnerTopForArcGlazing(editor, lightInner);
        if (outer.Bottom >= chordY - CadMath.Epsilon)
        {
            return TryBuildArchedSegmentSashContours(
                frameArc,
                chordY,
                upgradedInner,
                outer,
                profileThickness,
                out outerLoop,
                out innerLoop,
                out outerBottomLeft,
                out innerBottomLeft,
                out outerBottomRight,
                out innerBottomRight,
                out outerSpringingLeft,
                out innerSpringingLeft,
                out outerSpringingRight,
                out innerSpringingRight);
        }

        if (!ShouldDrawArchedSash(editor, upgradedInner))
        {
            return false;
        }

        return TryBuildFullArchedSashContours(
            frameArc,
            chordY,
            upgradedInner,
            outer,
            profileThickness,
            out outerLoop,
            out innerLoop,
            out outerBottomLeft,
            out innerBottomLeft,
            out outerBottomRight,
            out innerBottomRight,
            out outerSpringingLeft,
            out innerSpringingLeft,
            out outerSpringingRight,
            out innerSpringingRight);
    }

    private static bool TryBuildFullArchedSashContours(
        ArcSpringingGeometry frameArc,
        double chordY,
        in InnerOpeningBounds lightInner,
        in OuterBounds outer,
        double profileThickness,
        out ProfileContourLoop outerLoop,
        out ProfileContourLoop innerLoop,
        out CadPoint outerBottomLeft,
        out CadPoint innerBottomLeft,
        out CadPoint outerBottomRight,
        out CadPoint innerBottomRight,
        out CadPoint outerSpringingLeft,
        out CadPoint innerSpringingLeft,
        out CadPoint outerSpringingRight,
        out CadPoint innerSpringingRight)
    {
        outerLoop = innerLoop = null!;
        outerBottomLeft = innerBottomLeft = outerBottomRight = innerBottomRight = default;
        outerSpringingLeft = innerSpringingLeft = outerSpringingRight = innerSpringingRight = default;

        var overlapTop = lightInner.PinsFrameTop ? FrameOverlapMm : OverlapMm;
        var rOuter = frameArc.InnerRadius + overlapTop;
        var boxHeight = chordY - outer.Bottom;
        var profileWall = FrameProfileGeometry.ResolveInnerInset(outer.Width, boxHeight, profileThickness);
        if (profileWall <= CadMath.Epsilon)
        {
            return false;
        }

        return TryBuildArchedSashProfileLoops(
            frameArc,
            rOuter,
            profileWall,
            outer.Left,
            outer.Right,
            outer.Bottom,
            out outerLoop,
            out innerLoop,
            out outerBottomLeft,
            out innerBottomLeft,
            out outerBottomRight,
            out innerBottomRight,
            out outerSpringingLeft,
            out innerSpringingLeft,
            out outerSpringingRight,
            out innerSpringingRight);
    }

    private static bool TryBuildArchedSegmentSashContours(
        ArcSpringingGeometry frameArc,
        double chordY,
        in InnerOpeningBounds lightInner,
        in OuterBounds outer,
        double profileThickness,
        out ProfileContourLoop outerLoop,
        out ProfileContourLoop innerLoop,
        out CadPoint outerBottomLeft,
        out CadPoint innerBottomLeft,
        out CadPoint outerBottomRight,
        out CadPoint innerBottomRight,
        out CadPoint outerSpringingLeft,
        out CadPoint innerSpringingLeft,
        out CadPoint outerSpringingRight,
        out CadPoint innerSpringingRight)
    {
        outerLoop = innerLoop = null!;
        outerBottomLeft = innerBottomLeft = outerBottomRight = innerBottomRight = default;
        outerSpringingLeft = innerSpringingLeft = outerSpringingRight = innerSpringingRight = default;

        var overlapTop = lightInner.PinsFrameTop ? FrameOverlapMm : OverlapMm;
        var rOuter = frameArc.InnerRadius + overlapTop;
        var segmentHeight = Math.Max(lightInner.Top - outer.Bottom, outer.Height);
        var profileWall = FrameProfileGeometry.ResolveInnerInset(outer.Width, segmentHeight, profileThickness);
        if (profileWall <= CadMath.Epsilon)
        {
            return false;
        }

        var x0 = outer.Left;
        var x1 = outer.Right;
        var y0 = outer.Bottom;
        if (y0 > chordY + CadMath.MmPrecision &&
            FrameProfileGeometry.TryResolveInnerArcHorizontalSpanAtY(
                frameArc,
                y0,
                outer.Left,
                outer.Right,
                out var arcLeft,
                out var arcRight))
        {
            x0 = Math.Max(x0, arcLeft);
            x1 = Math.Min(x1, arcRight);
        }

        if (x1 <= x0 + CadMath.Epsilon)
        {
            return false;
        }

        return TryBuildArchedSashProfileLoops(
            frameArc,
            rOuter,
            profileWall,
            x0,
            x1,
            y0,
            out outerLoop,
            out innerLoop,
            out outerBottomLeft,
            out innerBottomLeft,
            out outerBottomRight,
            out innerBottomRight,
            out outerSpringingLeft,
            out innerSpringingLeft,
            out outerSpringingRight,
            out innerSpringingRight);
    }

    private static bool TryBuildArchedSashProfileLoops(
        ArcSpringingGeometry frameArc,
        double rOuter,
        double profileWall,
        double x0,
        double x1,
        double y0,
        out ProfileContourLoop outerLoop,
        out ProfileContourLoop innerLoop,
        out CadPoint outerBottomLeft,
        out CadPoint innerBottomLeft,
        out CadPoint outerBottomRight,
        out CadPoint innerBottomRight,
        out CadPoint outerSpringingLeft,
        out CadPoint innerSpringingLeft,
        out CadPoint outerSpringingRight,
        out CadPoint innerSpringingRight)
    {
        outerLoop = innerLoop = null!;
        outerBottomLeft = innerBottomLeft = outerBottomRight = innerBottomRight = default;
        outerSpringingLeft = innerSpringingLeft = outerSpringingRight = innerSpringingRight = default;

        var rInner = Math.Max(rOuter - profileWall, CadMath.Epsilon);
        var xi0 = x0 + profileWall;
        var xi1 = x1 - profileWall;
        var yi0 = y0 + profileWall;
        if (xi1 <= xi0 + CadMath.Epsilon)
        {
            return false;
        }

        outerBottomLeft = new CadPoint(x0, y0);
        outerBottomRight = new CadPoint(x1, y0);
        innerBottomLeft = new CadPoint(xi0, yi0);
        innerBottomRight = new CadPoint(xi1, yi0);

        outerSpringingLeft = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rOuter, x0);
        outerSpringingRight = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rOuter, x1);
        innerSpringingLeft = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rInner, xi0);
        innerSpringingRight = ProfileSegment.GetPointOnUpperArc(frameArc.Center, rInner, xi1);

        var outerStartAngle = CircularArc.AngleRad(frameArc.Center, outerSpringingLeft);
        var outerEndAngle = CircularArc.AngleRad(frameArc.Center, outerSpringingRight);
        var innerStartAngle = CircularArc.AngleRad(frameArc.Center, innerSpringingLeft);
        var innerEndAngle = CircularArc.AngleRad(frameArc.Center, innerSpringingRight);

        outerLoop = ProfileContourLoop.FromEdges(
            ProfileEdge.Line(new CadPoint(x0, y0), outerSpringingLeft),
            ProfileEdge.Arc(frameArc.Center, rOuter, outerStartAngle, outerEndAngle),
            ProfileEdge.Line(outerSpringingRight, new CadPoint(x1, y0)),
            ProfileEdge.Line(new CadPoint(x1, y0), new CadPoint(x0, y0)));

        innerLoop = ProfileContourLoop.FromEdges(
            ProfileEdge.Line(new CadPoint(xi0, yi0), innerSpringingLeft),
            ProfileEdge.Arc(frameArc.Center, rInner, innerStartAngle, innerEndAngle),
            ProfileEdge.Line(innerSpringingRight, new CadPoint(xi1, yi0)),
            ProfileEdge.Line(new CadPoint(xi1, yi0), new CadPoint(xi0, yi0)));

        return true;
    }

    public static bool TryResolveInnerArcTopY(
        CadEditorViewModel editor,
        in InnerOpeningBounds inner,
        double worldX,
        out double topY)
    {
        topY = inner.Top;
        if (editor.FrameRoot is null ||
            !inner.PinsFrameTop ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(
                editor.FrameRoot,
                editor.FrameThickness,
                out var arc))
        {
            return false;
        }

        topY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, worldX).Y;
        return true;
    }

    public static CadPoint ResolveInnerCorner(CadEditorViewModel editor, in InnerOpeningBounds inner, double x, double fallbackY)
    {
        if (TryResolveInnerArcTopY(editor, inner, x, out var arcY))
        {
            return new CadPoint(x, arcY);
        }

        return new CadPoint(x, fallbackY);
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