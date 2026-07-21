using System.Text.Json;
using System.Text.Json.Serialization;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

namespace NurSapat.Cad.UI.Services;

public sealed class ConstructionTemplatePayload
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FrameNode.TreeSnapshot? FrameRoot { get; set; }

    public ConstructionAssemblyData? Assembly { get; set; }

    public List<GeometryElement> Elements { get; set; } = [];

    public CadRect? DocumentRect { get; set; }
}

public static class ConstructionTemplateSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static string Serialize(ConstructionMemento memento) =>
        JsonSerializer.Serialize(new ConstructionTemplatePayload
        {
            Assembly = memento.AssemblyData,
            Elements = memento.Elements.Select(ConstructionMemento.CloneElement).ToList(),
            DocumentRect = memento.DocumentRect
        }, Options);

    public static ConstructionMemento? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ConstructionTemplatePayload>(json, Options);
            return payload is null ? null : LegacyTemplateMigration.ToMemento(payload);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
