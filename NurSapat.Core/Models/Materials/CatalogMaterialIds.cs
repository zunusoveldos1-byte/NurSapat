namespace NurSapat.Core.Models.Materials;

/// <summary>Стабильные идентификаторы материалов каталога (единый ключ для JSON и будущего PostgreSQL).</summary>
public static class CatalogMaterialIds
{
    public static readonly Guid ProfileRehau70 = Guid.Parse("a1000001-0000-4000-8000-000000000001");
    public static readonly Guid HardwareMacoEgress = Guid.Parse("a1000002-0000-4000-8000-000000000002");
    public static readonly Guid Glazing4_16_4 = Guid.Parse("a1000003-0000-4000-8000-000000000003");
    public static readonly Guid SealantEpdm = Guid.Parse("a1000004-0000-4000-8000-000000000004");
    public static readonly Guid Reinforcement12 = Guid.Parse("a1000005-0000-4000-8000-000000000005");
    public static readonly Guid Connector90 = Guid.Parse("a1000006-0000-4000-8000-000000000006");
    public static readonly Guid Bead24 = Guid.Parse("a1000007-0000-4000-8000-000000000007");
    public static readonly Guid Spoke8White = Guid.Parse("a1000008-0000-4000-8000-000000000008");
    public static readonly Guid Spoke8Gold = Guid.Parse("a1000009-0000-4000-8000-000000000009");
    public static readonly Guid Spoke8Brown = Guid.Parse("a1000010-0000-4000-8000-000000000010");
    public static readonly Guid Spoke18White = Guid.Parse("a1000011-0000-4000-8000-000000000011");
    public static readonly Guid Spoke18Gold = Guid.Parse("a1000012-0000-4000-8000-000000000012");
    public static readonly Guid Spoke18Brown = Guid.Parse("a1000013-0000-4000-8000-000000000013");
    public static readonly Guid Spoke26White = Guid.Parse("a1000014-0000-4000-8000-000000000014");
    public static readonly Guid Spoke26Gold = Guid.Parse("a1000015-0000-4000-8000-000000000015");
    public static readonly Guid Spoke26Brown = Guid.Parse("a1000016-0000-4000-8000-000000000016");
    public static readonly Guid MosquitoNetFiberglass = Guid.Parse("a1000017-0000-4000-8000-000000000017");
    public static readonly Guid SillPvc300 = Guid.Parse("a1000018-0000-4000-8000-000000000018");
    public static readonly Guid SillAluWhite = Guid.Parse("a1000019-0000-4000-8000-000000000019");
    public static readonly Guid ProfilePvc65Window = Guid.Parse("a1000020-0000-4000-8000-000000000020");
    public static readonly Guid ProfilePvc65CoplanarT = Guid.Parse("a1000021-0000-4000-8000-000000000021");

    public static readonly IReadOnlyDictionary<string, Guid> ByArticle =
        new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [CatalogMaterialArticles.ProfileRehau70] = ProfileRehau70,
            [CatalogMaterialArticles.HardwareMacoEgress] = HardwareMacoEgress,
            [CatalogMaterialArticles.Glazing4_16_4] = Glazing4_16_4,
            [CatalogMaterialArticles.SealantEpdm] = SealantEpdm,
            [CatalogMaterialArticles.Reinforcement12] = Reinforcement12,
            [CatalogMaterialArticles.Connector90] = Connector90,
            [CatalogMaterialArticles.Bead24] = Bead24,
            [CatalogMaterialArticles.Spoke8White] = Spoke8White,
            [CatalogMaterialArticles.Spoke8Gold] = Spoke8Gold,
            [CatalogMaterialArticles.Spoke8Brown] = Spoke8Brown,
            [CatalogMaterialArticles.Spoke18White] = Spoke18White,
            [CatalogMaterialArticles.Spoke18Gold] = Spoke18Gold,
            [CatalogMaterialArticles.Spoke18Brown] = Spoke18Brown,
            [CatalogMaterialArticles.Spoke26White] = Spoke26White,
            [CatalogMaterialArticles.Spoke26Gold] = Spoke26Gold,
            [CatalogMaterialArticles.Spoke26Brown] = Spoke26Brown,
            [CatalogMaterialArticles.MosquitoNetFiberglass] = MosquitoNetFiberglass,
            [CatalogMaterialArticles.SillPvc300] = SillPvc300,
            [CatalogMaterialArticles.SillAluWhite] = SillAluWhite,
            [CatalogMaterialArticles.ProfilePvc65Window] = ProfilePvc65Window,
            [CatalogMaterialArticles.ProfilePvc65CoplanarT] = ProfilePvc65CoplanarT
        };

    public static Guid? TryFromArticle(string? article) =>
        string.IsNullOrWhiteSpace(article) || !ByArticle.TryGetValue(article, out var id)
            ? null
            : id;
}

public static class CatalogMaterialArticles
{
    public const string ProfileRehau70 = "RH-70-WHT";
    public const string HardwareMacoEgress = "MACO-EGRESS";
    public const string Glazing4_16_4 = "GL-4-16-4";
    public const string SealantEpdm = "SEAL-EPDM";
    public const string Reinforcement12 = "ARM-1.2";
    public const string Connector90 = "CONN-90";
    public const string Bead24 = "BEAD-24";
    public const string Spoke8White = "SPOKE-8-WHT";
    public const string Spoke8Gold = "SPOKE-8-GLD";
    public const string Spoke8Brown = "SPOKE-8-BRN";
    public const string Spoke18White = "SPOKE-18-WHT";
    public const string Spoke18Gold = "SPOKE-18-GLD";
    public const string Spoke18Brown = "SPOKE-18-BRN";
    public const string Spoke26White = "SPOKE-26-WHT";
    public const string Spoke26Gold = "SPOKE-26-GLD";
    public const string Spoke26Brown = "SPOKE-26-BRN";
    public const string MosquitoNetFiberglass = "NET-FIBER";
    public const string SillPvc300 = "SILL-PVC-300";
    public const string SillAluWhite = "SILL-ALU-WHT";
    public const string ProfilePvc65Window = "PVC-65-TRANS-WIN";
    public const string ProfilePvc65CoplanarT = "PVC-65-TRANS-COP-T";
}
