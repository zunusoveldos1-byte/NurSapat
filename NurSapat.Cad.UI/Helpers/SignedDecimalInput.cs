using System.Globalization;
using System.Text.RegularExpressions;

namespace NurSapat.Cad.UI.Helpers;

public static partial class SignedDecimalInput
{
    [GeneratedRegex(@"^-?[0-9]*[\,\.]?[0-9]?$", RegexOptions.Compiled)]
    private static partial Regex SignedCoordinatePattern();

    [GeneratedRegex(@"^[0-9]*[\,\.]?[0-9]?$", RegexOptions.Compiled)]
    private static partial Regex PositiveDimensionPattern();

    public static bool IsValidSignedCoordinateText(string? text) =>
        string.IsNullOrEmpty(text) || SignedCoordinatePattern().IsMatch(text);

    public static bool IsValidPositiveDimensionText(string? text) =>
        string.IsNullOrEmpty(text) || PositiveDimensionPattern().IsMatch(text);

    public static bool TryParseCoordinate(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        value = Math.Round(value, 1, MidpointRounding.AwayFromZero);
        return true;
    }

    public static bool TryParsePositiveDimension(string? text, out double value)
    {
        if (!TryParseCoordinate(text, out value) || value <= 0)
        {
            value = 0;
            return false;
        }

        return true;
    }

    public static string FormatCoordinate(double value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.InvariantCulture);
}
