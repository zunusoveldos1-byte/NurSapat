using System.Globalization;
using System.Net.Http;
using System.Xml.Linq;
using NurSapat.Core.Interfaces.Currency;

namespace NurSapat.App.Services.Currency;

public sealed class CurrencyRateImporter : ICurrencyRateImporter
{
    private const string DailyRatesUrl = "https://www.nbkr.kg/XML/daily.xml";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static CurrencyRateImporter()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NurSapat-CurrencySync/9.0");
    }

    public async Task<CurrencyRateImportResult> ImportDailyRatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = await HttpClient.GetStreamAsync(DailyRatesUrl, cancellationToken);
            var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

            var root = document.Root
                ?? throw new InvalidOperationException("XML НБКР не содержит корневого элемента.");

            var rateDate = ParseRateDate(root);
            var rates = root.Elements("Currency")
                .Select(ParseCurrency)
                .Where(rate => rate is not null)
                .Cast<ImportedCurrencyRate>()
                .ToList();

            if (rates.Count == 0)
            {
                throw new InvalidOperationException("XML НБКР не содержит курсов валют.");
            }

            return new CurrencyRateImportResult(rateDate, rates);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new HttpRequestException("Не удалось загрузить или разобрать XML курсов НБКР.", ex);
        }
    }

    private static DateTime ParseRateDate(XElement root)
    {
        var dateAttribute = root.Attribute("Date")?.Value;
        if (string.IsNullOrWhiteSpace(dateAttribute))
        {
            throw new InvalidOperationException("XML НБКР не содержит атрибута Date.");
        }

        if (DateTime.TryParseExact(
                dateAttribute.Trim(),
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return parsedDate.Date;
        }

        throw new InvalidOperationException($"Некорректный формат даты в XML НБКР: '{dateAttribute}'.");
    }

    private static ImportedCurrencyRate? ParseCurrency(XElement currencyElement)
    {
        var code = currencyElement.Element("ISOCode")?.Value?.Trim()
            ?? currencyElement.Attribute("ISOCode")?.Value?.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var valueText = currencyElement.Element("Value")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(valueText))
        {
            return null;
        }

        var nominalText = currencyElement.Element("Nominal")?.Value?.Trim() ?? "1";
        if (!decimal.TryParse(nominalText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var nominal)
            || nominal <= 0)
        {
            nominal = 1m;
        }

        var value = decimal.Parse(
            valueText.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture);

        var ratePerUnit = value / nominal;
        return new ImportedCurrencyRate(code.ToUpperInvariant(), ratePerUnit);
    }
}
