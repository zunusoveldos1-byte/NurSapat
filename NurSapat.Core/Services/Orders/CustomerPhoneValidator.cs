using System.Text.RegularExpressions;

namespace NurSapat.Core.Services.Orders;

public static partial class CustomerPhoneValidator
{
    private static readonly Regex KyrgyzPhonePattern = KyrgyzPhoneRegex();

    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var normalized = Normalize(phone);
        return KyrgyzPhonePattern.IsMatch(normalized);
    }

    public static string Normalize(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("996") && digits.Length == 12)
        {
            return $"+996 ({digits[3..6]}) {digits[6..8]}-{digits[8..10]}-{digits[10..12]}";
        }

        return phone.Trim();
    }

    [GeneratedRegex(@"^\+996 \(\d{3}\) \d{2}-\d{2}-\d{2}$")]
    private static partial Regex KyrgyzPhoneRegex();
}
