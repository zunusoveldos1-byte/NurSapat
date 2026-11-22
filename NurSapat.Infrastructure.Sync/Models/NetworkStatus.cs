namespace NurSapat.Infrastructure.Sync.Models;

public enum NetworkQuality
{
    Checking,
    Excellent,
    Poor,
    Offline
}

public sealed record NetworkStatusSnapshot(
    NetworkQuality Quality,
    string DisplayText,
    TimeSpan? Latency,
    DateTimeOffset CheckedAtUtc)
{
    public bool IsOnline => Quality is NetworkQuality.Excellent or NetworkQuality.Poor;

    public static NetworkStatusSnapshot Checking { get; } =
        new(NetworkQuality.Checking, "Проверка сети…", null, DateTimeOffset.UtcNow);
}

public sealed record RemoteColumnSchema(
    string Name,
    string SqliteType,
    bool IsNullable,
    bool IsPrimaryKey);

public sealed record RemoteTableSchema(
    string Name,
    IReadOnlyCollection<RemoteColumnSchema> Columns);

public sealed record PendingMirrorRow(
    string TableName,
    string LocalKey,
    string Operation,
    IReadOnlyDictionary<string, object?> Values);
