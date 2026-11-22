using NurSapat.Core.Interfaces;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Materials;
using NurSapat.Core.Models.Orders;
using NurSapat.App.Services.Users;
using NurSapat.Infrastructure.Sync.Diagnostics;

namespace NurSapat.App.Services.Materials;

public sealed class MaterialCatalogResolver : IMaterialCatalogResolver
{
    private readonly IMaterialsDetailStore _store;
    private readonly IUserSessionService _userSessionService;

    public MaterialCatalogResolver(
        IMaterialsDetailStore store,
        IUserSessionService userSessionService)
    {
        _store = store;
        _userSessionService = userSessionService;
    }

    public Guid? ResolveCatalogMaterialId(Guid? constructionMaterialId)
    {
        var constructionMaterial = ConstructionMaterialCatalog.FindById(constructionMaterialId);
        return ResolveByArticle(constructionMaterial?.CatalogArticle);
    }

    public Guid? ResolveByArticle(string? article)
    {
        if (_userSessionService.IsOffline)
        {
            StartupTrace.Info(
                "[LOG] MaterialCatalogResolver.ResolveByArticle: " +
                "оффлайн-режим, PostgreSQL не вызывается.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(article))
        {
            return null;
        }

        if (CatalogMaterialIds.TryFromArticle(article) is Guid stableId
            && _store.GetById(stableId) is not null)
        {
            return stableId;
        }

        return _store.GetAll()
            .FirstOrDefault(item => string.Equals(item.Article, article, StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    public Guid? ResolveByCategory(CostMaterialCategory category, MaterialPriceUnit unit) =>
        MapCategory(category, unit) switch
        {
            (string article, _) when !string.IsNullOrWhiteSpace(article) => ResolveByArticle(article),
            _ => null
        };

    private static (string? article, MaterialPriceUnit unit) MapCategory(
        CostMaterialCategory category,
        MaterialPriceUnit unit) => category switch
        {
            CostMaterialCategory.Profile => (CatalogMaterialArticles.ProfileRehau70, MaterialPriceUnit.LinearMeter),
            CostMaterialCategory.Glazing => (CatalogMaterialArticles.Glazing4_16_4, MaterialPriceUnit.SquareMeter),
            CostMaterialCategory.Hardware => (CatalogMaterialArticles.HardwareMacoEgress, MaterialPriceUnit.Piece),
            CostMaterialCategory.Sealant => (CatalogMaterialArticles.SealantEpdm, MaterialPriceUnit.LinearMeter),
            CostMaterialCategory.Components when unit == MaterialPriceUnit.LinearMeter
                => (CatalogMaterialArticles.Reinforcement12, MaterialPriceUnit.LinearMeter),
            CostMaterialCategory.Components => (CatalogMaterialArticles.Connector90, MaterialPriceUnit.Piece),
            _ => (null, unit)
        };
}
