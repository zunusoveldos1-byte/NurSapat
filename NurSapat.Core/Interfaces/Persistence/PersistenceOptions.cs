namespace NurSapat.Core.Interfaces.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public PersistenceProvider Provider { get; set; } = PersistenceProvider.Demo;

    public string ConnectionStringName { get; set; } = "Default";
}
