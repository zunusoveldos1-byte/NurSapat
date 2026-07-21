using NurSapat.Infrastructure.Updates.Abstractions;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class FileUpdateLogger : IUpdateLogger, IDisposable
{
    private readonly string _logFilePath;
    private readonly object _sync = new();
    private readonly StreamWriter _writer;

    public FileUpdateLogger()
    {
        var logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NurSapat",
            "Logs");

        Directory.CreateDirectory(logsDirectory);
        _logFilePath = Path.Combine(logsDirectory, "Update.log");
        _writer = new StreamWriter(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
    }

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", message);
        if (exception is not null)
        {
            Write("ERROR", exception.ToString());
        }
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_sync)
        {
            _writer.WriteLine(line);
        }
    }

    public void Dispose() => _writer.Dispose();
}
