using System.Text;

namespace NurSapat.App.Services.Export;

internal sealed class DbfColumnDefinition
{
    public required string Name { get; init; }

    public char Type { get; init; } = 'C';

    public byte Length { get; init; } = 50;

    public byte DecimalCount { get; init; }
}

internal static class DbfFileWriter
{
    public static void Write(
        string filePath,
        IReadOnlyList<DbfColumnDefinition> columns,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var recordLength = (ushort)(columns.Sum(column => column.Length) + 1);
        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write((byte)0x03);
        writer.Write(ToDosDate(DateTime.Now));
        writer.Write((uint)rows.Count);
        writer.Write((ushort)(32 + columns.Count * 32 + 1));
        writer.Write(recordLength);
        writer.Write(new byte[20]);

        foreach (var column in columns)
        {
            var nameBytes = new byte[11];
            var asciiName = Encoding.ASCII.GetBytes(column.Name.ToUpperInvariant());
            Array.Copy(asciiName, nameBytes, Math.Min(asciiName.Length, 11));
            writer.Write(nameBytes);
            writer.Write(column.Type);
            writer.Write(column.DecimalCount);
            writer.Write(column.Length);
            writer.Write(new byte[14]);
        }

        writer.Write((byte)0x0D);

        foreach (var row in rows)
        {
            writer.Write((byte)0x20);
            foreach (var column in columns)
            {
                row.TryGetValue(column.Name, out var value);
                writer.Write(FormatField(value, column));
            }
        }

        writer.Write((byte)0x1A);
    }

    private static byte[] FormatField(object? value, DbfColumnDefinition column)
    {
        var text = column.Type switch
        {
            'N' => FormatNumber(value, column.Length, column.DecimalCount),
            'D' => FormatDate(value),
            _ => FormatText(value, column.Length)
        };

        var bytes = new byte[column.Length];
        var encoded = Encoding.GetEncoding(866).GetBytes(text);
        Array.Copy(encoded, bytes, Math.Min(encoded.Length, bytes.Length));
        return bytes;
    }

    private static string FormatText(object? value, int length)
    {
        var text = value?.ToString() ?? string.Empty;
        return text.Length > length ? text[..length] : text.PadRight(length);
    }

    private static string FormatNumber(object? value, int length, int decimals)
    {
        if (value is null)
        {
            return new string(' ', length);
        }

        if (!decimal.TryParse(value.ToString(), out var number))
        {
            return new string(' ', length);
        }

        var format = decimals > 0 ? $"{{0:{new string('0', length - decimals - 1)}.{new string('0', decimals)}}}" : $"{{0:{new string('0', length)}}}";
        var text = string.Format(format, number);
        return text.Length > length ? text[^length..] : text.PadLeft(length);
    }

    private static string FormatDate(object? value)
    {
        if (value is DateTime date)
        {
            return date.ToString("yyyyMMdd");
        }

        return "        ";
    }

    private static byte[] ToDosDate(DateTime date)
    {
        var year = Math.Clamp(date.Year - 1900, 0, 255);
        return [(byte)year, (byte)date.Month, (byte)date.Day];
    }
}
