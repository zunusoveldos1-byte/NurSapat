using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IExchangeRatesStore
{
    IReadOnlyList<ExchangeRatePackage> GetAll();

    ExchangeRatePackage? GetById(Guid id);

    ExchangeRatePackage? GetByStartDate(DateTime startDate);

    ExchangeRatePackage? GetRatesForDate(DateTime date);

    void Add(ExchangeRatePackage package);

    void Update(ExchangeRatePackage package);

    void Delete(Guid id);
}

public sealed class ExchangeRatesStore : IExchangeRatesStore
{
    private readonly List<ExchangeRatePackage> _packages = CreateDemoPackages();

    public IReadOnlyList<ExchangeRatePackage> GetAll() =>
        _packages
            .OrderByDescending(package => package.StartDate)
            .ToList();

    public ExchangeRatePackage? GetById(Guid id) =>
        _packages.FirstOrDefault(package => package.Id == id);

    public ExchangeRatePackage? GetByStartDate(DateTime startDate)
    {
        var date = startDate.Date;
        return _packages.FirstOrDefault(package => package.StartDate.Date == date);
    }

    public ExchangeRatePackage? GetRatesForDate(DateTime date)
    {
        var targetDate = date.Date;
        return _packages
            .Where(package => package.StartDate.Date <= targetDate)
            .OrderByDescending(package => package.StartDate)
            .FirstOrDefault();
    }

    public void Add(ExchangeRatePackage package) => _packages.Add(package);

    public void Update(ExchangeRatePackage package)
    {
        var index = _packages.FindIndex(item => item.Id == package.Id);
        if (index >= 0)
        {
            _packages[index] = package;
        }
    }

    public void Delete(Guid id) => _packages.RemoveAll(package => package.Id == id);

    private static List<ExchangeRatePackage> CreateDemoPackages() =>
    [
        new()
        {
            StartDate = new DateTime(2026, 1, 1),
            UsdRate = 86.20m,
            EurRate = 98.10m,
            CnyRate = 12.55m,
            UeRate = 86.20m
        },
        new()
        {
            StartDate = new DateTime(2026, 7, 1),
            UsdRate = 87.45m,
            EurRate = 99.62m,
            CnyRate = 12.88m,
            UeRate = 87.45m
        }
    ];
}
