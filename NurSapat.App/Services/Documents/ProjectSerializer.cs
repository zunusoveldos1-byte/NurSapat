using System.Text.Json;
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
        await using var stream = File.OpenRead(filePath);
        var project = await JsonSerializer.DeserializeAsync<ProjectDocumentData>(stream, JsonOptions, cancellationToken)
            ?? throw new FormatException("Файл проекта NurSapat повреждён или имеет неподдерживаемый формат.");

        if (project.SchemaVersion > ProjectFormat.CurrentSchemaVersion)
        {
            throw new FormatException(
                "Файл проекта создан в более новой версии схемы. Обновите версию программы NurSapat.");
        }

        return project;
    }

    public async Task SaveAsync(ProjectDocumentData data, string filePath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
    }
}
