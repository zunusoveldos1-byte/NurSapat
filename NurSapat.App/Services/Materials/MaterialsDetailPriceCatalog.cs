using NurSapat.App.Models.Materials;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;
using NurSapat.App.Services.Users;
using NurSapat.Infrastructure.Sync.Diagnostics;

namespace NurSapat.App.Services.Materials;

public sealed class MaterialsDetailPriceCatalog : IMaterialPriceCatalog
{
    private readonly IMaterialsDetailStore _store;
    private readonly IMaterialCatalogResolver _resolver;
    private readonly IUserSessionService _userSessionService;

    public MaterialsDetailPriceCatalog(
        IMaterialsDetailStore store,
        IMaterialCatalogResolver resolver,
        IUserSessionService userSessionService)
    {
        _store = store;
        _resolver = resolver;
        _userSessionService = userSessionService;
    }

    public MaterialPriceQuote? TryGetByMaterialId(Guid materialId)
    {
        if (_userSessionService.IsOffline)
        {
            StartupTrace.Info(
                "[LOG] MaterialsDetailPriceCatalog.TryGetByMaterialId: " +
                "оффлайн-режим, PostgreSQL не вызывается.");
            return null;
        }

        var detail = _store.GetById(materialId);
        return detail is null ? null : ToQuote(detail);
    }

    public MaterialPriceQuote? TryGetForElement(GeometryElement element, GeometryElementKind effectiveKind)
    {
        var materialId = _resolver.ResolveCatalogMaterialId(element.MaterialId);
        if (materialId is Guid resolvedId)
        {
            return TryGetByMaterialId(resolvedId);
        }

        return TryGetByCategory(MapKindToCategory(effectiveKind), ResolveUnitForKind(effectiveKind));
    }

    public MaterialPriceQuote? TryGetByCategory(CostMaterialCategory category, MaterialPriceUnit unit)
    {
        var materialId = _resolver.ResolveByCategory(category, unit);
        return materialId is Guid resolvedId
            ? TryGetByMaterialId(resolvedId)
            : null;
    }

    private MaterialPriceQuote ToQuote(MaterialDetail detail) =>
        new(
            detail.Id,
            detail.Name,
            detail.Article,
            MapMainUnit(detail.MainUnit),
            ResolvePrice(detail),
            detail.WastePercent,
            MaterialEnumLabels.Currency(detail.Currency));

    private static decimal ResolvePrice(MaterialDetail detail)
    {
        var colorPrice = detail.ColorPrices.FirstOrDefault(color => color.IsDefault)?.Price;
        if (colorPrice is > 0)
        {
            return colorPrice.Value;
        }

        return detail.BasePrice;
    }

    private static MaterialPriceUnit MapMainUnit(MaterialMainUnit unit) => unit switch
    {
        MaterialMainUnit.LinearMeter => MaterialPriceUnit.LinearMeter,
        MaterialMainUnit.SquareMeter => MaterialPriceUnit.SquareMeter,
        MaterialMainUnit.Kilogram => MaterialPriceUnit.Kilogram,
        MaterialMainUnit.Set => MaterialPriceUnit.Set,
        _ => MaterialPriceUnit.Piece
    };

    private static MaterialPriceUnit ResolveUnitForKind(GeometryElementKind kind) =>
        kind is GeometryElementKind.Glazing or GeometryElementKind.Corrugation or GeometryElementKind.MosquitoNet
            ? MaterialPriceUnit.SquareMeter
            : kind is GeometryElementKind.Hardware or GeometryElementKind.Handle
                or GeometryElementKind.Accessory or GeometryElementKind.Installation
                ? MaterialPriceUnit.Piece
                : MaterialPriceUnit.LinearMeter;

    private static CostMaterialCategory MapKindToCategory(GeometryElementKind kind) => kind switch
    {
        GeometryElementKind.Glazing or GeometryElementKind.Corrugation => CostMaterialCategory.Glazing,
        GeometryElementKind.Hardware or GeometryElementKind.Handle => CostMaterialCategory.Hardware,
        GeometryElementKind.Bead => CostMaterialCategory.Components,
        GeometryElementKind.Sill or GeometryElementKind.Installation => CostMaterialCategory.Installation,
        _ when ConstructionMaterialCatalog.ForKind(kind).Any() => CostMaterialCategory.Profile,
        _ => CostMaterialCategory.Components
    };
}
