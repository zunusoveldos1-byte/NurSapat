using System.Linq;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Геометрия переходного профиля как полноценного Host Frame:
/// внутренние/наружные грани, полки примыкания и монтажные плоскости.
/// Все расчёты импостов/створок/фурнитуры опираются на эти поверхности.
/// </summary>
internal static class TransitionAdjacentFrameGeometry
{
    /// <summary>
    /// Полное описание переходного профиля как несущей рамы.
    /// </summary>
    public readonly record struct TransitionFrameHostGeometry(
        double OuterLeft,
        double OuterBottom,
        double OuterRight,
        double OuterTop,
        double InnerLeftBottom,
        double InnerRightBottom,
        double InnerLeftTop,
        double InnerRightTop,
        double InnerBottomLeft,
        double InnerBottomRight,
        double InnerTopLeft,
        double InnerTopRight,
        double ProfileThickness,
        double HalfThickness)
    {
        public double OuterWidth => OuterRight - OuterLeft;
        public double OuterHeight => OuterTop - OuterBottom;

        /// <summary>Внутренняя левая грань (X) на высоте probeY.</summary>
        public double InnerLeftFace(double probeY) =>
            InterpolateVertical(InnerLeftBottom, InnerLeftTop, OuterBottom, OuterTop, probeY);

        /// <summary>Внутренняя правая грань (X) на высоте probeY.</summary>
        public double InnerRightFace(double probeY) =>
            InterpolateVertical(InnerRightBottom, InnerRightTop, OuterBottom, OuterTop, probeY);

        /// <summary>Внутренняя нижняя грань (Y) на ширине probeX.</summary>
        public double InnerBottomFace(double probeX) =>
            InterpolateHorizontal(InnerBottomLeft, InnerBottomRight, OuterLeft, OuterRight, probeX);

        /// <summary>Внутренняя верхняя грань (Y) на ширине probeX.</summary>
        public double InnerTopFace(double probeX) =>
            InterpolateHorizontal(InnerTopLeft, InnerTopRight, OuterLeft, OuterRight, probeX);

        public double MountingLeft => OuterLeft + HalfThickness;
        public double MountingRight => OuterRight - HalfThickness;
        public double MountingBottom => OuterBottom + HalfThickness;
        public double MountingTop => OuterTop - HalfThickness;
    }

    /// <summary>
    /// Единая точка входа: внутренний проём хоста с учётом примыкающих переходных профилей.
    /// Используется для импостов, створок и фурнитуры без специальных веток.
    /// </summary>
    public static SashGeometry.InnerOpeningBounds ResolveHostInnerOpeningBounds(
        CadEditorViewModel editor,
        FrameNode hostNode,
        FrameNode? frameRoot,
        double frameThickness,
        bool includeExtremeImposts = true)
    {
        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
            hostNode,
            frameRoot,
            frameThickness,
            includeExtremeImposts);

        if (frameRoot?.IsFictitiousContainer != true)
        {
            return inner;
        }

