using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using Npgsql;
using NurSapat.Data;

namespace NurSapat.App.Services.Utilities;

public interface IDatabaseBackupPayloadService
{
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
            await RunPostgresToolAsync(
                "pg_dump",
                tempDump,
                restore: false,
                cancellationToken);

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
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                var entry = archive.GetEntry(DatabaseEntryName)
                    ?? throw new InvalidDataException(
                        $"Архив не содержит снимок PostgreSQL ({DatabaseEntryName}).");
                await using var source = entry.Open();
                await using var target = new FileStream(
                    tempDump,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);
                await source.CopyToAsync(target, cancellationToken);
            }

            NpgsqlConnection.ClearAllPools();
            await RunPostgresToolAsync(
                "pg_restore",
                tempDump,
                restore: true,
                cancellationToken);
            NpgsqlConnection.ClearAllPools();
        }
        finally
        {
            TryDelete(tempDump);
        }
    }

    private async Task RunPostgresToolAsync(
        string executable,
        string dumpPath,
        bool restore,
        CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnectionStringBuilder(databaseService.ConnectionString);
        var host = connection.Host
            ?? throw new InvalidOperationException("В строке подключения PostgreSQL не указан Host.");
        var username = connection.Username
            ?? throw new InvalidOperationException("В строке подключения PostgreSQL не указан Username.");
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolvePostgresExecutable(executable),
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        startInfo.Environment["PGPASSWORD"] = connection.Password ?? string.Empty;
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add(host);
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(connection.Port.ToString());
        startInfo.ArgumentList.Add("--username");
        startInfo.ArgumentList.Add(username);
        startInfo.ArgumentList.Add("--dbname");
        startInfo.ArgumentList.Add(connection.Database ?? "postgres");

        if (restore)
        {
            startInfo.ArgumentList.Add("--clean");
            startInfo.ArgumentList.Add("--if-exists");
            startInfo.ArgumentList.Add("--no-owner");
            startInfo.ArgumentList.Add("--no-privileges");
            startInfo.ArgumentList.Add("--exit-on-error");
            startInfo.ArgumentList.Add(dumpPath);
        }
        else
        {
            startInfo.ArgumentList.Add("--format=custom");
            startInfo.ArgumentList.Add("--file");
            startInfo.ArgumentList.Add(dumpPath);
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
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var error = await errorTask;
            await outputTask;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"{executable} завершился с кодом {process.ExitCode}: {error}");
            }
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
