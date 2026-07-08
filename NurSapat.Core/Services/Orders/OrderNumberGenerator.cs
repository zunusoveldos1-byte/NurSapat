namespace NurSapat.Core.Services.Orders;

public static class OrderNumberGenerator
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, int> MonthlySequence = new(StringComparer.Ordinal);

    public static string GenerateNext(DateTime? referenceDate = null)
    {
        var date = referenceDate?.Date ?? DateTime.Today;
        var key = $"{date:yyyy-MM}";

        lock (Sync)
        {
            MonthlySequence.TryGetValue(key, out var sequence);
            sequence++;
            MonthlySequence[key] = sequence;
            return $"{date:yyyy-MM}-{sequence:0000}";
        }
    }

    public static string GenerateNext(IEnumerable<string> existingNumbers, DateTime? referenceDate = null)
    {
        var date = referenceDate?.Date ?? DateTime.Today;
        var prefix = $"{date:yyyy-MM}-";
        var max = 0;

        foreach (var number in existingNumbers)
        {
            if (!number.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (int.TryParse(number[prefix.Length..], out var value))
            {
                max = Math.Max(max, value);
            }
        }

        lock (Sync)
        {
            MonthlySequence[$"{date:yyyy-MM}"] = Math.Max(max, MonthlySequence.GetValueOrDefault($"{date:yyyy-MM}"));
        }

        return GenerateNext(date);
    }
}
