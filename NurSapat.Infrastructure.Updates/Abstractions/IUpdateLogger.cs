namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdateLogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}
