using System.Text;
using System.Text.Json;
using Dapper;
using NurSapat.App.Models.Settings;
using NurSapat.App.Models.Utilities;
using NurSapat.App.Services.Settings;
using NurSapat.Data;

namespace NurSapat.App.Services.Utilities;

public interface IProductionBaseService
{
    Task ExportUpdatePackageAsync(string filePath, CancellationToken cancellationToken = default);

    Task ImportUpdatePackageAsync(string filePath, string? password, CancellationToken cancellationToken = default);
}

public sealed class ProductionBaseService : IProductionBaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string PackageHeader = "NURPRODUPD1";

    private readonly DatabaseService _databaseService;
    private readonly IEnvironmentSettingsService _environmentSettingsService;

    public ProductionBaseService(
        DatabaseService databaseService,
        IEnvironmentSettingsService environmentSettingsService)
    {
        _databaseService = databaseService;
        _environmentSettingsService = environmentSettingsService;
    }

    public async Task ExportUpdatePackageAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var package = await BuildPackageAsync(cancellationToken);
        var json = JsonSerializer.Serialize(package, JsonOptions);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        var header = Encoding.UTF8.GetBytes(PackageHeader);
        await using var stream = File.Create(filePath);
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(plainBytes, cancellationToken);
    }

    public async Task ImportUpdatePackageAsync(string filePath, string? password, CancellationToken cancellationToken = default)
    {
        var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        if (fileBytes.Length < PackageHeader.Length)
        {
            throw new InvalidOperationException("Файл обновления повреждён или имеет неверный формат.");
        }

        var header = Encoding.UTF8.GetString(fileBytes[..PackageHeader.Length]);
        if (!string.Equals(header, PackageHeader, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Ожидается файл обновления с расширением .prod-update.");
        }

        var payload = fileBytes[PackageHeader.Length..];
        var plainBytes = PackageEncryptionHelper.Decrypt(payload, password);
        var json = Encoding.UTF8.GetString(plainBytes);
        var package = JsonSerializer.Deserialize<ProductionUpdatePackage>(json, JsonOptions)
            ?? throw new InvalidOperationException("Не удалось прочитать пакет обновления.");

        await ApplyPackageAsync(package, cancellationToken);
    }

    private async Task<ProductionUpdatePackage> BuildPackageAsync(CancellationToken cancellationToken)
    {
        await using var connection = _databaseService.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string materialSql = @"
            SELECT article AS Article,
                   name AS Name,
                   category::text AS Category,
                   price AS Price,
                   currency AS Currency,
                   unit_of_measure AS UnitOfMeasure,
                   waste_percent AS WastePercent
            FROM materials
            ORDER BY article";

        const string constraintsSql = @"
            SELECT hardware_brand AS HardwareBrand,
                   min_width_mm AS MinWidthMm,
                   max_width_mm AS MaxWidthMm,
                   min_height_mm AS MinHeightMm,
                   max_height_mm AS MaxHeightMm,
                   max_weight_kg AS MaxWeightKg
            FROM hardware_constraints
            ORDER BY hardware_brand";

        var settings = _environmentSettingsService.Current;

        return new ProductionUpdatePackage
        {
            Materials = (await connection.QueryAsync<DealerMaterialEntry>(materialSql)).ToList(),
            HardwareConstraints = (await connection.QueryAsync<ProductionHardwareConstraintEntry>(constraintsSql)).ToList(),
            TechnologySettings = new ProductionTechnologySettings
            {
                ProfileCutLevel = settings.ProfileCutLevel.ToString(),
                ProfileWasteMode = settings.ProfileWasteMode.ToString(),
                GlassWasteMode = settings.GlassWasteMode.ToString(),
                MinGlassCutLengthMm = settings.MinGlassCutLengthMm,
                MinGlassCutAreaM2 = settings.MinGlassCutAreaM2,
                AvoidDeadZoneCuts = settings.AvoidDeadZoneCuts,
                ParallelCutTwoBars = settings.ParallelCutTwoBars
            }
        };
    }

    private async Task ApplyPackageAsync(ProductionUpdatePackage package, CancellationToken cancellationToken)
    {
        await using var connection = _databaseService.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string upsertMaterialSql = @"
            INSERT INTO materials (article, name, category, price, currency, unit_of_measure, waste_percent)
            VALUES (@Article, @Name, @Category::material_category, @Price, @Currency, @UnitOfMeasure, @WastePercent)
            ON CONFLICT (article) DO UPDATE
            SET name = EXCLUDED.name,
                category = EXCLUDED.category,
                price = EXCLUDED.price,
                currency = EXCLUDED.currency,
                unit_of_measure = EXCLUDED.unit_of_measure,
                waste_percent = EXCLUDED.waste_percent";

        foreach (var material in package.Materials)
        {
            await connection.ExecuteAsync(upsertMaterialSql, material, transaction);
        }

        await connection.ExecuteAsync("DELETE FROM hardware_constraints", transaction: transaction);

        const string insertConstraintSql = @"
            INSERT INTO hardware_constraints
                (hardware_brand, min_width_mm, max_width_mm, min_height_mm, max_height_mm, max_weight_kg)
            VALUES
                (@HardwareBrand, @MinWidthMm, @MaxWidthMm, @MinHeightMm, @MaxHeightMm, @MaxWeightKg)";

        foreach (var constraint in package.HardwareConstraints)
        {
            await connection.ExecuteAsync(insertConstraintSql, constraint, transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        ApplyTechnologySettings(package.TechnologySettings);
    }

    private void ApplyTechnologySettings(ProductionTechnologySettings technologySettings)
    {
        var settings = _environmentSettingsService.Current.Clone();

        if (Enum.TryParse<ProfileCutLevel>(technologySettings.ProfileCutLevel, out var profileCutLevel))
        {
            settings.ProfileCutLevel = profileCutLevel;
        }

        if (Enum.TryParse<ProfileWasteMode>(technologySettings.ProfileWasteMode, out var profileWasteMode))
        {
            settings.ProfileWasteMode = profileWasteMode;
        }

        if (Enum.TryParse<GlassWasteMode>(technologySettings.GlassWasteMode, out var glassWasteMode))
        {
            settings.GlassWasteMode = glassWasteMode;
        }

        settings.MinGlassCutLengthMm = technologySettings.MinGlassCutLengthMm;
        settings.MinGlassCutAreaM2 = technologySettings.MinGlassCutAreaM2;
        settings.AvoidDeadZoneCuts = technologySettings.AvoidDeadZoneCuts;
        settings.ParallelCutTwoBars = technologySettings.ParallelCutTwoBars;

        _environmentSettingsService.Save(settings);
    }
}
