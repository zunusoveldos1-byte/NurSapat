namespace NurSapat.Cad.Tools;

public sealed record CadToolInfo(
    CadToolKind Kind,
    string Id,
    string DisplayName,
    CadToolGroup Group,
    CadToolInteraction Interaction,
    bool IsImplemented);

/// <summary>Метаданные зарегистрированных CAD-инструментов.</summary>
public static class CadToolMetadata
{
    private static readonly IReadOnlyDictionary<CadToolKind, CadToolInfo> ByKind;
    private static readonly IReadOnlyDictionary<string, CadToolKind> ById;

    static CadToolMetadata()
    {
        var entries = new CadToolInfo[]
        {
            Info(CadToolKind.SelectElements, "select-elements", "Выделение элементов", CadToolGroup.SystemAndEditing, CadToolInteraction.Persistent, true),
            Info(CadToolKind.DeleteLastPlaced, "delete-last-placed", "Удалить последний элемент", CadToolGroup.SystemAndEditing, CadToolInteraction.InstantAction, true),
            Info(CadToolKind.Undo, "undo", "Отменить действие", CadToolGroup.SystemAndEditing, CadToolInteraction.InstantAction, true),
            Info(CadToolKind.SelectObject, "select-object", "Выделение объекта", CadToolGroup.SelectionAndFiltering, CadToolInteraction.Persistent, true),
            Info(CadToolKind.SelectProfile, "select-profile", "Выделение профиля", CadToolGroup.SelectionAndFiltering, CadToolInteraction.Persistent, true),
            Info(CadToolKind.SelectVertex, "select-vertex", "Выделение вершины", CadToolGroup.SelectionAndFiltering, CadToolInteraction.Persistent, true),
            Info(CadToolKind.AlignGlazingUnits, "align-glazing", "Выравнивание стеклопакетов", CadToolGroup.DimensionsAndFill, CadToolInteraction.InstantAction, false),
            Info(CadToolKind.AlignDeadLights, "align-dead-lights", "Выравнивание глухих просветов", CadToolGroup.DimensionsAndFill, CadToolInteraction.InstantAction, false),
            Info(CadToolKind.SetVertexCoordinates, "set-vertex-coordinates", "Установить координаты вершины", CadToolGroup.GeometryAndImposts, CadToolInteraction.Persistent, false),
            Info(CadToolKind.SplitProfile, "split-profile", "Разбить профиль надвое", CadToolGroup.GeometryAndImposts, CadToolInteraction.Persistent, false),
            Info(CadToolKind.OffsetImpostVertical, "offset-impost-v", "Сместить импост верт.", CadToolGroup.GeometryAndImposts, CadToolInteraction.Persistent, true),
            Info(CadToolKind.OffsetImpostHorizontal, "offset-impost-h", "Сместить импост гориз.", CadToolGroup.GeometryAndImposts, CadToolInteraction.Persistent, true),
            Info(CadToolKind.AlignImposts, "align-imposts", "Выравнивание импостов", CadToolGroup.GeometryAndImposts, CadToolInteraction.InstantAction, false),
            Info(CadToolKind.ChangeOverallDimensions, "change-dimensions", "Изменить габаритные размеры", CadToolGroup.DimensionsAndFill, CadToolInteraction.InstantAction, false),
            Info(CadToolKind.DeleteSelection, "delete-selection", "Удаление", CadToolGroup.SystemAndEditing, CadToolInteraction.InstantAction, true),
            Info(CadToolKind.ClearConstruction, "clear-construction", "Очистить конструкцию", CadToolGroup.SystemAndEditing, CadToolInteraction.InstantAction, true),
            Info(CadToolKind.RadiusVertex, "radius-vertex", "Выделение радиусных вершин", CadToolGroup.SelectionAndFiltering, CadToolInteraction.Persistent, true),
        };

        ByKind = entries.ToDictionary(i => i.Kind);
        ById = entries
            .SelectMany(i => new[] { KeyValuePair.Create(i.Id, i.Kind) })
            .Concat([KeyValuePair.Create("select", CadToolKind.SelectElements)])
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    public static CadToolInfo Get(CadToolKind kind) =>
        ByKind.TryGetValue(kind, out var info)
            ? info
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Неизвестный инструмент.");

    public static CadToolKind? ResolveKind(string id) =>
        ById.TryGetValue(id, out var kind) ? kind : null;

    public static string GetId(CadToolKind kind) => Get(kind).Id;

    public static string GetDisplayName(CadToolKind kind) => Get(kind).DisplayName;

    public static CadToolGroup GetGroup(CadToolKind kind) => Get(kind).Group;

    public static CadToolInteraction GetInteraction(CadToolKind kind) => Get(kind).Interaction;

    public static IReadOnlyList<CadToolInfo> GetByGroup(CadToolGroup group) =>
        ByKind.Values.Where(i => i.Group == group).OrderBy(i => (int)i.Kind).ToList();

    public static IReadOnlyList<CadToolInfo> All => ByKind.Values.OrderBy(i => (int)i.Kind).ToList();

    private static CadToolInfo Info(
        CadToolKind kind,
        string id,
        string displayName,
        CadToolGroup group,
        CadToolInteraction interaction,
        bool isImplemented) =>
        new(kind, id, displayName, group, interaction, isImplemented);
}