        ApplyAdjacentTransitionHostFrames(editor, hostNode, frameRoot, frameThickness, ref inner);
        return inner;
    }

    /// <summary>Алиас для обратной совместимости вызовов импостной геометрии.</summary>
    public static SashGeometry.InnerOpeningBounds ResolveImpostHostInnerOpeningBounds(
        CadEditorViewModel editor,
        FrameNode hostNode,
        FrameNode? frameRoot,
        double frameThickness) =>
        ResolveHostInnerOpeningBounds(editor, hostNode, frameRoot, frameThickness);

    /// <summary>
    /// Внутренний проём хоста с T-образной подрезкой на высоте оси горизонтального импоста.
    /// Правая грань останавливается на внутренней полке рамы до линии стыка (нащельника).
    /// </summary>
    public static SashGeometry.InnerOpeningBounds ResolveImpostHostInnerOpeningBounds(
        CadEditorViewModel editor,
        FrameNode hostNode,
        FrameNode? frameRoot,
        double frameThickness,
        double impostAxisY)
    {
        var inner = ResolveHostInnerOpeningBounds(editor, hostNode, frameRoot, frameThickness);
        if (frameRoot?.IsFictitiousContainer != true)
        {
            return inner;
        }

        var probeY = Math.Clamp(impostAxisY, hostNode.Y, hostNode.Y + hostNode.Height);
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(editor, element, frameRoot, frameThickness, out var host))
            {
                continue;
            }

            var transitionLeaf = ResolveHostLeaf(frameRoot, element, host);
            if (transitionLeaf is null)
            {
                continue;
            }

            if (IsHostLeftOfTransition(hostNode, transitionLeaf))
            {
                inner.Right = Math.Min(inner.Right, host.InnerLeftFace(probeY));
                inner.PinsFrameRight = true;
            }

            if (IsHostRightOfTransition(hostNode, transitionLeaf))
            {
                inner.Left = Math.Max(inner.Left, host.InnerRightFace(probeY));
                inner.PinsFrameLeft = true;
            }
        }

        if (inner.Right < inner.Left)
        {
            inner.Right = inner.Left;
        }

        inner.Left = CadMath.RoundMm(inner.Left);
        inner.Right = CadMath.RoundMm(inner.Right);
        return inner;
    }

    public static void ApplyAdjacentTransitionHostFrames(
        CadEditorViewModel editor,
        FrameNode hostNode,
        FrameNode frameRoot,
        double frameThickness,
        ref SashGeometry.InnerOpeningBounds inner)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(editor, element, frameRoot, frameThickness, out var host)
                || host.OuterWidth <= CadMath.Epsilon
                || host.OuterHeight <= CadMath.Epsilon)
            {
                continue;
            }

            var transitionLeaf = ResolveHostLeaf(frameRoot, element, host);
            if (transitionLeaf is null)
            {
                continue;
            }

            // Ячейка сама является Transition Host: полный контур кольца — только если
            // переходной профиль заполняет ячейку как рама; иначе это полоса внутри листа.
            if (transitionLeaf.NodeId == hostNode.NodeId
                || ReferenceEquals(transitionLeaf, hostNode))
            {
                if (IsFullLeafTransitionRing(host, hostNode))
                {
                    ApplyContainedTransitionHostOpening(host, hostNode, ref inner);
                }
                else
                {
                    ApplyEmbeddedTransitionBandConstraints(host, hostNode, ref inner);
                }

                continue;
            }

            // Переходной профиль — несущая сторона соседней ячейки.
            // Допускается зазор до толщины импоста между листьями дерева.
            if (IsHostRightOfTransition(hostNode, transitionLeaf)
                || IsHostGeometricallyRightOf(hostNode, host))
            {
                var probeY = Math.Clamp(
                    hostNode.Y + hostNode.Height / 2.0,
                    hostNode.Y,
                    hostNode.Y + hostNode.Height);
                inner.Left = Math.Max(inner.Left, host.InnerRightFace(probeY));
                inner.PinsFrameLeft = true;
            }

            if (IsHostLeftOfTransition(hostNode, transitionLeaf)
                || IsHostGeometricallyLeftOf(hostNode, host))
            {
                var probeY = Math.Clamp(
                    hostNode.Y + hostNode.Height / 2.0,
                    hostNode.Y,
                    hostNode.Y + hostNode.Height);
                inner.Right = Math.Min(inner.Right, host.InnerLeftFace(probeY));
                inner.PinsFrameRight = true;
            }

            if (IsHostAboveTransition(hostNode, transitionLeaf)
                || IsHostGeometricallyAbove(hostNode, host))
            {
                var probeX = Math.Clamp(
                    hostNode.X + hostNode.Width / 2.0,
                    hostNode.X,
                    hostNode.X + hostNode.Width);
                inner.Bottom = Math.Max(inner.Bottom, host.InnerTopFace(probeX));
                inner.PinsFrameBottom = true;
            }

            if (IsHostBelowTransition(hostNode, transitionLeaf)
                || IsHostGeometricallyBelow(hostNode, host))
            {
                var probeX = Math.Clamp(
                    hostNode.X + hostNode.Width / 2.0,
                    hostNode.X,
                    hostNode.X + hostNode.Width);
                inner.Top = Math.Min(inner.Top, host.InnerBottomFace(probeX));
                inner.PinsFrameTop = true;
            }
        }

        if (inner.Right < inner.Left)
        {
            inner.Right = inner.Left;
        }

        if (inner.Top < inner.Bottom)
        {
            inner.Top = inner.Bottom;
        }

        inner.Left = CadMath.RoundMm(inner.Left);
        inner.Bottom = CadMath.RoundMm(inner.Bottom);
        inner.Right = CadMath.RoundMm(inner.Right);
        inner.Top = CadMath.RoundMm(inner.Top);
    }

    private static void ApplyContainedTransitionHostOpening(
        in TransitionFrameHostGeometry host,
        FrameNode hostNode,
        ref SashGeometry.InnerOpeningBounds inner)
    {
        var midY = hostNode.Y + hostNode.Height / 2.0;
        var midX = hostNode.X + hostNode.Width / 2.0;

        inner.Left = Math.Max(inner.Left, host.InnerLeftFace(midY));
        inner.Right = Math.Min(inner.Right, host.InnerRightFace(midY));
        inner.Bottom = Math.Max(inner.Bottom, host.InnerBottomFace(midX));
        inner.Top = Math.Min(inner.Top, host.InnerTopFace(midX));
        inner.PinsFrameLeft = true;
        inner.PinsFrameRight = true;
        inner.PinsFrameBottom = true;
        inner.PinsFrameTop = true;
    }

    /// <summary>
    /// Переходной профиль заполняет ячейку как полноценная рама-кольцо (≥85% площади).
    /// </summary>
    private static bool IsFullLeafTransitionRing(in TransitionFrameHostGeometry host, FrameNode hostNode)
    {
        var leafArea = hostNode.Width * hostNode.Height;
        if (leafArea <= CadMath.Epsilon)
        {
            return false;
        }

        return host.OuterWidth * host.OuterHeight / leafArea >= 0.85;
    }

    /// <summary>
    /// Полоса переходного профиля внутри листа: подрезает проём по внутренней рабочей грани,
    /// не превращая весь лист во внутренность кольца.
    /// </summary>
    private static void ApplyEmbeddedTransitionBandConstraints(
        in TransitionFrameHostGeometry host,
        FrameNode hostNode,
        ref SashGeometry.InnerOpeningBounds inner)
    {
        var midX = hostNode.X + hostNode.Width / 2.0;
        var midY = hostNode.Y + hostNode.Height / 2.0;
        var leafBottom = hostNode.Y;
        var leafTop = hostNode.Y + hostNode.Height;
        var leafLeft = hostNode.X;
        var leafRight = hostNode.X + hostNode.Width;

        var touchesBottom = host.OuterBottom <= leafBottom + FrameNode.DefaultImpostProfileMm;
        var touchesTop = host.OuterTop >= leafTop - FrameNode.DefaultImpostProfileMm;
        var touchesLeft = host.OuterLeft <= leafLeft + FrameNode.DefaultImpostProfileMm;
        var touchesRight = host.OuterRight >= leafRight - FrameNode.DefaultImpostProfileMm;
        var spansWidth = host.OuterWidth >= hostNode.Width * 0.7;
        var spansHeight = host.OuterHeight >= hostNode.Height * 0.7;

        if (spansWidth && touchesBottom && !touchesTop)
        {
            inner.Bottom = Math.Max(inner.Bottom, host.InnerTopFace(midX));
            inner.PinsFrameBottom = true;
        }
        else if (spansWidth && touchesTop && !touchesBottom)
        {
            inner.Top = Math.Min(inner.Top, host.InnerBottomFace(midX));
            inner.PinsFrameTop = true;
        }
        else if (spansHeight && touchesLeft && !touchesRight)
        {
            inner.Left = Math.Max(inner.Left, host.InnerRightFace(midY));
            inner.PinsFrameLeft = true;
        }
        else if (spansHeight && touchesRight && !touchesLeft)
        {
            inner.Right = Math.Min(inner.Right, host.InnerLeftFace(midY));
            inner.PinsFrameRight = true;
        }
        else if (spansWidth)
        {
            // Горизонтальная полоса в середине листа: верхняя зона не ниже InnerTopFace.
            inner.Bottom = Math.Max(inner.Bottom, host.InnerTopFace(midX));
            inner.PinsFrameBottom = true;
        }
        else if (spansHeight)
        {
            inner.Left = Math.Max(inner.Left, host.InnerRightFace(midY));
            inner.PinsFrameLeft = true;
        }
    }

    /// <summary>
    /// Подрезка проёма размещения (створка/заполнение) по внутренним граням переходных профилей
    /// в зависимости от квадранта под курсором.
    /// </summary>
    public static void ClipPlacementOpeningAtTransitionHosts(
        CadEditorViewModel editor,
        FrameNode leaf,
        double probeX,
        double probeY,
        ref double left,
        ref double bottom,
        ref double right,
        ref double top,
        ref bool pinsLeft,
        ref bool pinsBottom,
        ref bool pinsRight,
        ref bool pinsTop)
    {
        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(editor, element, editor.FrameRoot, editor.FrameThickness, out var host)
                || host.OuterWidth <= CadMath.Epsilon
                || host.OuterHeight <= CadMath.Epsilon)
            {
                continue;
            }

            if (!RangesOverlap(left, right, host.OuterLeft, host.OuterRight)
                && !RangesOverlap(bottom, top, host.OuterBottom, host.OuterTop)
                && !IsHostGeometricallyAbove(leaf, host)
                && !IsHostGeometricallyBelow(leaf, host)
                && !IsHostGeometricallyLeftOf(leaf, host)
                && !IsHostGeometricallyRightOf(leaf, host))
            {
                var transitionLeaf = ResolveHostLeaf(editor.FrameRoot, element, host);
                if (transitionLeaf is null
                    || (!IsAdjacent(leaf, transitionLeaf)
                        && transitionLeaf.NodeId != leaf.NodeId
                        && !ReferenceEquals(transitionLeaf, leaf)))
                {
                    continue;
                }
            }

            // Курсор над переходником — упор снизу во внутреннюю верхнюю грань.
            if (probeY >= host.OuterTop - CadMath.MmPrecision
                || (probeY > host.MountingTop && probeY >= (host.OuterTop + host.OuterBottom) / 2.0))
            {
                var face = host.InnerTopFace(probeX);
                if (face > bottom - CadMath.MmPrecision)
                {
                    bottom = Math.Max(bottom, face);
                    pinsBottom = true;
                }
            }
            // Курсор под переходником — упор сверху во внутреннюю нижнюю грань.
            else if (probeY <= host.OuterBottom + CadMath.MmPrecision
                     || (probeY < host.MountingBottom && probeY < (host.OuterTop + host.OuterBottom) / 2.0))
            {
                var face = host.InnerBottomFace(probeX);
                if (face < top + CadMath.MmPrecision)
                {
                    top = Math.Min(top, face);
                    pinsTop = true;
                }
            }
            // Курсор внутри тела переходника по Y — смотрим горизонтальное примыкание.
            else if (probeX >= host.OuterRight - CadMath.MmPrecision
                     || probeX > host.MountingRight)
            {
                var face = host.InnerRightFace(probeY);
                if (face > left - CadMath.MmPrecision)
                {
                    left = Math.Max(left, face);
                    pinsLeft = true;
                }
            }
            else if (probeX <= host.OuterLeft + CadMath.MmPrecision
                     || probeX < host.MountingLeft)
            {
                var face = host.InnerLeftFace(probeY);
                if (face < right + CadMath.MmPrecision)
                {
                    right = Math.Min(right, face);
                    pinsRight = true;
                }
            }
            else
            {
                // Точка внутри тела профиля — проём недопустим.
                left = right = CadMath.RoundMm((left + right) / 2.0);
                bottom = top = CadMath.RoundMm((bottom + top) / 2.0);
                return;
            }
        }

        left = CadMath.RoundMm(left);
        bottom = CadMath.RoundMm(bottom);
        right = CadMath.RoundMm(right);
        top = CadMath.RoundMm(top);
    }

    /// <summary>
    /// Вертикальный импост: верх — внутренняя нижняя грань верхнего профиля,
    /// низ — внутренняя верхняя грань нижнего профиля (идеальный T-стык).
    /// </summary>
    public static void ClipVerticalSpanAtTransitionShelves(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX,
        ref double bottom,
        ref double top)
    {
        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(editor, element, editor.FrameRoot, editor.FrameThickness, out var host))
            {
                continue;
            }

            // Ось внутри тела переходного профиля — подрезка по внутренним горизонтальным полкам.
            if (impostAxisX > host.OuterLeft + CadMath.MmPrecision
                && impostAxisX < host.OuterRight - CadMath.MmPrecision)
            {
                if (top <= host.OuterBottom + CadMath.Epsilon || bottom >= host.OuterTop - CadMath.Epsilon)
                {
                    continue;
                }

                bottom = Math.Max(bottom, host.InnerBottomFace(impostAxisX));
                ApplyTransitionHostVerticalImpostTopJoint(splitNode, impostAxisX, host, ref top);
                continue;
            }

            // Ось снаружи, но в зоне примыкания по Y: T-стык к вертикальной внутренней полке
            // уже задаётся через ResolveHostInnerOpeningBounds (Left/Right).
            // Здесь дополнительно ограничиваем пролёт по горизонтальным полкам соседнего хоста,
            // если splitNode перекрывается с переходным профилем по высоте.
            var transitionLeaf = ResolveHostLeaf(editor.FrameRoot, element, host);
            if (transitionLeaf is null)
            {
                continue;
            }

            if (!RangesOverlap(splitNode.Y, splitNode.Y + splitNode.Height, host.OuterBottom, host.OuterTop))
            {
                continue;
            }

            // Вертикальный импост соседней ячейки: низ/верх — внутренние грани переходного хоста,
            // если ячейка полностью лежит в пределах высоты переходного профиля.
            if (IsHostRightOfTransition(splitNode, transitionLeaf)
                || IsHostLeftOfTransition(splitNode, transitionLeaf)
                || IsNodeChildAdjacentToTransition(splitNode, transitionLeaf))
            {
                // Не сужаем полный вертикальный пролёт соседней колонны до высоты переходного —
                // горизонтальные T-стыки обеспечивает PinFrame / nested frames.
                // Здесь ничего не делаем: Left/Right уже заданы HostInnerOpening.
            }
        }

        if (top < bottom)
        {
            top = bottom;
        }
    }

    /// <summary>
    /// Горизонтальный импост упирается во внутреннюю левую/правую полку переходного профиля.
    /// </summary>
    public static void ClipHorizontalSpanAtTransitionShelves(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisY,
        ref double left,
        ref double right)
    {
        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(editor, element, editor.FrameRoot, editor.FrameThickness, out var host))
            {
                continue;
            }

            if (impostAxisY <= host.OuterBottom + CadMath.MmPrecision
                || impostAxisY >= host.OuterTop - CadMath.MmPrecision)
            {
                continue;
            }

            if (right <= host.OuterLeft + CadMath.Epsilon || left >= host.OuterRight - CadMath.Epsilon)
            {
                continue;
            }

            // Ось проходит через тело переходного профиля — упор во внутренние вертикальные полки.
            left = Math.Max(left, host.InnerLeftFace(impostAxisY));
            right = Math.Min(right, host.InnerRightFace(impostAxisY));
        }

        if (right < left)
        {
            right = left;
        }
    }

    /// <summary>
    /// Подрезка горизонтального импоста по внутренним полкам переходного профиля
    /// (аналог ClipVerticalImpostSpanAtTransitionHosts для ригеля).
    /// </summary>
    public static void ClipHorizontalImpostSpanAtTransitionHosts(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisY,
        ref double left,
        ref double right)
    {
        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(
                    editor,
                    element,
                    editor.FrameRoot,
                    editor.FrameThickness,
                    out var host))
            {
                continue;
            }

            if (!ImpostRowInteractsWithTransitionHost(editor, splitNode, impostAxisY, element, host))
            {
                continue;
            }

            if (impostAxisY <= host.OuterBottom + CadMath.Epsilon
                || impostAxisY >= host.OuterTop - CadMath.Epsilon)
            {
                continue;
            }

            var transitionLeaf = ResolveHostLeaf(editor.FrameRoot, element, host);
            if (transitionLeaf is null)
            {
                continue;
            }

            if (IsSplitSubtreeLeftOfTransition(splitNode, transitionLeaf))
            {
                ApplyTransitionHostHorizontalImpostRightJoint(
                    splitNode,
                    impostAxisY,
                    transitionLeaf,
                    host,
                    ref right);
            }
            else if (IsHostRightOfTransition(splitNode, transitionLeaf))
            {
                left = Math.Max(left, host.InnerRightFace(impostAxisY));
            }
            else if (impostAxisY > host.OuterBottom + CadMath.MmPrecision
                     && impostAxisY < host.OuterTop - CadMath.MmPrecision)
            {
                left = Math.Max(left, host.InnerLeftFace(impostAxisY));
                right = Math.Min(right, host.InnerRightFace(impostAxisY));
            }
        }

        if (right < left)
        {
            right = left;
        }
    }

    /// <summary>
    /// Финальный snap правого конца ригеля к внутренней полке переходного профиля.
    /// Принудительно дотягивает торец, если ячейка слева от переходника (зеркало вертикального PinTop).
    /// </summary>
    public static void PinHorizontalImpostRightToTransitionHostInnerShelf(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisY,
        ref double right)
    {
        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(
                    editor,
                    element,
                    editor.FrameRoot,
                    editor.FrameThickness,
                    out var host))
            {
                continue;
            }

            if (!ImpostRowInteractsWithTransitionHost(editor, splitNode, impostAxisY, element, host))
            {
                continue;
            }

            var transitionLeaf = ResolveHostLeaf(editor.FrameRoot, element, host);
            if (transitionLeaf is null || !IsSplitSubtreeLeftOfTransition(splitNode, transitionLeaf))
            {
                continue;
            }

            var probeY = ClampProbeY(impostAxisY, host);
            var junctionOuterX = host.OuterLeft;
            var innerRight = CadMath.RoundMm(host.InnerLeftFace(probeY));

            // Принудительный snap: nested-frame clip мог оставить микрозазор 1–4 мм до полки переходника.
            if (SashGeometry.HorizontalImpostRowTouchesFrameRight(
                    splitNode,
                    junctionOuterX,
                    innerRight,
                    right,
                    impostAxisY)
                || right < innerRight - CadMath.MmPrecision)
            {
                right = innerRight;
            }
        }
    }

    /// <summary>
    /// Т-стык правого конца горизонтального импоста с внутренней полкой переходного профиля.
    /// </summary>
    private static void ApplyTransitionHostHorizontalImpostRightJoint(
        FrameNode splitNode,
        double impostAxisY,
        FrameNode transitionLeaf,
        in TransitionFrameHostGeometry host,
        ref double right)
    {
        var probeY = ClampProbeY(impostAxisY, host);
        var junctionOuterX = host.OuterLeft;
        var innerRight = CadMath.RoundMm(host.InnerLeftFace(probeY));

        if (IsSplitSubtreeLeftOfTransition(splitNode, transitionLeaf))
        {
            right = innerRight;
            return;
        }

        if (SashGeometry.HorizontalImpostRowTouchesFrameRight(
                splitNode,
                junctionOuterX,
                innerRight,
                right,
                impostAxisY))
        {
            right = innerRight;
            return;
        }

        right = Math.Min(right, innerRight);
    }

    private static double ClampProbeY(double impostAxisY, in TransitionFrameHostGeometry host) =>
        Math.Clamp(impostAxisY, host.OuterBottom, host.OuterTop);

    private static FrameNode ResolveHorizontalImpostInteractionNode(FrameNode splitNode, double impostAxisY) =>
        SashGeometry.TryGetHorizontalImpostRowCell(splitNode, impostAxisY, out var rowCell)
            ? rowCell
            : splitNode;

    /// <summary>
    /// Импостный узел или его дочерние/родительские ячейки слева от переходного профиля.
    /// </summary>
    private static bool IsSplitSubtreeLeftOfTransition(FrameNode splitNode, FrameNode transitionLeaf)
    {
        if (IsHostLeftOfTransition(splitNode, transitionLeaf))
        {
            return true;
        }

        if (splitNode.ChildA is not null && IsHostLeftOfTransition(splitNode.ChildA, transitionLeaf))
        {
            return true;
        }

        if (splitNode.ChildB is not null && IsHostLeftOfTransition(splitNode.ChildB, transitionLeaf))
        {
            return true;
        }

        for (var node = splitNode.Parent; node is not null; node = node.Parent)
        {
            if (IsHostLeftOfTransition(node, transitionLeaf))
            {
                return true;
            }
        }

        return IsNodeChildAdjacentToTransition(splitNode, transitionLeaf)
               && ((splitNode.ChildA is not null && IsHostLeftOfTransition(splitNode.ChildA, transitionLeaf))
                   || (splitNode.ChildB is not null && IsHostLeftOfTransition(splitNode.ChildB, transitionLeaf)));
    }

    private static bool ImpostRowInteractsWithTransitionHost(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisY,
        GeometryElement element,
        in TransitionFrameHostGeometry host)
    {
        if (impostAxisY > host.OuterBottom + CadMath.MmPrecision
            && impostAxisY < host.OuterTop - CadMath.MmPrecision
            && splitNode.X + splitNode.Width > host.OuterLeft + CadMath.MmPrecision
            && splitNode.X < host.OuterRight - CadMath.MmPrecision)
        {
            return true;
        }

        if (editor.FrameRoot is null)
        {
            return false;
        }

        var transitionLeaf = ResolveHostLeaf(editor.FrameRoot, element, host);
        if (transitionLeaf is null)
        {
            return false;
        }

        var interactionNode = ResolveHorizontalImpostInteractionNode(splitNode, impostAxisY);
        if (!RangesOverlap(
                interactionNode.Y,
                interactionNode.Y + interactionNode.Height,
                host.OuterBottom,
                host.OuterTop)
            && !RangesOverlap(
                splitNode.Y,
                splitNode.Y + splitNode.Height,
                host.OuterBottom,
                host.OuterTop))
        {
            return false;
        }

        if (IsSplitSubtreeLeftOfTransition(splitNode, transitionLeaf)
            || IsHostRightOfTransition(splitNode, transitionLeaf)
            || IsHostRightOfTransition(interactionNode, transitionLeaf)
            || IsNodeChildAdjacentToTransition(splitNode, transitionLeaf))
        {
            return RangesOverlap(
                interactionNode.X,
                interactionNode.X + interactionNode.Width,
                host.OuterLeft,
                host.OuterRight)
                || RangesOverlap(
                    splitNode.X,
                    splitNode.X + splitNode.Width,
                    host.OuterLeft,
                    host.OuterRight);
        }

        return false;
    }

    /// <summary>
    /// Подрезка вертикального импоста по внутренним полкам переходного профиля
    /// (аналог ClipVerticalImpostSpanAtNestedFrames для Frame).
    /// </summary>
    public static void ClipVerticalImpostSpanAtTransitionHosts(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX,
        ref double bottom,
        ref double top)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(
                    editor,
                    element,
                    editor.FrameRoot,
                    editor.FrameThickness,
                    out var host))
            {
                continue;
            }

            if (!ImpostColumnInteractsWithTransitionHost(editor, splitNode, impostAxisX, element, host))
            {
                continue;
            }

            if (host.OuterTop <= bottom + CadMath.Epsilon || host.OuterBottom >= top - CadMath.Epsilon)
            {
                continue;
            }

            var probeX = ClampProbeX(impostAxisX, host);
            bottom = Math.Max(bottom, CadMath.RoundMm(host.InnerBottomFace(probeX)));
            ApplyTransitionHostVerticalImpostTopJoint(splitNode, impostAxisX, host, ref top);
        }

        if (top < bottom)
        {
            top = bottom;
        }
    }

    /// <summary>
    /// Финальный snap верха к внутренней полке переходного профиля
    /// (после прочих подрезок, которые могут оставить микрозазор от сетки).
    /// </summary>
    public static void PinVerticalImpostTopToTransitionHostInnerShelf(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX,
        ref double top)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            if (!TryResolveHostGeometry(
                    editor,
                    element,
                    editor.FrameRoot,
                    editor.FrameThickness,
                    out var host))
            {
                continue;
            }

            if (!ImpostColumnInteractsWithTransitionHost(editor, splitNode, impostAxisX, element, host))
            {
                continue;
            }

            var probeX = ClampProbeX(impostAxisX, host);
            var innerTop = CadMath.RoundMm(host.InnerTopFace(probeX));
            if (SashGeometry.VerticalImpostColumnTouchesFrameTop(
                    splitNode,
                    impostAxisX,
                    host.OuterTop,
                    innerTop,
                    top))
            {
                top = innerTop;
            }
        }
    }

    /// <summary>
    /// Т-стык верха вертикального импоста с внутренней полкой переходного профиля.
    /// </summary>
    private static void ApplyTransitionHostVerticalImpostTopJoint(
        FrameNode splitNode,
        double impostAxisX,
        in TransitionFrameHostGeometry host,
        ref double top)
    {
        var probeX = ClampProbeX(impostAxisX, host);
        var innerTop = CadMath.RoundMm(host.InnerTopFace(probeX));
        if (SashGeometry.VerticalImpostColumnTouchesFrameTop(
                splitNode,
                impostAxisX,
                host.OuterTop,
                innerTop,
                top))
        {
            top = innerTop;
            return;
        }

        top = Math.Min(top, innerTop);
    }

    private static double ClampProbeX(double impostAxisX, in TransitionFrameHostGeometry host) =>
        Math.Clamp(impostAxisX, host.OuterLeft, host.OuterRight);

    private static bool ImpostColumnInteractsWithTransitionHost(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX,
        GeometryElement element,
        in TransitionFrameHostGeometry host)
    {
        if (impostAxisX > host.OuterLeft + CadMath.MmPrecision
            && impostAxisX < host.OuterRight - CadMath.MmPrecision)
        {
            return true;
        }

        if (editor.FrameRoot is null)
        {
            return false;
        }

        var transitionLeaf = ResolveHostLeaf(editor.FrameRoot, element, host);
        if (transitionLeaf is null)
        {
            return false;
        }

        if (!IsHostRightOfTransition(splitNode, transitionLeaf)
            && !IsHostLeftOfTransition(splitNode, transitionLeaf)
            && !IsNodeChildAdjacentToTransition(splitNode, transitionLeaf))
        {
            return false;
        }

        return RangesOverlap(
            splitNode.Y,
            splitNode.Y + splitNode.Height,
            host.OuterBottom,
            host.OuterTop);
    }

    public static bool TryResolveHostGeometry(
        CadEditorViewModel editor,
        GeometryElement element,
        FrameNode? frameRoot,
        double frameThickness,
        out TransitionFrameHostGeometry host)
    {
        host = default;
        if (element.Kind != GeometryElementKind.TransitionFrame)
        {
            return false;
        }

        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return false;
        }

        var profileThickness = ResolveTransitionProfileThickness(element);
        var halfThickness = FrameProfileGeometry.ResolveProfileThickness(width, height, profileThickness) / 2.0;
        if (halfThickness <= CadMath.Epsilon)
        {
            return false;
        }

        var outerLeft = originX;
        var outerBottom = originY;
        var outerRight = originX + width;
        var outerTop = originY + height;

        var transitionLeaf = frameRoot?.FindLeafAt((double)element.X, (double)element.Y);
        if (transitionLeaf is not null && frameRoot is not null)
        {
            SashGeometry.ApplyExtremeImpostJunctionToTransitionBandOuter(
                transitionLeaf,
                frameRoot,
                frameThickness,
                ref outerLeft,
                ref outerBottom,
                ref outerRight,
                ref outerTop);
        }

        if (outerRight <= outerLeft + CadMath.Epsilon || outerTop <= outerBottom + CadMath.Epsilon)
        {
            return false;
        }

        double innerLeftBottom;
        double innerRightBottom;
        double innerLeftTop;
        double innerRightTop;
        double innerBottomLeft;
        double innerTopLeft;
        double innerBottomRight;
        double innerTopRight;

        if (transitionLeaf is not null && frameRoot is not null)
        {
            SashGeometry.ResolveTransitionBandInnerCorners(
                transitionLeaf,
                frameRoot,
                frameThickness,
                outerLeft,
                outerBottom,
                outerRight,
                outerTop,
                halfThickness,
                out innerLeftBottom,
                out innerRightBottom,
                out innerLeftTop,
                out innerRightTop,
                out innerBottomLeft,
                out innerTopLeft,
                out innerBottomRight,
                out innerTopRight);
        }
        else
        {
            innerLeftBottom = innerLeftTop = outerLeft + halfThickness;
            innerRightBottom = innerRightTop = outerRight - halfThickness;
            innerBottomLeft = innerBottomRight = outerBottom + halfThickness;
            innerTopLeft = innerTopRight = outerTop - halfThickness;
        }

        host = new TransitionFrameHostGeometry(
            CadMath.RoundMm(outerLeft),
            CadMath.RoundMm(outerBottom),
            CadMath.RoundMm(outerRight),
            CadMath.RoundMm(outerTop),
            CadMath.RoundMm(innerLeftBottom),
            CadMath.RoundMm(innerRightBottom),
            CadMath.RoundMm(innerLeftTop),
            CadMath.RoundMm(innerRightTop),
            CadMath.RoundMm(innerBottomLeft),
            CadMath.RoundMm(innerBottomRight),
            CadMath.RoundMm(innerTopLeft),
            CadMath.RoundMm(innerTopRight),
            profileThickness,
            halfThickness);
        return true;
    }

    public static double ResolveTransitionProfileThickness(GeometryElement element)
    {
        var material = ConstructionMaterialCatalog.FindById(element.MaterialId);
        if (material is { ProfileWidth: > 0 })
        {
            return material.ProfileWidth;
        }

        var series = ProfileSeriesCatalog.FindById(element.ProfileSeriesId);
        if (series is { SystemDepth: > 0 })
        {
            return series.SystemDepth;
        }

        return ConstructionMaterialCatalog.ForKind(GeometryElementKind.TransitionFrame).FirstOrDefault()?.ProfileWidth
            ?? 46;
    }

    public static bool IsTransitionHostEdge(
        CadEditorViewModel editor,
        FrameNode hostNode,
        bool left,
        bool right,
        bool bottom,
        bool top)
    {
        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return false;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            var transitionLeaf = editor.FrameRoot.FindLeafAt((double)element.X, (double)element.Y);
            if (transitionLeaf is null)
            {
                continue;
            }

            if (!TryResolveHostGeometry(editor, element, editor.FrameRoot, editor.FrameThickness, out var host))
            {
                if (left && IsHostRightOfTransition(hostNode, transitionLeaf))
                {
                    return true;
                }

                if (right && IsHostLeftOfTransition(hostNode, transitionLeaf))
                {
                    return true;
                }

                if (bottom && IsHostAboveTransition(hostNode, transitionLeaf))
                {
                    return true;
                }

                if (top && IsHostBelowTransition(hostNode, transitionLeaf))
                {
                    return true;
                }

                continue;
            }

            if (left && (IsHostRightOfTransition(hostNode, transitionLeaf) || IsHostGeometricallyRightOf(hostNode, host)))
            {
                return true;
            }

            if (right && (IsHostLeftOfTransition(hostNode, transitionLeaf) || IsHostGeometricallyLeftOf(hostNode, host)))
            {
                return true;
            }

            if (bottom && (IsHostAboveTransition(hostNode, transitionLeaf) || IsHostGeometricallyAbove(hostNode, host)))
            {
                return true;
            }

            if (top && (IsHostBelowTransition(hostNode, transitionLeaf) || IsHostGeometricallyBelow(hostNode, host)))
            {
                return true;
            }
        }

        return false;
    }

    private static FrameNode? ResolveHostLeaf(
        FrameNode frameRoot,
        GeometryElement element,
        in TransitionFrameHostGeometry host)
    {
        var byCenter = frameRoot.FindLeafAt((double)element.X, (double)element.Y);
        if (byCenter is not null)
        {
            return byCenter;
        }

        return frameRoot.FindLeafAt(
            (host.OuterLeft + host.OuterRight) / 2.0,
            (host.OuterBottom + host.OuterTop) / 2.0);
    }

    private static double InterpolateVertical(
        double valueAtBottom,
        double valueAtTop,
        double yBottom,
        double yTop,
        double probeY)
    {
        var span = yTop - yBottom;
        if (Math.Abs(span) <= CadMath.Epsilon)
        {
            return CadMath.RoundMm(valueAtBottom);
        }

        var t = Math.Clamp((probeY - yBottom) / span, 0.0, 1.0);
        return CadMath.RoundMm(valueAtBottom + (valueAtTop - valueAtBottom) * t);
    }

    private static double InterpolateHorizontal(
        double valueAtLeft,
        double valueAtRight,
        double xLeft,
        double xRight,
        double probeX)
    {
        var span = xRight - xLeft;
        if (Math.Abs(span) <= CadMath.Epsilon)
        {
            return CadMath.RoundMm(valueAtLeft);
        }

        var t = Math.Clamp((probeX - xLeft) / span, 0.0, 1.0);
        return CadMath.RoundMm(valueAtLeft + (valueAtRight - valueAtLeft) * t);
    }

    private static bool IsNodeChildAdjacentToTransition(FrameNode splitNode, FrameNode transitionLeaf)
    {
        if (splitNode.ChildA is not null && IsAdjacent(splitNode.ChildA, transitionLeaf))
        {
            return true;
        }

        if (splitNode.ChildB is not null && IsAdjacent(splitNode.ChildB, transitionLeaf))
        {
            return true;
        }

        return false;
    }

    private static bool IsAdjacent(FrameNode a, FrameNode b) =>
        IsHostRightOfTransition(a, b)
        || IsHostLeftOfTransition(a, b)
        || IsHostAboveTransition(a, b)
        || IsHostBelowTransition(a, b);

    private static bool IsHostRightOfTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        EdgesTouchWithImpostGap(hostNode.X, transitionLeaf.X + transitionLeaf.Width)
        && RangesOverlap(hostNode.Y, hostNode.Y + hostNode.Height, transitionLeaf.Y, transitionLeaf.Y + transitionLeaf.Height);

    private static bool IsHostLeftOfTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        EdgesTouchWithImpostGap(transitionLeaf.X, hostNode.X + hostNode.Width)
        && RangesOverlap(hostNode.Y, hostNode.Y + hostNode.Height, transitionLeaf.Y, transitionLeaf.Y + transitionLeaf.Height);

    private static bool IsHostAboveTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        EdgesTouchWithImpostGap(hostNode.Y, transitionLeaf.Y + transitionLeaf.Height)
        && RangesOverlap(hostNode.X, hostNode.X + hostNode.Width, transitionLeaf.X, transitionLeaf.X + transitionLeaf.Width);

    private static bool IsHostBelowTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        EdgesTouchWithImpostGap(transitionLeaf.Y, hostNode.Y + hostNode.Height)
        && RangesOverlap(hostNode.X, hostNode.X + hostNode.Width, transitionLeaf.X, transitionLeaf.X + transitionLeaf.Width);

    /// <summary>
    /// Соседство с допуском на толщину физического импоста между листьями дерева.
    /// </summary>
    private static bool EdgesTouchWithImpostGap(double higherEdge, double lowerEdge) =>
        higherEdge >= lowerEdge - CadMath.MmPrecision
        && higherEdge <= lowerEdge + FrameNode.DefaultImpostProfileMm + CadMath.MmPrecision;

    private static bool IsHostGeometricallyRightOf(FrameNode hostNode, in TransitionFrameHostGeometry host) =>
        EdgesTouchWithImpostGap(hostNode.X, host.OuterRight)
        && RangesOverlap(hostNode.Y, hostNode.Y + hostNode.Height, host.OuterBottom, host.OuterTop);

    private static bool IsHostGeometricallyLeftOf(FrameNode hostNode, in TransitionFrameHostGeometry host) =>
        EdgesTouchWithImpostGap(host.OuterLeft, hostNode.X + hostNode.Width)
        && RangesOverlap(hostNode.Y, hostNode.Y + hostNode.Height, host.OuterBottom, host.OuterTop);

    private static bool IsHostGeometricallyAbove(FrameNode hostNode, in TransitionFrameHostGeometry host) =>
        EdgesTouchWithImpostGap(hostNode.Y, host.OuterTop)
        && RangesOverlap(hostNode.X, hostNode.X + hostNode.Width, host.OuterLeft, host.OuterRight);

    private static bool IsHostGeometricallyBelow(FrameNode hostNode, in TransitionFrameHostGeometry host) =>
        EdgesTouchWithImpostGap(host.OuterBottom, hostNode.Y + hostNode.Height)
        && RangesOverlap(hostNode.X, hostNode.X + hostNode.Width, host.OuterLeft, host.OuterRight);

    private static bool RangesOverlap(double a0, double a1, double b0, double b1)
    {
        var minA = Math.Min(a0, a1);
        var maxA = Math.Max(a0, a1);
        var minB = Math.Min(b0, b1);
        var maxB = Math.Max(b0, b1);
        return maxA > minB + CadMath.MmPrecision && maxB > minA + CadMath.MmPrecision;
    }

    private static (double X, double Y, double W, double H) GetWorldOriginBounds(GeometryElement element)
    {
        var w = (double)element.WidthMm;
        var h = (double)element.HeightMm;
        return ((double)element.X - w / 2.0, (double)element.Y - h / 2.0, w, h);
    }
}
