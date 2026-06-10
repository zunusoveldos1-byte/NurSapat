using System.Text.Json;
using Avalonia.Platform.Storage;

namespace NurSapat.App.Services;

public sealed class ProjectFileDto
{
    public int SchemaVersion { get; init; } = ProjectFileManager.SupportedSchemaVersion;

    public string AppVersion { get; init; } = string.Empty;

    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;

    public string OrderNumber { get; init; } = string.Empty;

    public int BaseWidth { get; init; }

    public int BaseHeight { get; init; }

    public string ProfileSystem { get; init; } = string.Empty;

    public JsonElement RootSectionJson { get; init; }
}

public sealed class ProjectFileManager
{
    public const int SupportedSchemaVersion = 1;

    private static readonly FilePickerFileType NurSapatProjectFileType = new("NurSapat Project")
    {
        Patterns = new[] { "*.nsp" },
        AppleUniformTypeIdentifiers = new[] { "com.nursapat.project" },
        MimeTypes = new[] { "application/vnd.nursapat.project+json" }
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<IStorageFile?> SaveProjectAsync(
        IStorageProvider storageProvider,
        ProjectFileDto projectData,
        IStorageFile? existingFile = null)
    {
        ArgumentNullException.ThrowIfNull(storageProvider);
        ArgumentNullException.ThrowIfNull(projectData);

        var targetFile = existingFile ?? await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить проект NurSapat",
            SuggestedFileName = BuildSuggestedFileName(projectData.OrderNumber),
            DefaultExtension = "nsp",
            FileTypeChoices = new[] { NurSapatProjectFileType }
        });

        if (targetFile is null)
        {
            return null;
        }

        await using var stream = await targetFile.OpenWriteAsync();
        stream.SetLength(0);
        await JsonSerializer.SerializeAsync(stream, projectData, JsonOptions);
        return targetFile;
    }

    public async Task<ProjectFileDto?> OpenProjectAsync(IStorageProvider storageProvider)
    {
        ArgumentNullException.ThrowIfNull(storageProvider);

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть проект NurSapat",
            AllowMultiple = false,
            FileTypeFilter = new[] { NurSapatProjectFileType }
        });

        var sourceFile = files.FirstOrDefault();
        if (sourceFile is null)
        {
            return null;
        }

        await using var stream = await sourceFile.OpenReadAsync();
        var project = await JsonSerializer.DeserializeAsync<ProjectFileDto>(stream, JsonOptions)
            ?? throw new FormatException("Файл проекта NurSapat поврежден или имеет неподдерживаемый формат.");

        if (project.SchemaVersion > SupportedSchemaVersion)
        {
            throw new FormatException("Файл проекта создан в более новой версии схемы. Обновите версию программы NurSapat.");
        }

        return project;
    }

    private static string BuildSuggestedFileName(string orderNumber)
    {
        var normalizedOrderNumber = string.IsNullOrWhiteSpace(orderNumber)
            ? "БезНомера"
            : string.Join("_", orderNumber.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        return $"Заказ_{normalizedOrderNumber}";
    }
}
