namespace NurSapat.Core.Validation;

/// <summary>Общие инварианты доменных моделей (этап 8).</summary>
public static class DomainInvariants
{
    public static void RequireNonEmptyGuid(Guid value, string propertyName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{propertyName} is required.", propertyName);
        }
    }

    public static void RequireNonEmpty(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} is required.", propertyName);
        }
    }

    public static void RequirePositive(decimal value, string propertyName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(propertyName, value, $"{propertyName} must be greater than zero.");
        }
    }

    public static void RequireNonNegative(decimal value, string propertyName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(propertyName, value, $"{propertyName} must be non-negative.");
        }
    }

    public static bool TryValidateNonEmptyGuid(Guid value) => value != Guid.Empty;

    public static bool TryValidatePositive(decimal value) => value > 0;

    public static bool TryValidateNonNegative(decimal value) => value >= 0;
}
