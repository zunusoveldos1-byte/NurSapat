using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Interfaces;

public interface IMaterialCatalogResolver
{
    Guid? ResolveCatalogMaterialId(Guid? constructionMaterialId);

    Guid? ResolveByArticle(string? article);

    Guid? ResolveByCategory(CostMaterialCategory category, MaterialPriceUnit unit);
}
