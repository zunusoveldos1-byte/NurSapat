using System.Diagnostics;
using System.Text;

namespace NurSapat.Infrastructure.Sync.Diagnostics;

/// <summary>
/// Diagnostic trace for the startup authentication pipeline. The trace never
/// writes tokens, passwords, encryption keys, or password hashes.
/// </summary>
public static class StartupTrace
{
    private static readonly object Gate = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NurSapat",
        "logs");

    public static string LogFilePath => Path.Combine(LogDirectory, "startup.log");

    public static void Info(string message) => Write("LOG", message);

    public static void Error(string stage, Exception exception) =>
        Write("ERROR", $"{stage}{Environment.NewLine}{exception}");

    private static void Write(string level, string message)
    {
        if (message.StartsWith("[LOG] ", StringComparison.Ordinal))
        {
            message = message[6..];
        }

        var line =
            $"[{DateTimeOffset.Now:O}] [PID:{Environment.ProcessId}] [{level}] {message}";

        Console.WriteLine(line);
        Trace.WriteLine(line);

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    LogFilePath,
                    line + Environment.NewLine,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }
        catch (Exception ex)
        {
            // File logging must never break authentication. Console and Trace
            // remain available, and the logging failure itself is not hidden.
            var fallback =
                $"[{DateTimeOffset.Now:O}] [PID:{Environment.ProcessId}] [ERROR] " +
                $"Не удалось записать startup.log:{Environment.NewLine}{ex}";
            Console.Error.WriteLine(fallback);
            Trace.WriteLine(fallback);
        }
    }
}
