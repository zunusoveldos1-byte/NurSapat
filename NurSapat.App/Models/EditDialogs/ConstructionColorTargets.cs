namespace NurSapat.App.Models.EditDialogs;

[Flags]
public enum ConstructionColorTargets
{
    None = 0,
    Profile = 1,
    Imposts = 2,
    Sashes = 4,
    Layout = 8,
    DecorativeElements = 16,
    All = Profile | Imposts | Sashes | Layout | DecorativeElements
}
