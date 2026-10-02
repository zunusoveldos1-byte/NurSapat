using NurSapat.App.Models.Config;

namespace NurSapat.App.ViewModels.Config;

public sealed class ConstructionSectionItemViewModel
{
    public ConstructionSectionItemViewModel(ConstructionSectionType sectionType)
    {
        SectionType = sectionType;
        DisplayName = sectionType.GetDisplayName();
    }

    public ConstructionSectionType SectionType { get; }

    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}
