namespace NurSapat.App.Models.Help;

public enum HelpTab
{
    Help,
    WhatsNew,
    Update,
    Consultation,
    About
}

public static class HelpTabKeys
{
    public const string Help = "Help";
    public const string WhatsNew = "WhatsNew";
    public const string Update = "Update";
    public const string Consultation = "Consultation";
    public const string About = "About";

    public static HelpTab Parse(string? key) => key switch
    {
        WhatsNew => HelpTab.WhatsNew,
        Update => HelpTab.Update,
        Consultation => HelpTab.Consultation,
        About => HelpTab.About,
        _ => HelpTab.Help
    };
}
