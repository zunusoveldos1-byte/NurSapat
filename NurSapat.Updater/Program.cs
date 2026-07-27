using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;
using NurSapat.Infrastructure.Updates.Options;
using NurSapat.Infrastructure.Updates.Services;

namespace NurSapat.Updater;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var arguments = UpdaterArguments.Parse(args);
        if (!arguments.IsValid)
        {
            Console.Error.WriteLine(arguments.ErrorMessage);
            return 1;
        }

        var logger = new FileUpdateLogger();
        var options = new UpdateOptions();
        var fileReplacer = new UpdateFileReplacer(logger);
        var rollbackService = new RollbackService(fileReplacer, logger);
        var engine = new UpdaterEngine(fileReplacer, rollbackService, logger, options);

        try
        {
            return await engine.RunAsync(arguments);
        }
        catch (Exception ex)
        {
            logger.Error("Критическая ошибка Updater.", ex);
            return 2;
        }
        finally
        {
            logger.Dispose();
        }
    }
}
