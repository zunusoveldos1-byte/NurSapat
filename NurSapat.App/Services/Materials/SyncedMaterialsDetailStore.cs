using System.Text.Json;
using NurSapat.App.Models.Materials;
using NurSapat.App.Services.Users;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.App.Services.Materials;

/// <summary>
/// Offline-first materials store: SQLite + isolation by current user_id.
/// </summary>
public sealed class SyncedMaterialsDetailStore : IMaterialsDetailStore
{
    private readonly IOfflineMaterialRepository _repository;
    private readonly IUserSessionService _session;
    private readonly ISupabaseAuthService _auth;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SyncedMaterialsDetailStore(
        IOfflineMaterialRepository repository,
        IUserSessionService session,
        ISupabaseAuthService auth)
    {
        _repository = repository;
        _session = session;
        _auth = auth;
    }

    private Guid? CurrentUserId =>
        _session.CurrentUser?.Id
        ?? _auth.CurrentUserId;

    public IReadOnlyList<MaterialDetail> GetAll()
    {
        var userId = CurrentUserId;
        var rows = Run(() => _repository.GetAllAsync());
        return rows
            .Where(row => userId is null || row.UserId == userId)
            .Select(ToDetail)
            .ToList();
    }

    public MaterialDetail? GetById(Guid id)
    {
        var row = Run(() => _repository.GetByIdAsync(id));
        if (row is null)
        {
            return null;
        }

        var userId = CurrentUserId;
        if (userId is not null && row.UserId != userId)
        {
            return null;
        }

        return ToDetail(row);
    }

    public MaterialDetail? GetByArticle(string article) =>
        GetAll().FirstOrDefault(item =>
            string.Equals(item.Article, article, StringComparison.OrdinalIgnoreCase));

    public void Add(MaterialDetail material)
    {
        var userId = CurrentUserId
            ?? throw new InvalidOperationException("Нет текущего пользователя для сохранения материала.");

        if (material.Id == Guid.Empty)
        {
            material.Id = Guid.NewGuid();
        }

        var local = ToLocal(material, userId);
        Run(() => _repository.InsertAsync(local));
    }

    public void Update(MaterialDetail material)
    {
        var userId = CurrentUserId
            ?? throw new InvalidOperationException("Нет текущего пользователя для обновления материала.");

        var local = ToLocal(material, userId);
        Run(() => _repository.UpdateAsync(local));
    }

    public void Delete(Guid id) =>
        Run(() => _repository.SoftDeleteAsync(id));

    private static T Run<T>(Func<Task<T>> action) =>
        action().ConfigureAwait(false).GetAwaiter().GetResult();

    private static void Run(Func<Task> action) =>
        action().ConfigureAwait(false).GetAwaiter().GetResult();

    private static LocalMaterial ToLocal(MaterialDetail m, Guid userId) => new()
    {
        Id = m.Id,
        Article = m.Article,
        Name = m.Name,
        Note = m.Note,
        Manufacturer = m.Manufacturer,
        Section = (int)m.Section,
        MainUnit = (int)m.MainUnit,
        LinearGroup = m.LinearGroup.HasValue ? (int)m.LinearGroup.Value : null,
        SquareGroup = m.SquareGroup.HasValue ? (int)m.SquareGroup.Value : null,
        Currency = (int)m.Currency,
        BasePrice = m.BasePrice,
        WastePercent = m.WastePercent,
        PrintOnAssemblyDrawing = m.PrintOnAssemblyDrawing,
        PrintOnCommercialOffer = m.PrintOnCommercialOffer,
        PrintOnShippingSheet = m.PrintOnShippingSheet,
        GroupPrintPositions = m.GroupPrintPositions,
        IsWarmProfile = m.IsWarmProfile,
        InnerCoatingAreaPerMeter = m.InnerCoatingAreaPerMeter,
        OuterCoatingAreaPerMeter = m.OuterCoatingAreaPerMeter,
        EnableLengthCutOptimization = m.EnableLengthCutOptimization,
        BarLengthMm = m.BarLengthMm,
        IlliquidThresholdMm = m.IlliquidThresholdMm,
        ScrapMm = m.ScrapMm,
        EnablePlaneCutOptimization = m.EnablePlaneCutOptimization,
        SheetWidthMm = m.SheetWidthMm,
        SheetHeightMm = m.SheetHeightMm,
        GlassTint = m.GlassTint,
        MinimumCalculatedAreaSqM = m.MinimumCalculatedAreaSqM,
        UseSecondaryUnit = m.UseSecondaryUnit,
        SecondaryUnitCoefficient = m.SecondaryUnitCoefficient,
        PriceByKilogram = m.PriceByKilogram,
        PricesJson = JsonSerializer.Serialize(new PricesPayload
        {
            ColorPrices = m.ColorPrices,
            DimensionPrices = m.DimensionPrices
        }, JsonOptions),
        UserId = userId
    };

    private static MaterialDetail ToDetail(LocalMaterial m)
    {
        var detail = new MaterialDetail
        {
            Id = m.Id,
            Article = m.Article,
            Name = m.Name,
            Note = m.Note,
            Manufacturer = m.Manufacturer,
            Section = (MaterialSection)m.Section,
            MainUnit = (MaterialMainUnit)m.MainUnit,
            LinearGroup = m.LinearGroup.HasValue ? (MaterialLinearGroup)m.LinearGroup.Value : null,
            SquareGroup = m.SquareGroup.HasValue ? (MaterialSquareGroup)m.SquareGroup.Value : null,
            Currency = (MaterialCurrency)m.Currency,
            BasePrice = m.BasePrice,
            WastePercent = m.WastePercent,
            PrintOnAssemblyDrawing = m.PrintOnAssemblyDrawing,
            PrintOnCommercialOffer = m.PrintOnCommercialOffer,
            PrintOnShippingSheet = m.PrintOnShippingSheet,
            GroupPrintPositions = m.GroupPrintPositions,
            IsWarmProfile = m.IsWarmProfile,
            InnerCoatingAreaPerMeter = m.InnerCoatingAreaPerMeter,
            OuterCoatingAreaPerMeter = m.OuterCoatingAreaPerMeter,
            EnableLengthCutOptimization = m.EnableLengthCutOptimization,
            BarLengthMm = m.BarLengthMm,
            IlliquidThresholdMm = m.IlliquidThresholdMm,
            ScrapMm = m.ScrapMm,
            EnablePlaneCutOptimization = m.EnablePlaneCutOptimization,
            SheetWidthMm = m.SheetWidthMm,
            SheetHeightMm = m.SheetHeightMm,
            GlassTint = m.GlassTint,
            MinimumCalculatedAreaSqM = m.MinimumCalculatedAreaSqM,
            UseSecondaryUnit = m.UseSecondaryUnit,
            SecondaryUnitCoefficient = m.SecondaryUnitCoefficient,
            PriceByKilogram = m.PriceByKilogram
        };

        try
        {
            var payload = JsonSerializer.Deserialize<PricesPayload>(m.PricesJson, JsonOptions);
            if (payload?.ColorPrices is { Count: > 0 })
            {
                detail.ColorPrices = payload.ColorPrices;
            }

            if (payload?.DimensionPrices is { Count: > 0 })
            {
                detail.DimensionPrices = payload.DimensionPrices;
            }
        }
        catch
        {
            // keep empty price lists
        }

        return detail;
    }

    private sealed class PricesPayload
    {
        public List<MaterialColorPrice> ColorPrices { get; set; } = [];
        public List<MaterialDimensionPrice> DimensionPrices { get; set; } = [];
    }
}
