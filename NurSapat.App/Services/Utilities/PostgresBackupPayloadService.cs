using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using Npgsql;
using NurSapat.Data;

namespace NurSapat.App.Services.Utilities;

public interface IDatabaseBackupPayloadService
{
    string DatabaseTarget { get; }

    Task<int> InspectDatabasePayloadAsync(string archivePath, CancellationToken cancellationToken = default);

    Task AddDatabasePayloadAsync(string archivePath, CancellationToken cancellationToken = default);

    Task RestoreDatabasePayloadAsync(string archivePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Adds/restores a PostgreSQL custom-format dump inside a .zp project archive.
/// PostgreSQL has no local database file that can be copied safely, therefore
/// pg_dump/pg_restore are used as the supported snapshot mechanism.
/// </summary>
public sealed class PostgresBackupPayloadService(DatabaseService databaseService)
    : IDatabaseBackupPayloadService
{
    private const string DatabaseEntryName = "database.dump";

    public string DatabaseTarget
    {
        get
        {
            var connection = new NpgsqlConnectionStringBuilder(databaseService.ConnectionString);
            return $"{connection.Host}:{connection.Port} / {connection.Database}";
        }
    }

    public async Task AddDatabasePayloadAsync(
        string archivePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException("Архив .zp не найден.", archivePath);
        }

        var tempDump = Path.Combine(Path.GetTempPath(), $"nursapat-{Guid.NewGuid():N}.dump");
        try
        {
            await RunPostgresToolAsync("pg_dump",
                ["--format=custom", "--file", tempDump], cancellationToken);

            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
            archive.GetEntry(DatabaseEntryName)?.Delete();
            archive.CreateEntryFromFile(
                tempDump,
                DatabaseEntryName,
                CompressionLevel.Optimal);
        }
        finally
        {
            TryDelete(tempDump);
        }
    }

    public async Task<int> InspectDatabasePayloadAsync(
        string archivePath,
        CancellationToken cancellationToken = default)
    {
        var tempDump = Path.Combine(Path.GetTempPath(), $"nursapat-{Guid.NewGuid():N}.dump");
        try
        {
            await ExtractDumpAsync(archivePath, tempDump, cancellationToken);
            var contents = await RunPostgresToolAsync("pg_restore", ["--list", tempDump], cancellationToken,
                connect: false);
            var tableCount = contents.Split('\n').Count(line => line.Contains(" TABLE DATA ", StringComparison.Ordinal));
            return tableCount > 0
                ? tableCount
                : throw new InvalidDataException("Снимок PostgreSQL не содержит данных таблиц.");
        }
        finally
        {
            TryDelete(tempDump);
        }
    }

    public async Task RestoreDatabasePayloadAsync(
        string archivePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException("Архив .zp не найден.", archivePath);
        }

        var tempDump = Path.Combine(Path.GetTempPath(), $"nursapat-{Guid.NewGuid():N}.dump");
        try
        {
            await ExtractDumpAsync(archivePath, tempDump, cancellationToken);

            NpgsqlConnection.ClearAllPools();
            try
            {
                await RunPostgresToolAsync("pg_restore",
                    ["--single-transaction", "--clean", "--if-exists", "--no-owner", "--no-privileges",
                        "--exit-on-error", tempDump], cancellationToken);
            }
            finally
            {
                NpgsqlConnection.ClearAllPools();
            }
        }
        finally
        {
            TryDelete(tempDump);
        }
    }

    private static async Task ExtractDumpAsync(
        string archivePath, string targetPath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.GetEntry(DatabaseEntryName)
            ?? throw new InvalidDataException("Архив проекта не содержит резервную копию PostgreSQL.");
        await using var source = entry.Open();
        await using var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(target, cancellationToken);
    }

    private async Task<string> RunPostgresToolAsync(
        string executable,
        string[] arguments,
        CancellationToken cancellationToken,
        bool connect = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolvePostgresExecutable(executable),
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        if (connect)
        {
            var connection = new NpgsqlConnectionStringBuilder(databaseService.ConnectionString);
            startInfo.Environment["PGPASSWORD"] = connection.Password ?? string.Empty;
            startInfo.Environment["PGCONNECT_TIMEOUT"] = Math.Max(connection.Timeout, 2).ToString();
            startInfo.ArgumentList.Add("--no-password");
            startInfo.ArgumentList.Add($"--host={connection.Host ?? throw new InvalidOperationException("Укажите сервер PostgreSQL.")}");
            startInfo.ArgumentList.Add($"--port={connection.Port}");
            startInfo.ArgumentList.Add($"--username={connection.Username ?? throw new InvalidOperationException("Укажите пользователя PostgreSQL.")}");
            startInfo.ArgumentList.Add($"--dbname={connection.Database ?? throw new InvalidOperationException("Укажите имя базы PostgreSQL.")}");
        }
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Не удалось запустить {executable}.");
        }
        catch (Win32Exception ex)
        {
            throw new FileNotFoundException(
                $"Утилита PostgreSQL {executable} не найдена. Добавьте папку PostgreSQL bin в PATH.",
                executable,
                ex);
        }

        using (process)
        {
            var errorTask = process.StandardError.ReadToEndAsync();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
                await process.WaitForExitAsync(CancellationToken.None);
                throw;
            }
            var error = await errorTask;
            var output = await outputTask;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"{executable} завершился с кодом {process.ExitCode}: {error}");
            }
            return output;
        }
    }

    private static string ResolvePostgresExecutable(string executable)
    {
        var fileName = OperatingSystem.IsWindows() ? $"{executable}.exe" : executable;
        var pathDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var directory in pathDirectories)
        {
            var candidate = Path.Combine(directory.Trim(), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        if (OperatingSystem.IsWindows())
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PostgreSQL");
            if (Directory.Exists(root))
            {
                var candidate = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories)
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (candidate is not null)
                {
                    return candidate;
                }
            }
        }

        throw new FileNotFoundException(
            $"Утилита PostgreSQL {fileName} не найдена. Добавьте папку PostgreSQL bin в PATH.",
            fileName);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Cleanup must not hide backup/restore errors.
        }
    }
}
