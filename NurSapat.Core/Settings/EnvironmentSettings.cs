namespace NurSapat.Core.Settings;

public sealed class EnvironmentSettings
{
    public string DbPath { get; init; }

    public string DocumentsPath { get; init; }

    public string SketchesPath { get; init; }

    public string LogsPath { get; init; }

    public EnvironmentSettings()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NurSapat");

        DbPath = Path.Combine(root, "db");
        DocumentsPath = Path.Combine(root, "documents");
        SketchesPath = Path.Combine(root, "sketches");
        LogsPath = Path.Combine(root, "logs");
    }

    public EnvironmentSettings(string dbPath, string documentsPath, string sketchesPath, string logsPath)
    {
        DbPath = dbPath;
        DocumentsPath = documentsPath;
        SketchesPath = sketchesPath;
        LogsPath = logsPath;
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DbPath);
        Directory.CreateDirectory(DocumentsPath);
        Directory.CreateDirectory(SketchesPath);
        Directory.CreateDirectory(LogsPath);
    }
}
