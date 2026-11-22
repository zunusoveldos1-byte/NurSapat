using System.Text.Json;
using System.IO.Compression;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services.Documents;

public interface IProjectSerializer
{
    Task<ProjectDocumentData> LoadAsync(string filePath, CancellationToken cancellationToken = default);

    Task SaveAsync(ProjectDocumentData data, string filePath, CancellationToken cancellationToken = default);
}

public sealed class ProjectSerializer : IProjectSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ProjectDocumentData> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Архив проекта не найден.", filePath);
        }

        await using var stream = await OpenProjectPayloadAsync(filePath, cancellationToken);
        var project = await JsonSerializer.DeserializeAsync<ProjectDocumentData>(
                stream,
                JsonOptions,
                cancellationToken)
            ?? throw new FormatException("Архив проекта NurSapat повреждён или имеет неподдерживаемый формат.");

        if (project.SchemaVersion > ProjectFormat.CurrentSchemaVersion)
        {
            throw new FormatException(
                "Файл проекта создан в более новой версии схемы. Обновите версию программы NurSapat.");
        }

        return project;
    }

    public async Task SaveAsync(ProjectDocumentData data, string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new DirectoryNotFoundException("Не удалось определить папку архива.");
        Directory.CreateDirectory(directory);

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"nursapat-zp-{Guid.NewGuid():N}");
        var tempPayload = Path.Combine(tempDirectory, ProjectFormat.ArchiveEntryName);
        var tempArchive = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(tempDirectory);
            await using (var stream = new FileStream(
                             tempPayload,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            ZipFile.CreateFromDirectory(
                tempDirectory,
                tempArchive,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);
            File.Move(tempArchive, fullPath, overwrite: true);
        }
        finally
        {
            TryDeleteFile(tempArchive);
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static async Task<Stream> OpenProjectPayloadAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (Path.GetExtension(filePath).Equals(".nsp", StringComparison.OrdinalIgnoreCase))
        {
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                useAsync: true);
        }

        var memory = new MemoryStream();
        using (var archive = ZipFile.OpenRead(filePath))
        {
            var entry = archive.GetEntry(ProjectFormat.ArchiveEntryName)
                ?? throw new InvalidDataException(
                    $"Архив не содержит обязательный файл {ProjectFormat.ArchiveEntryName}.");
            await using var entryStream = entry.Open();
            await entryStream.CopyToAsync(memory, cancellationToken);
        }

        memory.Position = 0;
        return memory;
    }

    private static void TryDeleteFile(string path)
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
            // Cleanup must not hide the original save exception.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Cleanup must not hide the original save exception.
        }
    }
}
