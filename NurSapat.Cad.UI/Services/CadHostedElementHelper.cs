using NurSapat.Cad.Core.Models;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

/// <summary>Операции с элементами, привязанными к створке или заполнению.</summary>
public static class CadHostedElementHelper
{
    public static GeometryElement? FindHosted(
        IEnumerable<GeometryElement> elements,
        GeometryElementKind kind,
        Guid hostSashId,
        string? sourceToolId = null) =>
        elements.FirstOrDefault(element =>
            element.Kind == kind
            && element.HostSashId == hostSashId
            && (sourceToolId is null || element.SourceToolId == sourceToolId));

    public static IReadOnlyList<GeometryElement> FindAllHosted(
        IEnumerable<GeometryElement> elements,
        Guid hostSashId) =>
        elements.Where(element => element.HostSashId == hostSashId).ToList();

    public static void RemoveHosted(
        IList<GeometryElement> elements,
        Guid hostSashId,
        GeometryElementKind? kind = null,
        string? sourceToolId = null)
    {
        for (var i = elements.Count - 1; i >= 0; i--)
        {
            var element = elements[i];
            if (element.HostSashId != hostSashId)
            {
                continue;
            }

            if (kind is not null && element.Kind != kind)
            {
                continue;
            }

            if (sourceToolId is not null && element.SourceToolId != sourceToolId)
            {
                continue;
            }

            elements.RemoveAt(i);
        }
    }

    public static void RemoveHostedForSash(IList<GeometryElement> elements, Guid sashId)
    {
        RemoveHosted(elements, sashId);
        for (var i = elements.Count - 1; i >= 0; i--)
        {
            if (elements[i].Kind == GeometryElementKind.Sash && elements[i].Id == sashId)
            {
                elements.RemoveAt(i);
                break;
            }
        }
    }

    public static void PurgeOrphanedHostedElements(
        IList<GeometryElement> elements,
        FrameNode? frameRoot)
    {
        var sashIds = elements
            .Where(element => element.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash)
            .Select(element => element.Id)
            .ToHashSet();

        for (var i = elements.Count - 1; i >= 0; i--)
        {
            var element = elements[i];
            if (element.HostSashId is not Guid hostId)
            {
                continue;
            }

            if (!sashIds.Contains(hostId))
            {
                elements.RemoveAt(i);
            }
        }

        if (frameRoot is null)
        {
            return;
        }

        for (var i = elements.Count - 1; i >= 0; i--)
        {
            var element = elements[i];
            if (element.Kind is not (GeometryElementKind.Accessory or GeometryElementKind.Installation))
            {
                continue;
            }

            var cx = (double)element.X;
            var cy = (double)element.Y;
            if (frameRoot.FindLeafAt(cx, cy) is not { IsLeaf: true })
            {
                elements.RemoveAt(i);
            }
        }
    }

    public static Guid? ResolveDefaultMaterialId(GeometryElementKind kind)
    {
        var material = ConstructionMaterialCatalog.ForKind(kind).FirstOrDefault();
        return material?.Id;
    }

    public static SashHingeSide ResolveOppositeSide(SashHingeSide hingeSide) => hingeSide switch
    {
        SashHingeSide.Left => SashHingeSide.Right,
        SashHingeSide.Right => SashHingeSide.Left,
        SashHingeSide.Top => SashHingeSide.Bottom,
        _ => SashHingeSide.Top
    };

    public static SashHingeSide ResolveDefaultHandleSide(SashHingeSide hingeSide, SashOpeningType openingType)
    {
        if (openingType is SashOpeningType.SlideLeft or SashOpeningType.SlideRight)
        {
            return SashHingeSide.Right;
        }

        if (openingType == SashOpeningType.LeftTurn)
        {
            return SashHingeSide.Right; // петли слева → ручка справа
        }

        if (openingType == SashOpeningType.RightTurn)
        {
            return SashHingeSide.Left; // петли справа → ручка слева
        }

        return hingeSide is SashHingeSide.Left or SashHingeSide.Right
            ? ResolveOppositeSide(hingeSide)
            : hingeSide == SashHingeSide.Bottom
                ? SashHingeSide.Top
                : SashHingeSide.Bottom;
    }
}
