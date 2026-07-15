using System.Diagnostics;
using System.Runtime.CompilerServices;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.Diagnostics;

/// <summary>Временная трассировка координаты горизонтального импоста по цепочке drag → model → render.</summary>
internal static class HorizontalImpostDragDiagnostics
{
    public static bool IsEnabled { get; set; } = true;

    public static void Trace(
        string stage,
        double? ghostAxisY = null,
        double? moveInputAxisY = null,
        double? modelAxisY = null,
        double? renderAxisY = null,
        FrameNode? splitNode = null,
        FrameNode? frameRoot = null,
        bool? moveSucceeded = null,
        string? detail = null,
        [CallerMemberName] string? caller = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        var splitId = splitNode is null ? "null" : $"{splitNode.GetHashCode()}";
        var rootId = frameRoot is null ? "null" : $"{frameRoot.GetHashCode()}";
        var sameRootRef = splitNode is not null && frameRoot is not null && ReferenceEquals(splitNode, frameRoot)
            ? "split==root"
            : splitNode is not null && frameRoot is not null && ReferenceEquals(splitNode.GetRoot(), frameRoot)
                ? "split.in.root"
                : "split!=root";

        Debug.WriteLine(
            $"[HorizontalImpostDrag] stage={stage} caller={caller} " +
            $"ghostY={Fmt(ghostAxisY)} moveInY={Fmt(moveInputAxisY)} modelY={Fmt(modelAxisY)} renderY={Fmt(renderAxisY)} " +
            $"moved={moveSucceeded?.ToString() ?? "-"} split#{splitId} root#{rootId} {sameRootRef}" +
            (string.IsNullOrEmpty(detail) ? string.Empty : $" detail={detail}"));
    }

    public static double? TryReadModelAxisY(FrameNode? splitNode, int? uniformGapIndex = null)
    {
        if (splitNode is null)
        {
            return null;
        }

        if (splitNode.IsUniformSplit && uniformGapIndex is int gapIndex
            && splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var uniformAxis))
        {
            return uniformAxis;
        }

        return splitNode.TryGetHorizontalImpostAxisWorldY(out var axisY) ? axisY : null;
    }

    public static double? TryReadRenderAxisY(FrameNode? splitNode, int? uniformGapIndex = null)
    {
        if (splitNode is null)
        {
            return null;
        }

        if (splitNode.IsUniformSplit && uniformGapIndex is int gapIndex
            && splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var uniformAxis))
        {
            return uniformAxis;
        }

        return splitNode.TryGetHorizontalImpostAxisWorldY(out var axisY) ? axisY : null;
    }

    private static string Fmt(double? value) =>
        value is double v ? v.ToString("F1") : "-";
}
