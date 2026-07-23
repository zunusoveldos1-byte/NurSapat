namespace NurSapat.Updater;

internal sealed class UpdaterArguments
{
    public string? PackagePath { get; init; }

    public string? TargetDirectory { get; init; }

    public int ProcessId { get; init; }

    public string ApplicationExeName { get; init; } = "NurSapat.App.exe";

    public string? BackupDirectory { get; init; }

    public Version? TargetVersion { get; init; }

    public bool Apply { get; init; }

    public bool IsValid { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public static UpdaterArguments Parse(string[] args)
    {
        string? package = null;
        string? target = null;
        string? backup = null;
        string? exe = null;
        string? version = null;
        var pid = 0;
        var apply = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--package" when i + 1 < args.Length:
                    package = args[++i];
                    break;
                case "--target" when i + 1 < args.Length:
                    target = args[++i];
                    break;
                case "--pid" when i + 1 < args.Length && int.TryParse(args[++i], out var parsedPid):
                    pid = parsedPid;
                    break;
                case "--exe" when i + 1 < args.Length:
                    exe = args[++i];
                    break;
                case "--backup" when i + 1 < args.Length:
                    backup = args[++i];
                    break;
                case "--version" when i + 1 < args.Length:
                    version = args[++i];
                    break;
                case "--apply":
                    apply = true;
                    break;
            }
        }

        if (!apply)
        {
            return Invalid("Не указан режим --apply.");
        }

        if (string.IsNullOrWhiteSpace(package) || !File.Exists(package))
        {
            return Invalid("Не найден архив обновления.");
        }

        if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target))
        {
            return Invalid("Не найдена целевая директория приложения.");
        }

        if (string.IsNullOrWhiteSpace(backup))
        {
            return Invalid("Не указана директория резервной копии.");
        }

        Version? parsedVersion = null;
        if (!string.IsNullOrWhiteSpace(version) && Version.TryParse(version, out var v))
        {
            parsedVersion = v;
        }

        return new UpdaterArguments
        {
            PackagePath = package,
            TargetDirectory = target,
            ProcessId = pid,
            ApplicationExeName = string.IsNullOrWhiteSpace(exe) ? "NurSapat.App.exe" : exe,
            BackupDirectory = backup,
            TargetVersion = parsedVersion,
            Apply = true,
            IsValid = true
        };
    }

    private static UpdaterArguments Invalid(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message
    };
}
