using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services.Documents;

public interface IPrintSettingsService
{
    PrintSettings Current { get; }

    void Load(PrintSettings settings);

    PrintSettings CloneCurrent();
}

public sealed class PrintSettingsService : IPrintSettingsService
{
    public PrintSettings Current { get; private set; } = new();

    public void Load(PrintSettings settings) => Current = settings.Clone();

    public PrintSettings CloneCurrent() => Current.Clone();
}
