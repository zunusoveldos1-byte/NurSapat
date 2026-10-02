using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

/// <summary>
/// Универсальный резолвер несущих хостов конструкции.
/// Frame, ArchFrame и TransitionFrame равноправны как Host Frame.
/// </summary>
internal static class ConstructionHostResolver
{
    private const double MinHostLeafOverlapFraction = 0.2;

    public static bool IsHostFrameKind(GeometryElementKind kind) =>
        kind is GeometryElementKind.Frame
            or GeometryElementKind.ArchFrame
            or GeometryElementKind.TransitionFrame;

    public static bool IsHostFrameElement(GeometryElement element) =>
        IsHostFrameKind(element.Kind) && !DummyFrame.IsDummyElement(element);

    public static bool HasConstructionHostOnCanvas(CadEditorViewModel editor) =>
        editor.CanvasElements.Any(IsHostFrameElement);

    /// <summary>
    /// Несущий элемент в ячейке. Хост привязан к поддереву HostFrameNodeId —
    /// не «протекает» в соседние колонки/контейнеры.
    /// </summary>
    public static GeometryElement? FindHostFrameInLeaf(CadEditorViewModel editor, FrameNode leaf)
    {
        GeometryElement? best = null;
        var bestScore = 0.0;

        foreach (var element in editor.CanvasElements)
        {
            if (!IsHostFrameElement(element))
            {
                continue;
            }

            if (!TryScoreHostInLeaf(editor.FrameRoot, leaf, element, out var score) || score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            best = element;
        }

        return best;
    }

    public static bool CanHostSash(CadEditorViewModel editor, FrameNode leaf)
    {
        // Створка только в терминальных листьях: узлы с дочерними ячейками (HasChildren) не принимают створку.
        if (!leaf.IsTerminalLeaf || leaf.HasChildCells)
        {
            return false;
        }

        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return true;
        }

        if (FindHostFrameInLeaf(editor, leaf) is not null)
        {
            return true;
        }

        if (TransitionAdjacentFrameGeometry.IsTransitionHostEdge(
            editor,
            leaf,
            left: true,
            right: true,
            bottom: true,
            top: true))
        {
            return true;
        }

        // Виртуальная граница (сквозная ось импоста соседней колонки/ряда) замыкает проём:
        // ячейка валидна, если к ней примыкает несущий хост соседнего листа.
        return HasAdjacentHostAcrossVirtualBoundary(editor, leaf);
    }

