using System.Collections.Generic;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Services;
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
    private const double LeafSashOverlapMinAreaMm2 = 100.0;
    private const double HardwareArcTransitionMarginMm = 12.0;

    /// <summary>
    /// Технологический отступ остекления от внутреннего контура рамы.
    /// Без створки (глухое окно) — 0, иначе зазор под штапик/фальц.
    /// </summary>
    public static double ResolveGlazingTechnologyGap(bool hasSashInOpening) =>
        hasSashInOpening ? GlassBeadGapMm : 0.0;

    public static bool HasSashInLeaf(CadEditorViewModel editor, FrameNode leaf)
    {
        if (editor.FrameRoot is null || !leaf.IsLeaf)
        {
            return false;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.Sash)
            {
                continue;
            }

            if (TryMeasureLeafSashOverlap(editor, leaf, element, out var overlap)
                && overlap >= LeafSashOverlapMinAreaMm2)
            {
                return true;
            }
        }

        return false;
    }

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
        public bool IsTriangular;
        public CadPoint TriangleBl;
        public CadPoint TriangleBr;
        public CadPoint TriangleTop;

        public double Width => Right - Left;
        public double Height => Top - Bottom;

        public double ResolveTopAtX(double worldX) =>
            IsTriangular
                ? FrameTriangleGeometry.InterpolateTopAtX(TriangleBl, TriangleBr, TriangleTop, worldX)
                : Top;
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
        public bool IsTriangular { get; init; }
        public CadPoint ArcCenter { get; init; }
        public double ArcRadius { get; init; }
        public CadPoint BottomLeft { get; init; }
        public CadPoint BottomRight { get; init; }
        public CadPoint SpringingLeft { get; init; }
        public CadPoint SpringingRight { get; init; }
        public CadPoint TopApex { get; init; }

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
            IsTriangular
                ? FrameTriangleGeometry.InterpolateTopAtX(BottomLeft, BottomRight, TopApex, worldX)
                : IsArched
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
            if (IsTriangular)
            {
                return ProfileContourLoop.FromEdges(
                    ProfileEdge.Line(BottomLeft, TopApex),
                    ProfileEdge.Line(TopApex, BottomRight),
                    ProfileEdge.Line(BottomRight, BottomLeft));
            }

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
            if (insetMm <= CadMath.Epsilon)
            {
                return false;
            }

            if (IsTriangular)
            {
                var outer = (BottomLeft, BottomRight, TopApex);
                var inner = FrameTriangleGeometry.InsetTriangle(outer, insetMm);
                innerLoop = ProfileContourLoop.FromEdges(
                    ProfileEdge.Line(inner.Bl, inner.Top),
                    ProfileEdge.Line(inner.Top, inner.Br),
                    ProfileEdge.Line(inner.Br, inner.Bl));
                return true;
            }

            if (Width <= insetMm * 2 || Height <= insetMm * 2)
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
    /// Внешний контур: 8 мм на импост, 18 мм на внешнюю раму / переходной профиль (Host Frame).
    /// </summary>
    public static OuterBounds ExpandInnerBoundsToOuter(in InnerOpeningBounds inner, CadEditorViewModel? editor = null)
    {
        var overlapLeft = inner.PinsFrameLeft ? FrameOverlapMm : OverlapMm;
        var overlapRight = inner.PinsFrameRight ? FrameOverlapMm : OverlapMm;
        var overlapBottom = inner.PinsFrameBottom ? FrameOverlapMm : OverlapMm;
        var overlapTop = inner.PinsFrameTop ? FrameOverlapMm : OverlapMm;

        if (editor?.FrameRoot is not null)
        {
            var leaf = editor.FrameRoot.FindLeafAt(
                (inner.Left + inner.Right) / 2.0,
                (inner.Bottom + inner.Top) / 2.0);

            if (editor.FrameRoot.IsFictitiousContainer && leaf is not null)
            {
                // Переходной профиль — неподвижное основание: створка не выходит за внутреннюю полку.
                if (TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: true, right: false, bottom: false, top: false))
                {
                    overlapLeft = 0;
                }

                if (TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: false, right: true, bottom: false, top: false))
                {
                    overlapRight = 0;
                }

                if (TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: false, right: false, bottom: true, top: false))
                {
                    overlapBottom = 0;
                }

                if (TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: false, right: false, bottom: false, top: true))
                {
                    overlapTop = 0;
                }
            }

            if (!editor.FrameRoot.IsFictitiousContainer)
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
            else if (leaf is not null)
            {
                // Края, совпадающие с внутренней полкой переходного профиля (без pin-флага).
                if (!inner.PinsFrameLeft
                    && TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: true, right: false, bottom: false, top: false))
                {
                    overlapLeft = 0;
                }

                if (!inner.PinsFrameRight
                    && TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: false, right: true, bottom: false, top: false))
                {
                    overlapRight = 0;
                }

                if (!inner.PinsFrameBottom
                    && TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: false, right: false, bottom: true, top: false))
                {
                    overlapBottom = 0;
                }

                if (!inner.PinsFrameTop
                    && TransitionAdjacentFrameGeometry.IsTransitionHostEdge(editor, leaf, left: false, right: false, bottom: false, top: true))
                {
                    overlapTop = 0;
                }
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
        if (editor.FrameRoot is null
            || element.Kind is not (GeometryElementKind.Sash or GeometryElementKind.SlidingSash))
        {
            return false;
        }

        var leaf = ResolveSashHostLeaf(editor, element);
        if (leaf is { IsTerminalLeaf: true })
        {
            inner = ResolveSashPlacementInnerBounds(editor, leaf, (double)element.X, (double)element.Y);
            if (inner.Width <= CadMath.Epsilon || inner.Height <= CadMath.Epsilon)
            {
                inner = InnerOpeningBoundsFromElement(element);
            }

            inner = UpgradeInnerTopForArcGlazing(editor, inner);
            return inner.Width > 0 && inner.Height > 0;
        }

        inner = InnerOpeningBoundsFromElement(element);
        inner = UpgradeInnerTopForArcGlazing(editor, inner);
        return inner.Width > 0 && inner.Height > 0;
    }

    /// <summary>
    /// Терминальная ячейка для створки: тот же overlap-поиск, что у ghost-превью.
    /// </summary>
    public static FrameNode? ResolveSashPlacementLeafAtPoint(
        CadEditorViewModel editor,
        double worldX,
        double worldY)
    {
        var leaf = editor.FrameRoot?.FindLeafAt(worldX, worldY);
        return leaf is { IsTerminalLeaf: true } ? leaf : null;
    }

    /// <summary>
    /// Лист-хост створки: приоритет HostFrameNodeId терминальной ячейки, затем FindLeafAt по центру.
    /// </summary>
    public static FrameNode? ResolveSashHostLeaf(CadEditorViewModel editor, GeometryElement sash)
    {
        if (editor.FrameRoot is null)
        {
            return null;
        }

        if (sash.HostFrameNodeId is Guid hostId
            && editor.FrameRoot.FindNodeById(hostId) is FrameNode boundLeaf
            && boundLeaf.IsTerminalLeaf)
        {
            return boundLeaf;
        }

        return ResolveSashPlacementLeafAtPoint(editor, (double)sash.X, (double)sash.Y);
    }

    /// <summary>
    /// Внутренний проём створки в терминальной ячейке (квадрант сетки + рама), как у ghost-превью.
    /// </summary>
    public static InnerOpeningBounds ResolveSashPlacementInnerBounds(
        CadEditorViewModel editor,
        FrameNode leaf,
        double probeX,
        double probeY)
    {
        if (!leaf.IsTerminalLeaf)
        {
            return default;
        }

        var leafRight = leaf.X + leaf.Width;
        var leafTop = leaf.Y + leaf.Height;

        if (editor.FrameRoot?.Shape == ConstructionShape.Triangle)
        {
            return ResolveLeafCellAxisBoundsAtPoint(
                leaf,
                editor.FrameRoot,
                editor.FrameThickness,
                probeX,
                probeY);
        }

        if (editor.FrameRoot?.IsFictitiousContainer == true)
        {
            var cell = ResolveLeafCellAxisBoundsAtPoint(
                leaf,
                editor.FrameRoot,
                editor.FrameThickness,
                probeX,
                probeY);
            var hostInner = ResolveLeafInnerOpeningBounds(editor, leaf);

            var left = Math.Max(leaf.X, Math.Max(cell.Left, hostInner.Left));
            var bottom = Math.Max(leaf.Y, Math.Max(cell.Bottom, hostInner.Bottom));
            var right = Math.Min(leafRight, Math.Min(cell.Right, hostInner.Right));
            var top = Math.Min(leafTop, Math.Min(cell.Top, hostInner.Top));

            // Подрезка по телу переходного профиля в той же/соседней ячейке:
            // световой проём никогда не заходит на внутреннюю рабочую грань переходника.
            var pinsLeft = hostInner.PinsFrameLeft;
            var pinsRight = hostInner.PinsFrameRight;
            var pinsBottom = hostInner.PinsFrameBottom;
            var pinsTop = hostInner.PinsFrameTop;
            TransitionAdjacentFrameGeometry.ClipPlacementOpeningAtTransitionHosts(
                editor,
                leaf,
                probeX,
                probeY,
                ref left,
                ref bottom,
                ref right,
                ref top,
                ref pinsLeft,
                ref pinsBottom,
                ref pinsRight,
                ref pinsTop);

            if (right <= left + CadMath.Epsilon || top <= bottom + CadMath.Epsilon)
            {
                return default;
            }

            return RoundBounds(new InnerOpeningBounds
            {
                Left = left,
                Bottom = bottom,
                Right = right,
                Top = top,
                PinsFrameLeft = pinsLeft,
                PinsFrameRight = pinsRight,
                PinsFrameBottom = pinsBottom,
                PinsFrameTop = pinsTop
            });
        }

        return ResolveLeafInnerOpeningBounds(editor, leaf);
    }

    public static (double X, double Y, double Width, double Height) ResolveSashPlacementBounds(
        CadEditorViewModel editor,
        FrameNode leaf,
        double probeX,
        double probeY)
    {
        var inner = ResolveSashPlacementInnerBounds(editor, leaf, probeX, probeY);
        return (inner.Left, inner.Bottom, inner.Width, inner.Height);
    }

    public static (double X, double Y, double Width, double Height) ResolveLeafInnerOpening(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness)
    {
        var inner = ResolveLeafInnerOpeningBounds(leaf, frameRoot, frameThickness);
        return (inner.Left, inner.Bottom, inner.Width, inner.Height);
    }

    /// <summary>
    /// Внутренний проём листа с учётом переходных профилей как Host Frame.
    /// </summary>
    public static (double X, double Y, double Width, double Height) ResolveLeafInnerOpening(
        CadEditorViewModel editor,
        FrameNode leaf)
    {
        var inner = ResolveLeafInnerOpeningBounds(editor, leaf);
        return (inner.Left, inner.Bottom, inner.Width, inner.Height);
    }

    public static InnerOpeningBounds ResolveLeafInnerOpeningBounds(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        bool includeExtremeImposts = true) =>
        ResolveNodeInnerOpeningBounds(leaf, frameRoot, frameThickness, includeExtremeImposts);

    /// <summary>
    /// Внутренний проём с учётом примыкающих переходных профилей (Host Frame).
    /// </summary>
    public static InnerOpeningBounds ResolveLeafInnerOpeningBounds(
        CadEditorViewModel editor,
        FrameNode leaf,
        bool includeExtremeImposts = true)
    {
        if (editor.FrameRoot is null)
        {
            return ResolveLeafInnerOpeningBounds(leaf, null, editor.FrameThickness, includeExtremeImposts);
        }

        return TransitionAdjacentFrameGeometry.ResolveHostInnerOpeningBounds(
            editor,
            leaf,
            editor.FrameRoot,
            editor.FrameThickness,
            includeExtremeImposts);
    }

    /// <summary>
    /// Границы ячейки по осям сетки конструкции (без объёма фиктивной коробки).
    /// Используется для крайних импостов / нащельника: чистые осевые линии импостов
    /// и внешнего контура, без вычитания толщины профиля рамы.
    /// </summary>
    public static InnerOpeningBounds ResolveLeafCellAxisBounds(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        bool includeExtremeImposts = false)
    {
        var inner = new InnerOpeningBounds
        {
            Left = leaf.X,
            Bottom = leaf.Y,
            Right = leaf.X + leaf.Width,
            Top = leaf.Y + leaf.Height
        };

        ApplyCellGridAxisEdges(leaf, ref inner);

        if (frameRoot is not null)
        {
            if (frameRoot.IsFictitiousContainer)
            {
                PinFictitiousGridAxisEdges(leaf, frameRoot, ref inner);
            }
            else
            {
                PinFrameInnerEdges(leaf, frameRoot, frameThickness, ref inner);
            }
        }

        if (leaf.IsLeaf && includeExtremeImposts)
        {
            ExtremeImpostGeometry.ApplyExtremeImpostInsets(leaf, ref inner);
        }

        return RoundBounds(inner);
    }

    /// <summary>
    /// Границы ячейки по осям сетки с учётом сквозных импостов и квадранта под курсором.
    /// Горизонтальные/вертикальные линии импостов делят всё координатное пространство,
    /// даже если физический сплит существует только в соседней колонке.
    /// </summary>
    public static InnerOpeningBounds ResolveLeafCellAxisBoundsAtPoint(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        double worldX,
        double worldY,
        bool includeExtremeImposts = false)
    {
        var inner = ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness, includeExtremeImposts);
        if (frameRoot is null)
        {
            return inner;
        }

        ApplyCrossCuttingGridAxes(leaf, frameRoot, worldX, worldY, ref inner);
        ApplyTriangularBoundsAtPoint(frameRoot, worldX, ref inner);
        return RoundBounds(inner);
    }

    /// <summary>
    /// Опорный контур для размещения элементов в пунктирной ячейке (квадрант + нащельник).
    /// </summary>
    public static (double OriginX, double OriginY, double Width, double Height) ResolvePlacementBoundsAtPoint(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        double worldX,
        double worldY)
    {
        if (frameRoot?.IsFictitiousContainer == true)
        {
            var cell = ResolveLeafCellAxisBoundsAtPoint(leaf, frameRoot, frameThickness, worldX, worldY);
            return leaf.ResolvePlacementBoundsForCell(cell.Left, cell.Bottom, cell.Right, cell.Top);
        }

        if (frameRoot?.Shape == ConstructionShape.Triangle)
        {
            var cell = ResolveLeafCellAxisBoundsAtPoint(leaf, frameRoot, frameThickness, worldX, worldY);
            if (cell.Width <= CadMath.Epsilon || cell.Height <= CadMath.Epsilon)
            {
                return (cell.Left, cell.Bottom, 0, 0);
            }

            return (cell.Left, cell.Bottom, cell.Width, cell.Height);
        }

        return leaf.ResolveNestedOpeningBounds();
    }

    /// <summary>
    /// Опорный контур дверного профиля («65 компланарная Т»): сквозные горизонтальные
    /// оси разметки игнорируются, вертикальный пролёт — от низа до верха рамы.
    /// Горизонтальные границы берутся из квадранта под курсором (колонка сохраняется).
    /// </summary>
    public static (double OriginX, double OriginY, double Width, double Height) ResolveFullHeightPlacementBoundsAtPoint(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        double worldX,
        double worldY)
    {
        if (frameRoot?.IsFictitiousContainer != true)
        {
            return ResolvePlacementBoundsAtPoint(leaf, frameRoot, frameThickness, worldX, worldY);
        }

        var cell = ResolveLeafCellAxisBoundsAtPoint(leaf, frameRoot, frameThickness, worldX, worldY);
        return leaf.ResolvePlacementBoundsForCell(
            cell.Left,
            frameRoot.Y,
            cell.Right,
            frameRoot.Y + frameRoot.Height);
    }

    /// <summary>
    /// Собирает внутренние грани крайних импостов ячейки для стыковки переходного профиля.
    /// </summary>
    public static Dictionary<ImpostEdgeAlignment, double> ResolveExtremeImpostAxisLines(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness)
    {
        var axes = new Dictionary<ImpostEdgeAlignment, double>();
        if (!leaf.IsLeaf || frameRoot is null)
        {
            return axes;
        }

        var cellAxes = ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness);
        foreach (var slot in leaf.ExtremeImposts)
        {
            axes[slot.EdgeAlignment] = ResolveExtremeImpostInnerFaceWorld(cellAxes, slot);
        }

        return axes;
    }

    /// <summary>
    /// Внутренняя грань крайнего импоста (нащельник), обращённая к переходному профилю.
    /// Профиль упирается в эту плоскость и не заходит в объём нащельника.
    /// </summary>
    internal static double ResolveExtremeImpostInnerFaceWorld(
        in SashGeometry.InnerOpeningBounds cellAxes,
        ExtremeImpostSlot slot)
    {
        var axis = ExtremeImpostGeometry.ResolveAxisWorld(
            cellAxes,
            slot.EdgeAlignment,
            slot.ThicknessMm,
            slot.OffsetMm);
        var half = slot.ThicknessMm / 2.0;
        return slot.EdgeAlignment switch
        {
            ImpostEdgeAlignment.Left => CadMath.RoundMm(axis + half),
            ImpostEdgeAlignment.Right => CadMath.RoundMm(axis - half),
            ImpostEdgeAlignment.Bottom => CadMath.RoundMm(axis + half),
            ImpostEdgeAlignment.Top => CadMath.RoundMm(axis - half),
            _ => axis
        };
    }

    /// <summary>
    /// Подрезает внешний контур переходного профиля по внутренним граням крайних импостов.
    /// Нащельник остаётся разделителем: адаптер заканчивается у его грани, не перекрывая полосу.
    /// </summary>
    public static void ApplyExtremeImpostJunctionToTransitionBandOuter(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        ref double outerLeft,
        ref double outerBottom,
        ref double outerRight,
        ref double outerTop)
    {
        if (!leaf.IsLeaf || frameRoot is null)
        {
            return;
        }

        var cellAxes = ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness);
        foreach (var slot in leaf.ExtremeImposts)
        {
            var junction = ResolveExtremeImpostInnerFaceWorld(cellAxes, slot);
            switch (slot.EdgeAlignment)
            {
                case ImpostEdgeAlignment.Left:
                    outerLeft = Math.Max(outerLeft, junction);
                    break;
                case ImpostEdgeAlignment.Right:
                    outerRight = Math.Min(outerRight, junction);
                    break;
                case ImpostEdgeAlignment.Bottom:
                    outerBottom = Math.Max(outerBottom, junction);
                    break;
                case ImpostEdgeAlignment.Top:
                    outerTop = Math.Min(outerTop, junction);
                    break;
            }
        }
    }

    /// <summary>
    /// Подрезает внутренний контур переходного профиля до внутренних граней крайних импостов.
    /// Учитывает пролёт каждого импоста, чтобы горизонтальные ригели стыковались с вертикальными.
    /// </summary>
    public static void ApplyExtremeImpostJunctionToTransitionBand(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        double bandLeft,
        double bandBottom,
        double bandRight,
        double bandTop,
        ref double innerLeft,
        ref double innerRight,
        ref double innerBottom,
        ref double innerTop)
    {
        if (!leaf.IsLeaf || frameRoot is null)
        {
            return;
        }

        var cellAxes = ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness);
        foreach (var slot in leaf.ExtremeImposts)
        {
            var junction = ResolveExtremeImpostInnerFaceWorld(cellAxes, slot);
            ExtremeImpostGeometry.ResolveSpan(
                leaf,
                frameRoot,
                frameThickness,
                cellAxes,
                slot.EdgeAlignment,
                slot.ThicknessMm,
                out var spanStart,
                out var spanEnd,
                slot);

            if (slot.Orientation == SplitOrientation.Vertical)
            {
                if (slot.EdgeAlignment == ImpostEdgeAlignment.Left
                    && ProfileSpanIntersectsVerticalBand(spanStart, spanEnd, bandBottom, bandTop))
                {
                    innerLeft = Math.Max(innerLeft, junction);
                }
                else if (slot.EdgeAlignment == ImpostEdgeAlignment.Right
                         && ProfileSpanIntersectsVerticalBand(spanStart, spanEnd, bandBottom, bandTop))
                {
                    innerRight = Math.Min(innerRight, junction);
                }

                continue;
            }

            if (slot.EdgeAlignment == ImpostEdgeAlignment.Bottom
                && ProfileSpanIntersectsHorizontalBand(spanStart, spanEnd, bandLeft, bandRight))
            {
                innerBottom = Math.Max(innerBottom, junction);
            }
            else if (slot.EdgeAlignment == ImpostEdgeAlignment.Top
                     && ProfileSpanIntersectsHorizontalBand(spanStart, spanEnd, bandLeft, bandRight))
            {
                innerTop = Math.Min(innerTop, junction);
            }
        }
    }

    private static bool ProfileSpanIntersectsVerticalBand(
        double spanStart,
        double spanEnd,
        double bandBottom,
        double bandTop) =>
        spanEnd > bandBottom + CadMath.MmPrecision && spanStart < bandTop - CadMath.MmPrecision;

    private static bool ProfileSpanIntersectsHorizontalBand(
        double spanStart,
        double spanEnd,
        double bandLeft,
        double bandRight) =>
        spanEnd > bandLeft + CadMath.MmPrecision && spanStart < bandRight - CadMath.MmPrecision;

    /// <summary>
    /// Вычисляет внутренние углы переходного профиля с учётом нащельника и сквозных осей сетки.
    /// </summary>
    public static void ResolveTransitionBandInnerCorners(
        FrameNode leaf,
        FrameNode? frameRoot,
        double frameThickness,
        double outerLeft,
        double outerBottom,
        double outerRight,
        double outerTop,
        double halfThickness,
        out double innerLeftBottom,
        out double innerRightBottom,
        out double innerLeftTop,
        out double innerRightTop,
        out double innerBottomLeft,
        out double innerTopLeft,
        out double innerBottomRight,
        out double innerTopRight)
    {
        innerLeftBottom = outerLeft + halfThickness;
        innerRightBottom = outerRight - halfThickness;
        innerLeftTop = outerLeft + halfThickness;
        innerRightTop = outerRight - halfThickness;
        innerBottomLeft = outerBottom + halfThickness;
        innerTopLeft = outerTop - halfThickness;
        innerBottomRight = outerBottom + halfThickness;
        innerTopRight = outerTop - halfThickness;

        if (frameRoot is null)
        {
            return;
        }

        var probeY = outerBottom + halfThickness;
        var probeX = outerLeft + halfThickness;
        var gridAtBottom = ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness);
        innerLeftBottom = Math.Min(innerLeftBottom, gridAtBottom.Left + halfThickness);
        innerRightBottom = Math.Max(innerRightBottom, gridAtBottom.Right - halfThickness);
        innerBottomLeft = Math.Min(innerBottomLeft, gridAtBottom.Bottom + halfThickness);

        probeY = outerTop - halfThickness;
        var gridAtTop = ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness);
        innerLeftTop = Math.Min(innerLeftTop, gridAtTop.Left + halfThickness);
        innerRightTop = Math.Max(innerRightTop, gridAtTop.Right - halfThickness);
        innerTopLeft = Math.Max(innerTopLeft, gridAtTop.Top - halfThickness);

        probeX = outerRight - halfThickness;
        var gridAtRight = ResolveLeafCellAxisBounds(leaf, frameRoot, frameThickness);
        innerBottomRight = Math.Min(innerBottomRight, gridAtRight.Bottom + halfThickness);
        innerTopRight = Math.Max(innerTopRight, gridAtRight.Top - halfThickness);

        ApplyExtremeImpostJunctionToTransitionBand(
            leaf, frameRoot, frameThickness,
            outerLeft, outerBottom, outerRight, outerBottom + CadMath.MmPrecision,
            ref innerLeftBottom, ref innerRightBottom, ref innerBottomLeft, ref innerTopLeft);
        ApplyExtremeImpostJunctionToTransitionBand(
            leaf, frameRoot, frameThickness,
            outerLeft, outerTop - CadMath.MmPrecision, outerRight, outerTop,
            ref innerLeftTop, ref innerRightTop, ref innerBottomLeft, ref innerTopLeft);
        ApplyExtremeImpostJunctionToTransitionBand(
            leaf, frameRoot, frameThickness,
            outerLeft, outerBottom, outerLeft + CadMath.MmPrecision, outerTop,
            ref innerLeftBottom, ref innerRightBottom, ref innerBottomLeft, ref innerTopLeft);
        ApplyExtremeImpostJunctionToTransitionBand(
            leaf, frameRoot, frameThickness,
            outerRight - CadMath.MmPrecision, outerBottom, outerRight, outerTop,
            ref innerLeftBottom, ref innerRightBottom, ref innerBottomRight, ref innerTopRight);

        var minInnerLeft = outerLeft + halfThickness;
        var minInnerBottom = outerBottom + halfThickness;
        var maxInnerRight = outerRight - halfThickness;
        var maxInnerTop = outerTop - halfThickness;

        innerLeftBottom = Math.Max(innerLeftBottom, minInnerLeft);
        innerLeftTop = Math.Max(innerLeftTop, minInnerLeft);
        innerRightBottom = Math.Min(innerRightBottom, maxInnerRight);
        innerRightTop = Math.Min(innerRightTop, maxInnerRight);
        innerBottomLeft = Math.Max(innerBottomLeft, minInnerBottom);
        innerBottomRight = Math.Max(innerBottomRight, minInnerBottom);
        innerTopLeft = Math.Min(innerTopLeft, maxInnerTop);
        innerTopRight = Math.Min(innerTopRight, maxInnerTop);
    }
    public static InnerOpeningBounds ResolveNodeInnerOpeningBounds(
        FrameNode node,
        FrameNode? frameRoot,
        double frameThickness,
        bool includeExtremeImposts = true)
    {
        var inner = new InnerOpeningBounds
        {
            Left = node.X,
            Bottom = node.Y,
            Right = node.X + node.Width,
            Top = node.Y + node.Height
        };

        ApplyImpostAxisEdges(node, ref inner);

        if (node.IsLeaf && includeExtremeImposts)
        {
            ExtremeImpostGeometry.ApplyExtremeImpostInsets(node, ref inner);
        }

        if (frameRoot is not null)
        {
            PinFrameInnerEdges(node, frameRoot, frameThickness, ref inner);
        }

        return RoundBounds(inner);
    }

    /// <summary>
    /// Вертикальный пролёт импоста по границам ячейки: низ — внутренняя полка рамы,
    /// верх — нижняя грань пересекающих горизонтальных импостов (динамически, без кэша).
    /// </summary>
    public static (double Bottom, double Top) ResolveVerticalImpostStructuralSpan(
        FrameNode splitNode,
        FrameNode? frameRoot,
        double frameThickness)
    {
        var bottom = splitNode.Y;
        var top = splitNode.Y + splitNode.Height;

        if (frameRoot is not null)
        {
            PinVerticalImpostSpanToFrame(splitNode, frameRoot, frameThickness, ref bottom, ref top);
        }

        ClipVerticalImpostSpanAtHorizontalAncestors(splitNode, ref bottom, ref top);

        return (
            CadMath.RoundMm(bottom),
            CadMath.RoundMm(Math.Max(bottom, top)));
    }

    /// <summary>
    /// Обрезка вертикального пролёта только по пересечениям с горизонтальными импостами-предками.
    /// Для круглых коробок: границы рамы задаёт дуга, здесь — только стыковка креста.
    /// </summary>
    public static (double Bottom, double Top) ClipVerticalImpostSpanAtHorizontalCrossings(
        FrameNode splitNode,
        double bottom,
        double top)
    {
        ClipVerticalImpostSpanAtHorizontalAncestors(splitNode, ref bottom, ref top);
        return (
            CadMath.RoundMm(bottom),
            CadMath.RoundMm(Math.Max(bottom, top)));
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

        var inner = ResolveLeafInnerOpeningBounds(editor, leaf);
        inner = UpgradeInnerTopForArcGlazing(editor, inner);
        if (inner.Width <= CadMath.Epsilon || inner.Height <= CadMath.Epsilon)
        {
            return false;
        }

        var gap = ResolveGlazingTechnologyGap(HasSashInLeaf(editor, leaf));
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

        if (editor.FrameRoot.Shape == ConstructionShape.Triangle &&
            TryBuildTriangularFixedFrameGlass(inner, gap, out glass))
        {
            return true;
        }

        return TryBuildRectangularFixedFrameGlass(inner, gap, x0, x1, y0, out glass);
    }

    private static bool TryBuildTriangularFixedFrameGlass(
        in InnerOpeningBounds inner,
        double gap,
        out GlassOpeningBounds glass)
    {
        glass = default;
        if (!inner.PinsFrameLeft || !inner.PinsFrameRight || !inner.PinsFrameBottom || !inner.PinsFrameTop)
        {
            return false;
        }

        var outer = inner.IsTriangular
            ? (inner.TriangleBl, inner.TriangleBr, inner.TriangleTop)
            : (new CadPoint(inner.Left, inner.Bottom), new CadPoint(inner.Right, inner.Bottom), new CadPoint((inner.Left + inner.Right) / 2.0, inner.Top));
        var insetTri = FrameTriangleGeometry.InsetTriangle(outer, gap);
        if (insetTri.Top.Y - insetTri.Bl.Y <= gap)
        {
            return false;
        }

        glass = new GlassOpeningBounds
        {
            Left = insetTri.Bl.X,
            Bottom = insetTri.Bl.Y,
            Right = insetTri.Br.X,
            Top = insetTri.Top.Y,
            IsTriangular = true,
            BottomLeft = insetTri.Bl,
            BottomRight = insetTri.Br,
            TopApex = insetTri.Top
        };
        return glass.Width > CadMath.Epsilon && glass.Height > CadMath.Epsilon;
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
        var inner = ResolveLeafInnerOpeningBounds(editor, leaf);
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
        if (editor.FrameRoot is null
            || sash.Kind is not (GeometryElementKind.Sash or GeometryElementKind.SlidingSash)
            || !leaf.IsTerminalLeaf)
        {
            return false;
        }

        GetSashOverlapBounds(sash, out var sLeft, out var sBottom, out var sRight, out var sTop);
        var iLeft = Math.Max(leaf.X, sLeft);
        var iRight = Math.Min(leaf.X + leaf.Width, sRight);
        var iBottom = Math.Max(leaf.Y, sBottom);
        var iTop = Math.Min(leaf.Y + leaf.Height, sTop);
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
        if (editor.FrameRoot is null
            || sash.Kind is not (GeometryElementKind.Sash or GeometryElementKind.SlidingSash))
        {
            return [];
        }

        if (ResolveSashHostLeaf(editor, sash) is { IsTerminalLeaf: true } boundLeaf)
        {
            return [boundLeaf];
        }

        var matches = new List<FrameNode>();
        foreach (var leaf in EnumerateLeaves(editor.FrameRoot))
        {
            if (!leaf.IsTerminalLeaf)
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
        if (editor.FrameRoot is null
            || sash.Kind is not (GeometryElementKind.Sash or GeometryElementKind.SlidingSash))
        {
            return null;
        }

        if (ResolveSashHostLeaf(editor, sash) is { IsTerminalLeaf: true } boundLeaf)
        {
            return boundLeaf;
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

        return bestLeaf;
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

        if (glazing.ParentBeadId is Guid beadId)
        {
            return Services.GlazingBead.GlassBuilder.TryResolveGlassBoundsForBeadGlazing(
                editor,
                glazing,
                elements,
                out glass);
        }

        if (glazing.HostSashId is Guid sashId)
        {
            var sash = elements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
            if (sash is not null && TryResolveGlassForSash(editor, sash, out glass, out _, out _))
            {
                return true;
            }
        }

        if (editor.FrameRoot is not null)
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

    /// <summary>
    /// Внутренние углы профиля створки (стыки 45°) — координаты для линий открывания.
    /// Совпадает с внутренней гранью, которую рисует <see cref="FrameVolumeRenderer"/>.
    /// </summary>
    public static InnerOpeningBounds ResolveSashProfileInnerFaceBounds(
        CadEditorViewModel editor,
        in InnerOpeningBounds placementInner)
    {
        var outer = ExpandInnerBoundsToOuter(placementInner, editor);
        if (outer.Width <= CadMath.Epsilon || outer.Height <= CadMath.Epsilon)
        {
            return placementInner;
        }

        var half = FrameProfileGeometry.ResolveInnerInset(
            outer.Width,
            outer.Height,
            SashDrawProfileThicknessMm);
        if (half <= CadMath.Epsilon || outer.Width <= half * 2 || outer.Height <= half * 2)
        {
            return placementInner;
        }

        return RoundBounds(new InnerOpeningBounds
        {
            Left = outer.Left + half,
            Bottom = outer.Bottom + half,
            Right = outer.Right - half,
            Top = outer.Top - half,
            PinsFrameLeft = placementInner.PinsFrameLeft,
            PinsFrameRight = placementInner.PinsFrameRight,
            PinsFrameBottom = placementInner.PinsFrameBottom,
            PinsFrameTop = placementInner.PinsFrameTop,
            IsTriangular = placementInner.IsTriangular,
            TriangleBl = placementInner.TriangleBl,
            TriangleBr = placementInner.TriangleBr,
            TriangleTop = placementInner.TriangleTop
        });
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
        if (frameRoot.Shape == ConstructionShape.Triangle)
        {
            PinTriangleFrameInnerEdges(leaf, frameRoot, frameThickness, ref inner);
            return;
        }

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

    private static void PinTriangleFrameInnerEdges(
        FrameNode leaf,
        FrameNode frameRoot,
        double frameThickness,
        ref InnerOpeningBounds inner)
    {
        var inset = FrameProfileGeometry.ResolveInnerInset(
            frameRoot.Width,
            frameRoot.Height,
            frameThickness);
        var outer = FrameTriangleGeometry.BuildOuterTriangle(
            frameRoot.X,
            frameRoot.Y,
            frameRoot.Width,
            frameRoot.Height);
        var innerTri = FrameTriangleGeometry.InsetTriangle(outer, inset);

        var frameLeft = frameRoot.X;
        var frameBottom = frameRoot.Y;
        var frameRight = frameRoot.X + frameRoot.Width;
        var frameTop = frameRoot.Y + frameRoot.Height;

        if (OccupiesFrameLeftEdge(leaf, frameLeft, innerTri.Bl.X))
        {
            inner.Left = innerTri.Bl.X;
            inner.PinsFrameLeft = true;
        }

        if (OccupiesFrameRightEdge(leaf, frameRight, innerTri.Br.X))
        {
            inner.Right = innerTri.Br.X;
            inner.PinsFrameRight = true;
        }

        if (OccupiesFrameBottomEdge(leaf, frameBottom, innerTri.Bl.Y))
        {
            inner.Bottom = innerTri.Bl.Y;
            inner.PinsFrameBottom = true;
        }

        if (OccupiesFrameTopEdge(leaf, frameTop, innerTri.Top.Y))
        {
            inner.Top = innerTri.Top.Y;
            inner.PinsFrameTop = true;
            inner.IsTriangular = true;
            inner.TriangleBl = innerTri.Bl;
            inner.TriangleBr = innerTri.Br;
            inner.TriangleTop = innerTri.Top;
        }
    }

    private static void ApplyTriangularBoundsAtPoint(FrameNode frameRoot, double worldX, ref InnerOpeningBounds inner)
    {
        if (frameRoot.Shape != ConstructionShape.Triangle || !inner.IsTriangular)
        {
            return;
        }

        inner.Top = CadMath.RoundMm(inner.ResolveTopAtX(worldX));
    }

    public static bool TryResolveTriangleInnerTopForSpan(
        FrameNode frameRoot,
        double frameThickness,
        double spanLeft,
        double spanRight,
        out double topY)
    {
        topY = 0;
        if (frameRoot.Shape != ConstructionShape.Triangle)
        {
            return false;
        }

        var innerTri = FrameTriangleGeometry.BuildInnerTriangle(frameRoot, frameThickness);
        var leftTop = FrameTriangleGeometry.InterpolateTopAtX(innerTri.Bl, innerTri.Br, innerTri.Top, spanLeft);
        var rightTop = FrameTriangleGeometry.InterpolateTopAtX(innerTri.Bl, innerTri.Br, innerTri.Top, spanRight);
        topY = CadMath.RoundMm(Math.Min(leftTop, rightTop));
        return true;
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

    private static void PinFictitiousGridAxisEdges(
        FrameNode leaf,
        FrameNode frameRoot,
        ref InnerOpeningBounds inner)
    {
        var frameLeft = frameRoot.X;
        var frameBottom = frameRoot.Y;
        var frameRight = frameRoot.X + frameRoot.Width;
        var frameTop = frameRoot.Y + frameRoot.Height;

        if (OccupiesFrameLeftEdge(leaf, frameLeft, frameLeft))
        {
            inner.Left = frameLeft;
            inner.PinsFrameLeft = true;
        }

        if (OccupiesFrameRightEdge(leaf, frameRight, frameRight))
        {
            inner.Right = frameRight;
            inner.PinsFrameRight = true;
        }

        if (OccupiesFrameBottomEdge(leaf, frameBottom, frameBottom))
        {
            inner.Bottom = frameBottom;
            inner.PinsFrameBottom = true;
        }

        if (OccupiesFrameTopEdge(leaf, frameTop, frameTop))
        {
            inner.Top = frameTop;
            inner.PinsFrameTop = true;
        }
    }

    private static void ApplyCellGridAxisEdges(FrameNode leaf, ref InnerOpeningBounds inner)
    {
        for (var node = leaf; node.Parent is not null; node = node.Parent)
        {
            var parent = node.Parent;
            if (parent.IsUniformSplit)
            {
                ApplyUniformCellGridAxisEdges(parent, leaf, ref inner);
                continue;
            }

            if (parent.IsLeaf || parent.ChildA is null || parent.ChildB is null)
            {
                continue;
            }

            if (parent.SplitOrientation == SplitOrientation.Vertical)
            {
                var centerX = ResolveVerticalImpostCenterX(parent);
                if (IsInSubtree(leaf, parent.ChildA))
                {
                    inner.Right = CadMath.RoundMm(centerX);
                    inner.PinsFrameRight = false;
                }
                else if (IsInSubtree(leaf, parent.ChildB))
                {
                    inner.Left = CadMath.RoundMm(centerX);
                    inner.PinsFrameLeft = false;
                }
            }
            else if (parent.SplitOrientation == SplitOrientation.Horizontal)
            {
                var centerY = ResolveHorizontalImpostCenterY(parent);
                if (IsInSubtree(leaf, parent.ChildA))
                {
                    inner.Top = CadMath.RoundMm(centerY);
                    inner.PinsFrameTop = false;
                }
                else if (IsInSubtree(leaf, parent.ChildB))
                {
                    inner.Bottom = CadMath.RoundMm(centerY);
                    inner.PinsFrameBottom = false;
                }
            }
        }
    }

    private static void ApplyUniformCellGridAxisEdges(
        FrameNode parent,
        FrameNode leaf,
        ref InnerOpeningBounds inner)
    {
        var cells = parent.UniformCells;

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
                    inner.Left = CadMath.RoundMm(leftAxis);
                    inner.PinsFrameLeft = false;
                }

                if (i < cells.Count - 1
                    && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var rightAxis))
                {
                    inner.Right = CadMath.RoundMm(rightAxis);
                    inner.PinsFrameRight = false;
                }
            }
            else if (parent.SplitOrientation == SplitOrientation.Horizontal)
            {
                if (i > 0
                    && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i - 1, out var bottomAxis))
                {
                    inner.Bottom = CadMath.RoundMm(bottomAxis);
                    inner.PinsFrameBottom = false;
                }

                if (i < cells.Count - 1
                    && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var topAxis))
                {
                    inner.Top = CadMath.RoundMm(topAxis);
                    inner.PinsFrameTop = false;
                }
            }

            return;
        }
    }

    /// <summary>
    /// Продлевает оси импостов через всё координатное пространство рамы.
    /// Ячейка под курсором ограничивается пересечениями сквозных осей.
    /// </summary>
    private static void ApplyCrossCuttingGridAxes(
        FrameNode leaf,
        FrameNode frameRoot,
        double worldX,
        double worldY,
        ref InnerOpeningBounds inner)
    {
        foreach (var impost in EnumerateGridImpostNodes(frameRoot))
        {
            if (impost.IsUniformSplit)
            {
                if (!IsInSubtree(leaf, impost))
                {
                    continue;
                }

                ApplyUniformCrossCuttingGridAxes(impost, leaf, worldX, worldY, ref inner);
                continue;
            }

            if (impost.ChildA is null || impost.ChildB is null)
            {
                continue;
            }

            // Сквозные оси: физический сплит в соседней колонке/ряду всё равно делит
            // координатное пространство текущей ячейки (виртуальная граница).
            // Фильтр — пересечение оси с габаритом листа, а не принадлежность поддереву.

            if (impost.SplitOrientation == SplitOrientation.Horizontal)
            {
                var axisY = ResolveHorizontalImpostCenterY(impost);
                if (!AxisCrossesVerticalSpan(axisY, leaf.Y, leaf.Y + leaf.Height))
                {
                    continue;
                }

                if (worldY >= axisY)
                {
                    inner.Bottom = CadMath.RoundMm(Math.Max(inner.Bottom, axisY));
                }
                else
                {
                    inner.Top = CadMath.RoundMm(Math.Min(inner.Top, axisY));
                }
            }
            else if (impost.SplitOrientation == SplitOrientation.Vertical)
            {
                var axisX = ResolveVerticalImpostCenterX(impost);
                if (!AxisCrossesVerticalSpan(axisX, leaf.X, leaf.X + leaf.Width))
                {
                    continue;
                }

                if (worldX >= axisX)
                {
                    inner.Left = CadMath.RoundMm(Math.Max(inner.Left, axisX));
                }
                else
                {
                    inner.Right = CadMath.RoundMm(Math.Min(inner.Right, axisX));
                }
            }
        }
    }

    private static void ApplyUniformCrossCuttingGridAxes(
        FrameNode uniformParent,
        FrameNode leaf,
        double worldX,
        double worldY,
        ref InnerOpeningBounds inner)
    {
        var gapCount = uniformParent.UniformCells.Count - 1;
        if (gapCount < 1)
        {
            return;
        }

        if (uniformParent.SplitOrientation == SplitOrientation.Horizontal)
        {
            for (var i = 0; i < gapCount; i++)
            {
                if (!uniformParent.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var axisY))
                {
                    continue;
                }

                if (!AxisCrossesVerticalSpan(axisY, leaf.Y, leaf.Y + leaf.Height))
                {
                    continue;
                }

                if (worldY >= axisY)
                {
                    inner.Bottom = CadMath.RoundMm(Math.Max(inner.Bottom, axisY));
                }
                else
                {
                    inner.Top = CadMath.RoundMm(Math.Min(inner.Top, axisY));
                }
            }

            return;
        }

        if (uniformParent.SplitOrientation == SplitOrientation.Vertical)
        {
            for (var i = 0; i < gapCount; i++)
            {
                if (!uniformParent.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var axisX))
                {
                    continue;
                }

                if (!AxisCrossesVerticalSpan(axisX, leaf.X, leaf.X + leaf.Width))
                {
                    continue;
                }

                if (worldX >= axisX)
                {
                    inner.Left = CadMath.RoundMm(Math.Max(inner.Left, axisX));
                }
                else
                {
                    inner.Right = CadMath.RoundMm(Math.Min(inner.Right, axisX));
                }
            }
        }
    }

    private static bool AxisCrossesVerticalSpan(double axis, double spanStart, double spanEnd) =>
        axis > spanStart + CadMath.MmPrecision && axis < spanEnd - CadMath.MmPrecision;

    private static IEnumerable<FrameNode> EnumerateGridImpostNodes(FrameNode node)
    {
        if (node.IsUniformSplit)
        {
            if (node.UniformCells.Count >= 2)
            {
                yield return node;
            }

            foreach (var cell in node.UniformCells)
            {
                foreach (var child in EnumerateGridImpostNodes(cell))
                {
                    yield return child;
                }
            }

            yield break;
        }

        if (!node.IsLeaf && node.ChildA is not null && node.ChildB is not null)
        {
            yield return node;
            foreach (var child in EnumerateGridImpostNodes(node.ChildA))
            {
                yield return child;
            }

            foreach (var child in EnumerateGridImpostNodes(node.ChildB))
            {
                yield return child;
            }
        }
    }

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

    private static void PinVerticalImpostSpanToFrame(
        FrameNode splitNode,
        FrameNode frameRoot,
        double frameThickness,
        ref double bottom,
        ref double top)
    {
        var inset = FrameProfileGeometry.ResolveInnerInset(
            frameRoot.Width,
            frameRoot.Height,
            frameThickness);

        var frameBottom = frameRoot.Y;
        var frameTop = frameRoot.Y + frameRoot.Height;
        var frameInnerBottom = CadMath.RoundMm(frameBottom + inset);
        var frameInnerTop = CadMath.RoundMm(frameTop - inset);

        if (CellTouchesFrameBottom(splitNode, frameBottom, frameInnerBottom))
        {
            bottom = frameInnerBottom;
        }

        if (CellTouchesFrameTop(splitNode, frameTop, frameInnerTop))
        {
            if (frameRoot.Shape == ConstructionShape.Triangle)
            {
                var innerTri = FrameTriangleGeometry.BuildInnerTriangle(frameRoot, frameThickness);
                var axisX = splitNode.X + splitNode.Width / 2.0;
                top = CadMath.RoundMm(
                    FrameTriangleGeometry.InterpolateTopAtX(innerTri.Bl, innerTri.Br, innerTri.Top, axisX));
            }
            else if (TryResolveArcAwareFrameInnerTop(
                    frameRoot,
                    frameThickness,
                    splitNode.X,
                    splitNode.X + splitNode.Width,
                    frameInnerTop,
                    out var arcAwareTop))
            {
                top = arcAwareTop;
            }
            else
            {
                top = frameInnerTop;
            }
        }
    }

    private static bool CellTouchesFrameBottom(FrameNode splitNode, double frameBottom, double frameInnerBottom)
    {
        for (var node = splitNode; ; node = node.Parent!)
        {
            if (OccupiesFrameBottomEdge(node, frameBottom, frameInnerBottom))
            {
                return true;
            }

            if (node.Parent is null)
            {
                break;
            }
        }

        return false;
    }

    internal static bool CellTouchesFrameTop(FrameNode splitNode, double frameTop, double frameInnerTop)
    {
        for (var node = splitNode; ; node = node.Parent!)
        {
            if (OccupiesFrameTopEdge(node, frameTop, frameInnerTop))
            {
                return true;
            }

            if (node.Parent is null)
            {
                break;
            }
        }

        return false;
    }

    /// <summary>
    /// Колонка/ячейка упирается в правую грань рамы (внешнюю или внутреннюю полку).
    /// </summary>
    internal static bool CellTouchesFrameRight(FrameNode splitNode, double frameRight, double frameInnerRight)
    {
        for (var node = splitNode; ; node = node.Parent!)
        {
            if (OccupiesFrameRightEdge(node, frameRight, frameInnerRight))
            {
                return true;
            }

            if (node.Parent is null)
            {
                break;
            }
        }

        return false;
    }

    /// <summary>
    /// Допустимый остаточный зазор (мм) между top и внутренней полкой рамы,
    /// вызванный округлением сетки — при таком зазоре импост дотягивается до полки.
    /// </summary>
    private const double VerticalImpostTopSnapToleranceMm = 4.0;

    /// <summary>
    /// Колонка вертикального импоста упирается в верхнюю раму: impost-узел, смежная ячейка
    /// или микрозазор от промежуточных расчётов сетки в пределах допуска.
    /// </summary>
    internal static bool VerticalImpostColumnTouchesFrameTop(
        FrameNode splitNode,
        double impostAxisX,
        double frameTop,
        double frameInnerTop,
        double top)
    {
        if (CellTouchesFrameTop(splitNode, frameTop, frameInnerTop))
        {
            return true;
        }

        if (TryGetVerticalImpostColumnCell(splitNode, impostAxisX, out var columnCell)
            && CellTouchesFrameTop(columnCell, frameTop, frameInnerTop))
        {
            return true;
        }

        var residualGap = frameInnerTop - top;
        return residualGap > CadMath.MmPrecision
            && residualGap <= VerticalImpostTopSnapToleranceMm;
    }

    private const double HorizontalImpostRightSnapToleranceMm = 4.0;

    /// <summary>
    /// Ряд горизонтального импоста упирается в правую раму: impost-узел, смежная ячейка
    /// или микрозазор от промежуточных расчётов сетки в пределах допуска.
    /// </summary>
    internal static bool HorizontalImpostRowTouchesFrameRight(
        FrameNode splitNode,
        double frameRight,
        double frameInnerRight,
        double right,
        double? impostAxisY = null)
    {
        if (CellTouchesFrameRight(splitNode, frameRight, frameInnerRight))
        {
            return true;
        }

        if (impostAxisY is double axisY
            && TryGetHorizontalImpostRowCell(splitNode, axisY, out var rowCell)
            && CellTouchesFrameRight(rowCell, frameRight, frameInnerRight))
        {
            return true;
        }

        for (var node = splitNode.Parent; node is not null; node = node.Parent)
        {
            if (CellTouchesFrameRight(node, frameRight, frameInnerRight))
            {
                return true;
            }
        }

        var residualGap = frameInnerRight - right;
        return residualGap > CadMath.MmPrecision
            && residualGap <= HorizontalImpostRightSnapToleranceMm;
    }

    internal static bool TryGetVerticalImpostColumnCell(
        FrameNode impostNode,
        double impostAxisX,
        out FrameNode columnCell)
    {
        columnCell = impostNode;
        if (impostNode.ChildA is null || impostNode.ChildB is null)
        {
            return false;
        }

        if (impostAxisX <= impostNode.ChildA.X + impostNode.ChildA.Width + CadMath.MmPrecision)
        {
            columnCell = impostNode.ChildA;
            return true;
        }

        columnCell = impostNode.ChildB;
        return true;
    }

    /// <summary>
    /// Ячейка ряда горизонтального импоста (ChildA — низ, ChildB — верх).
    /// Используется для локализации пролёта ригеля в пределах колонки/секции.
    /// </summary>
    internal static bool TryGetHorizontalImpostRowCell(
        FrameNode impostNode,
        double impostAxisY,
        out FrameNode rowCell)
    {
        rowCell = impostNode;
        if (impostNode.ChildA is null || impostNode.ChildB is null)
        {
            return false;
        }

        if (impostAxisY <= impostNode.ChildA.Y + impostNode.ChildA.Height + CadMath.MmPrecision)
        {
            rowCell = impostNode.ChildA;
            return true;
        }

        rowCell = impostNode.ChildB;
        return true;
    }

    /// <summary>
    /// Обрезка горизонтального пролёта по вертикальным импостам-предкам (изоляция колонок).
    /// </summary>
    public static void ClipHorizontalImpostSpanAtVerticalAncestors(
        FrameNode splitNode,
        ref double left,
        ref double right)
    {
        for (var current = splitNode.Parent; current is not null; current = current.Parent)
        {
            if (current.IsUniformSplit)
            {
                ClipHorizontalImpostSpanAtUniformVerticalParent(current, splitNode, ref left, ref right);
                continue;
            }

            if (current.IsLeaf || current.ChildA is null || current.ChildB is null
                || current.SplitOrientation != SplitOrientation.Vertical)
            {
                continue;
            }

            var half = current.GetResolvedImpostThicknessMm() / 2.0;
            var centerX = ResolveVerticalImpostCenterX(current);

            if (IsInSubtree(splitNode, current.ChildA))
            {
                right = Math.Min(right, centerX - half);
            }
            else if (IsInSubtree(splitNode, current.ChildB))
            {
                left = Math.Max(left, centerX + half);
            }
        }
    }

    private static void ClipHorizontalImpostSpanAtUniformVerticalParent(
        FrameNode parent,
        FrameNode splitNode,
        ref double left,
        ref double right)
    {
        if (parent.SplitOrientation != SplitOrientation.Vertical)
        {
            return;
        }

        var cells = parent.UniformCells;
        for (var i = 0; i < cells.Count - 1; i++)
        {
            if (!parent.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var axisX))
            {
                axisX = cells[i].X + cells[i].Width;
            }

            var half = parent.GetResolvedImpostThicknessMm() / 2.0;
            if (IsInSubtree(splitNode, cells[i]))
            {
                right = Math.Min(right, axisX - half);
            }
            else if (cells[i + 1] is not null && IsInSubtree(splitNode, cells[i + 1]))
            {
                left = Math.Max(left, axisX + half);
            }
        }
    }

    private static void ClipVerticalImpostSpanAtHorizontalAncestors(
        FrameNode splitNode,
        ref double bottom,
        ref double top)
    {
        for (var current = splitNode.Parent; current is not null; current = current.Parent)
        {
            if (current.IsUniformSplit)
            {
                ClipVerticalImpostSpanAtUniformParent(current, splitNode, ref bottom, ref top);
                continue;
            }

            if (current.IsLeaf || current.ChildA is null || current.ChildB is null
                || current.SplitOrientation != SplitOrientation.Horizontal)
            {
                continue;
            }

            var half = current.GetResolvedImpostThicknessMm() / 2.0;
            var centerY = ResolveHorizontalImpostCenterY(current);

            if (IsInSubtree(splitNode, current.ChildA))
            {
                top = Math.Min(top, centerY - half);
            }
            else if (IsInSubtree(splitNode, current.ChildB))
            {
                bottom = Math.Max(bottom, centerY + half);
            }
        }
    }

    private static void ClipVerticalImpostSpanAtUniformParent(
        FrameNode parent,
        FrameNode splitNode,
        ref double bottom,
        ref double top)
    {
        if (parent.SplitOrientation != SplitOrientation.Horizontal)
        {
            return;
        }

        var cells = parent.UniformCells;
        var half = parent.GetResolvedImpostThicknessMm() / 2.0;

        for (var i = 0; i < cells.Count; i++)
        {
            if (!IsInSubtree(splitNode, cells[i]))
            {
                continue;
            }

            if (i > 0
                && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i - 1, out var axisBelow))
            {
                bottom = Math.Max(bottom, axisBelow + half);
            }

            if (i < cells.Count - 1
                && parent.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var axisAbove))
            {
                top = Math.Min(top, axisAbove - half);
            }

            return;
        }
    }

    internal static bool IsInSubtree(FrameNode leaf, FrameNode? ancestor)
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