using System.Globalization;
using Microsoft.Data.Sqlite;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Local;

public sealed class GitHubReleaseCache : IGitHubReleaseCache
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public GitHubReleaseCache(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await SqliteSchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        IEnumerable<CachedGitHubRelease> releases,
        CancellationToken cancellationToken = default)
    {
        var list = releases.ToList();
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            await using (var clear = connection.CreateCommand())
            {
                clear.Transaction = tx;
                clear.CommandText = "DELETE FROM github_releases;";
                await clear.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var release in list)
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText = """
                    INSERT INTO github_releases (id, tag_name, name, body, published_at, fetched_at)
                    VALUES ($id, $tag_name, $name, $body, $published_at, $fetched_at);
                    """;
                insert.Parameters.AddWithValue("$id", release.Id);
                insert.Parameters.AddWithValue("$tag_name", release.TagName);
                insert.Parameters.AddWithValue("$name", release.Name);
                insert.Parameters.AddWithValue("$body", release.Body);
                insert.Parameters.AddWithValue(
                    "$published_at",
                    release.PublishedAt.ToString("O", CultureInfo.InvariantCulture));
                insert.Parameters.AddWithValue(
                    "$fetched_at",
                    release.FetchedAt.ToString("O", CultureInfo.InvariantCulture));
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<CachedGitHubRelease>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, tag_name, name, body, published_at, fetched_at
            FROM github_releases
            ORDER BY published_at DESC;
            """;

        var result = new List<CachedGitHubRelease>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(new CachedGitHubRelease
            {
                Id = reader.GetString(0),
                TagName = reader.GetString(1),
                Name = reader.GetString(2),
                Body = reader.GetString(3),
                PublishedAt = DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                FetchedAt = DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            });
        }

        return result;
    }
}