    /// <summary>
    /// Лист замкнут виртуальной границей сетки и имеет соседний Host Frame
    /// (рама / переходной профиль) через общую ось или ребро.
    /// </summary>
    private static bool HasAdjacentHostAcrossVirtualBoundary(CadEditorViewModel editor, FrameNode leaf)
    {
        if (editor.FrameRoot is null)
        {
            return false;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (!IsHostFrameElement(element))
            {
                continue;
            }

            if (!TryGetElementWorldBounds(element, out var left, out var bottom, out var right, out var top))
            {
                continue;
            }

            var sharesVerticalEdge =
                EdgesNear(leaf.X, right) || EdgesNear(leaf.X + leaf.Width, left);
            var sharesHorizontalEdge =
                EdgesNear(leaf.Y, top) || EdgesNear(leaf.Y + leaf.Height, bottom);
            var overlapsY = RangesOverlapSimple(leaf.Y, leaf.Y + leaf.Height, bottom, top);
            var overlapsX = RangesOverlapSimple(leaf.X, leaf.X + leaf.Width, left, right);

            if ((sharesVerticalEdge && overlapsY) || (sharesHorizontalEdge && overlapsX))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EdgesNear(double a, double b) =>
        Math.Abs(a - b) <= FrameNode.DefaultImpostProfileMm + CadMath.MmPrecision;

    private static bool RangesOverlapSimple(double a0, double a1, double b0, double b1) =>
        Math.Min(a1, b1) > Math.Max(a0, b0) + CadMath.MmPrecision;

    public static bool CanHostOpeningFillings(CadEditorViewModel editor, FrameNode leaf) =>
        CanHostSash(editor, leaf);

    public static bool CanHostHardware(CadEditorViewModel editor, FrameNode leaf) =>
        CanHostSash(editor, leaf);

    public static bool SupportsOpening(GeometryElement host) => IsHostFrameElement(host);

    public static bool SupportsHardware(GeometryElement host) => IsHostFrameElement(host);

    public static bool SupportsHandle(GeometryElement host) => IsHostFrameElement(host);

    public static bool TryGetElementWorldBounds(
        GeometryElement element,
        out double left,
        out double bottom,
        out double right,
        out double top)
    {
        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            left = bottom = right = top = 0;
            return false;
        }

        left = (double)element.X - width / 2.0;
        bottom = (double)element.Y - height / 2.0;
        right = left + width;
        top = bottom + height;
        return true;
    }

    public static bool TryMeasureHostLeafOverlap(
        FrameNode leaf,
        GeometryElement host,
        out double overlapArea)
    {
        overlapArea = 0;
        if (!TryGetElementWorldBounds(host, out var left, out var bottom, out var right, out var top))
        {
            return false;
        }

        var iLeft = Math.Max(left, leaf.X);
        var iRight = Math.Min(right, leaf.X + leaf.Width);
        var iBottom = Math.Max(bottom, leaf.Y);
        var iTop = Math.Min(top, leaf.Y + leaf.Height);
        if (iRight <= iLeft + CadMath.Epsilon || iTop <= iBottom + CadMath.Epsilon)
        {
            return false;
        }

        overlapArea = (iRight - iLeft) * (iTop - iBottom);
        return overlapArea > CadMath.Epsilon;
    }

    /// <summary>
    /// Элемент с HostFrameNodeId не должен «протекать» в соседние колонки/контейнеры.
    /// </summary>
    public static bool IsElementBoundToLeafSubtree(
        FrameNode? frameRoot,
        FrameNode leaf,
        GeometryElement element)
    {
        if (element.HostFrameNodeId is not Guid hostId || frameRoot is null)
        {
            return true;
        }

        var boundNode = frameRoot.FindNodeById(hostId);
        return boundNode is not null && SashGeometry.IsInSubtree(leaf, boundNode);
    }

    public static void LogSashPlacementDiagnostics(
        CadEditorViewModel editor,
        FrameNode? leaf,
        string stage,
        string? reason = null)
    {
        var host = leaf is null ? null : FindHostFrameInLeaf(editor, leaf);
        CadDiagnostics.LogInfo(
            $"[SashPlacement] stage={stage}; " +
            $"cell={(leaf is null ? "нет" : "да")}; " +
            $"host={(host is null ? "нет" : "да")}; " +
            $"hostKind={host?.Kind.ToString() ?? "-"}; " +
            $"canHostSash={(leaf is not null && CanHostSash(editor, leaf) ? "да" : "нет")}; " +
            $"hasHostOnCanvas={(HasConstructionHostOnCanvas(editor) ? "да" : "нет")}; " +
            $"hostFrameNodeId={host?.HostFrameNodeId?.ToString() ?? "-"}; " +
            $"reason={reason ?? "-"}");
    }

    private static bool TryScoreHostInLeaf(
        FrameNode? frameRoot,
        FrameNode leaf,
        GeometryElement element,
        out double score)
    {
        score = 0;

        if (element.HostFrameNodeId is Guid hostId
            && frameRoot is not null
            && frameRoot.FindNodeById(hostId) is FrameNode boundNode)
        {
            if (!SashGeometry.IsInSubtree(leaf, boundNode))
            {
                return false;
            }

            // Хост, привязанный к промежуточному контейнеру после импоста, не обслуживает дочерние ячейки.
            if (!boundNode.IsTerminalLeaf && boundNode.NodeId != leaf.NodeId)
            {
                return false;
            }
        }

        if (element.HostFrameNodeId == leaf.NodeId)
        {
            score = 1_000_000;
            return true;
        }

        if (leaf.Contains((double)element.X, (double)element.Y))
        {
            score = 500_000 + (double)element.WidthMm * (double)element.HeightMm;
            return true;
        }

        if (!TryMeasureHostLeafOverlap(leaf, element, out var overlapArea))
        {
            return false;
        }

        var leafArea = leaf.Width * leaf.Height;
        if (leafArea <= CadMath.Epsilon)
        {
            return false;
        }

        var fraction = overlapArea / leafArea;
        if (fraction < MinHostLeafOverlapFraction)
        {
            if (!IsHostBoundToAncestor(frameRoot, leaf, element) || overlapArea < 100.0)
            {
                return false;
            }
        }

        score = overlapArea;
        return true;
    }

    private static bool IsHostBoundToAncestor(FrameNode? frameRoot, FrameNode leaf, GeometryElement element)
    {
        if (frameRoot is null || element.HostFrameNodeId is not Guid hostId)
        {
            return false;
        }

        for (var node = leaf; node is not null; node = node.Parent)
        {
            if (node.NodeId == hostId)
            {
                return true;
            }
        }

        return false;
    }
}
