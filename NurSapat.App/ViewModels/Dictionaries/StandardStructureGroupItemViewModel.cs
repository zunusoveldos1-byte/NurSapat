using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Structures;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StandardStructureGroupItemViewModel : ObservableObject
{
    public Guid Id { get; init; }

    [ObservableProperty]
    private string _name = string.Empty;

    public static StandardStructureGroupItemViewModel FromModel(StandardStructureGroup group) => new()
    {
        Id = group.Id,
        Name = group.Name
    };

    public void RefreshFrom(StandardStructureGroup group) => Name = group.Name;
}
