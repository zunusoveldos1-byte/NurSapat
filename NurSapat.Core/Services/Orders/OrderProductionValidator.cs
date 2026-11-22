using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Services.Orders;

public sealed record ProductionIssue(string Message, Guid? ConstructionId = null, Guid? ElementId = null,
    bool BlocksProduction = true);

public static class OrderProductionValidator
{
    public static IReadOnlyList<ProductionIssue> Review(Order order, string? draftOrderNumber = null)
    {
        var issues = new List<ProductionIssue>();
        if (string.IsNullOrWhiteSpace(draftOrderNumber ?? order.Header.OrderNumber))
        {
            issues.Add(new("Укажите номер заказа."));
        }

        var constructions = order.Constructions.Where(item => item.IsSelectedForCalculation).ToList();
        if (constructions.Count == 0)
        {
            issues.Add(new("Выберите хотя бы одну конструкцию для расчёта."));
        }

        foreach (var construction in constructions)
        {
            ReviewConstruction(construction, issues);
        }

        return issues;
    }

    private static void ReviewConstruction(OrderConstruction construction, List<ProductionIssue> issues)
    {
        void Add(string message, GeometryElement? element = null, bool blocksProduction = true) =>
            issues.Add(new ProductionIssue(
                $"Конструкция {construction.Number} · {message}", construction.Id, element?.Id, blocksProduction));

        var elements = construction.Geometry.Elements
            .Where(element => !FictitiousFrame.IsFictitiousElement(element))
            .ToList();
        if (elements.Count == 0)
        {
            Add("Нет элементов чертежа.");
            return;
        }

        var bounds = construction.Geometry.ResolveBillableOuterBounds();
        if (bounds.WidthMm <= 0 || bounds.HeightMm <= 0)
        {
            Add("Проверьте габариты и материалы конструкции.");
        }

        var frames = elements.Where(element => element.Kind is GeometryElementKind.Frame
            or GeometryElementKind.ArchFrame).ToList();
        var frameSeries = frames.Select(element => element.ProfileSeriesId).OfType<Guid>().Distinct().ToList();
        var hardware = elements.Where(element => element.Kind is GeometryElementKind.Hardware or GeometryElementKind.Handle
            && element.MaterialId is not null).ToList();
        foreach (var element in elements)
        {
            var sash = element.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash;
            if (!sash && element.Kind is not (GeometryElementKind.Frame or GeometryElementKind.ArchFrame or GeometryElementKind.Glazing))
            {
                continue;
            }

            var label = string.IsNullOrWhiteSpace(element.Label) ? LabelFor(element.Kind) : element.Label;
            if (element.WidthMm <= 0 || element.HeightMm <= 0)
            {
                Add($"{label} · заполните ширину и высоту.", element);
            }
            if (element.MaterialId is null || element.MaterialId == Guid.Empty)
            {
                Add($"{label} · материал не назначен.", element);
            }
            if (sash && element.ProfileSeriesId is Guid series && frameSeries.Count == 1 && frameSeries[0] != series)
            {
                Add($"{label} · серия створки отличается от серии рамы. Проверьте совместимость.", element, false);
            }
            if (sash && element.OpeningType is not (null or SashOpeningType.Fixed)
                && !element.HasHardware && !hardware.Any(item => item.HostSashId == element.Id))
            {
                Add($"{label} · для открывающейся створки не указана фурнитура.", element);
            }
        }

        var lines = construction.SpecificationLines.Where(line => line.Category != CostMaterialCategory.Labor).ToList();
        if (lines.Count == 0)
        {
            Add("Спецификация материалов пуста. Выполните расчёт.");
        }
        foreach (var line in lines.Where(line => line.UnitPrice <= 0 || line.Quantity <= 0))
        {
            Add($"{line.Article} · проверьте цену и количество в спецификации.");
        }
    }

    private static string LabelFor(GeometryElementKind kind) => kind switch
    {
        GeometryElementKind.Glazing => "Стеклопакет",
        GeometryElementKind.Sash or GeometryElementKind.SlidingSash => "Створка",
        _ => "Рама"
    };
}
