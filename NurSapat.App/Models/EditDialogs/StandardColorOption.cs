namespace NurSapat.App.Models.EditDialogs;

public sealed class StandardColorOption
{
    public required string Name { get; init; }

    public required string Hex { get; init; }

    public bool IsCustomPicker { get; init; }
}
