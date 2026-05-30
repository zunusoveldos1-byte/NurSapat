using System.Text.Json;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Parametric;

namespace NurSapat.Cad.Core.Templates;

public sealed class CadTemplateSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task SaveAsync(CadDocument document, string filePath)
    {
        var dto = CadTemplateDto.FromDocument(document);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(dto, JsonOptions));
    }

    public async Task<CadDocument> LoadAsync(string filePath)
    {
        var dto = JsonSerializer.Deserialize<CadTemplateDto>(
            await File.ReadAllTextAsync(filePath),
            JsonOptions) ?? throw new InvalidDataException("Invalid NurSapat template.");
        return dto.ToDocument();
    }

    private sealed class CadTemplateDto
    {
        public string Format { get; init; } = "NurSapat.Cad.Template.v1";

        public string Name { get; init; } = string.Empty;

        public WindowOptions Options { get; init; } = new();

        public static CadTemplateDto FromDocument(CadDocument document) => new()
        {
            Name = document.Name,
            Options = document.Structure.Options
        };

        public CadDocument ToDocument()
        {
            var document = new CadDocument { Name = Name };
            document.Structure.InitializeFrame(Options.Width, Options.Height, Options.ProfileKind == ProfileKind.Hidden);
            return document;
        }
    }
}
