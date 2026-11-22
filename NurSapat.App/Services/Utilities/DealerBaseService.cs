using System.Text;
using System.Text.Json;
using Dapper;
using NurSapat.App.Models.Utilities;
using NurSapat.App.Services.Dictionaries;
using NurSapat.Data;

namespace NurSapat.App.Services.Utilities;

public interface IDealerBaseService
{
    Task ExportAsync(string filePath, string? password, CancellationToken cancellationToken = default);
}

public sealed class DealerBaseService : IDealerBaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProfileSeriesStore _profileSeriesStore;
    private readonly IManufacturersStore _manufacturersStore;
    private readonly DatabaseService _databaseService;

    public DealerBaseService(
        IProfileSeriesStore profileSeriesStore,
        IManufacturersStore manufacturersStore,
        DatabaseService databaseService)
    {
        _profileSeriesStore = profileSeriesStore;
        _manufacturersStore = manufacturersStore;
        _databaseService = databaseService;
    }

    public async Task ExportAsync(string filePath, string? password, CancellationToken cancellationToken = default)
    {
        var package = await BuildPackageAsync(cancellationToken);
        var json = JsonSerializer.Serialize(package, JsonOptions);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var payload = PackageEncryptionHelper.Encrypt(plainBytes, password);

        var header = Encoding.UTF8.GetBytes("NURDEALER1");
        await using var stream = File.Create(filePath);
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }

    private async Task<DealerBasePackage> BuildPackageAsync(CancellationToken cancellationToken)
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

        var materialEntries = (await connection.QueryAsync<DealerMaterialEntry>(materialSql)).ToList();

        return new DealerBasePackage
        {
            Materials = materialEntries,
            ProfileSeries = _profileSeriesStore.GetAll()
                .Select(item => new DealerProfileSeriesEntry
                {
                    Id = item.Id,
                    Name = item.Name,
                    ManufacturerId = item.ManufacturerId,
                    HardwareAxisMm = item.HardwareAxisMm
                })
                .ToList(),
            Manufacturers = _manufacturersStore.GetAll()
                .Select(item => new DealerManufacturerEntry
                {
                    Id = item.Id,
                    Name = item.Name,
                    FalsCorrectionMm = item.FalsCorrectionMm,
                    EndCuttingMm = item.EndCuttingMm,
                    BladeWidthMm = item.BladeWidthMm,
                    WeldingShrinkageMm = item.WeldingShrinkageMm
                })
                .ToList()
        };
    }
}
