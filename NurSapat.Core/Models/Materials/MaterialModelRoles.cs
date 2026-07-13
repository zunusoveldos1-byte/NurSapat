namespace NurSapat.Core.Models.Materials;

/// <summary>
/// Роли моделей материалов в домене NurSapat (этап 8).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>MaterialDetail</c> (App) + <see cref="CatalogMaterialIds"/> — канонический ERP-каталог (цены, склад, спецификация).</item>
/// <item><see cref="Construction.ConstructionMaterial"/> + <see cref="Construction.ConstructionMaterialCatalog"/> — CAD-поддомен (геометрия, объём); связь с ERP через <c>CatalogArticle</c>.</item>
/// <item><c>LegacyMaterial</c> (Data.Legacy) — legacy DTO таблицы PostgreSQL <c>materials</c>; не использовать в новой бизнес-логике.</item>
/// </list>
/// Класса <c>CatalogMaterial</c> нет: канонический ключ — <see cref="CatalogMaterialIds"/> + сущность <c>MaterialDetail</c>.
/// </remarks>
public static class MaterialModelRoles
{
    public const string ErpCatalogEntity = "MaterialDetail";
    public const string CadGeometryEntity = "ConstructionMaterial";
    public const string LegacyPgDto = "LegacyMaterial";
}
