namespace NurSapat.Cad.Foundation.Workspace;

/// <summary>
/// Идентификатор workspace: уникальный Guid + отображаемое имя.
/// </summary>
public readonly record struct WorkspaceId(Guid Id, string DisplayName)
{
    public static WorkspaceId New(string displayName) => new(Guid.NewGuid(), displayName);

    public override string ToString() => $"{DisplayName} [{Id:B}]";
}
