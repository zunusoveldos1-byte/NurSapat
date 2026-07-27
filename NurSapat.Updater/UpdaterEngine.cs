using System.Diagnostics;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Options;
using NurSapat.Infrastructure.Updates.Services;

namespace NurSapat.Updater;

internal sealed class UpdaterEngine
{
    private readonly UpdateFileReplacer _fileReplacer;
    private readonly IRollbackService _rollbackService;
    private readonly IUpdateLogger _logger;
    private readonly UpdateOptions _options;

    public UpdaterEngine(
        UpdateFileReplacer fileReplacer,
        IRollbackService rollbackService,
        IUpdateLogger logger,
        UpdateOptions options)
    {
        _fileReplacer = fileReplacer;
        _rollbackService = rollbackService;
        _logger = logger;
        _options = options;
    }

    public async Task<int> RunAsync(UpdaterArguments arguments, CancellationToken cancellationToken = default)
    {
        _logger.Info("Updater запущен.");

        await WaitForProcessExitAsync(arguments.ProcessId, cancellationToken);

        Directory.CreateDirectory(arguments.BackupDirectory!);
        await _fileReplacer.CreateBackupAsync(arguments.TargetDirectory!, arguments.BackupDirectory!, cancellationToken);

        var replaceResult = await _fileReplacer.ExtractAndReplaceAsync(
            arguments.PackagePath!,
            arguments.TargetDirectory!,
            cancellationToken);

        if (!replaceResult.Success)
        {
            await _rollbackService.RestoreFromBackupAsync(
                arguments.BackupDirectory!,
                arguments.TargetDirectory!,
                cancellationToken);
            return 3;
        }

        var verifyResult = _fileReplacer.VerifyRequiredFiles(arguments.TargetDirectory!, arguments.ApplicationExeName);
        if (!verifyResult.Success)
        {
            _logger.Error(verifyResult.Message);
            await _rollbackService.RestoreFromBackupAsync(
                arguments.BackupDirectory!,
                arguments.TargetDirectory!,
                cancellationToken);
            return 4;
        }

        TryDelete(arguments.PackagePath!);
        LaunchApplication(arguments);
        _logger.Info("Обновление завершено.");
        return 0;
    }

    private async Task WaitForProcessExitAsync(int processId, CancellationToken cancellationToken)
    {
        if (processId <= 0)
        {
            return;
        }

        _logger.Info($"Ожидание завершения процесса {processId}.");
        try
        {
            var process = Process.GetProcessById(processId);
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (ArgumentException)
        {
            _logger.Info("Процесс приложения уже завершён.");
        }
    }

    private void LaunchApplication(UpdaterArguments arguments)
    {
        var exePath = Path.Combine(arguments.TargetDirectory!, arguments.ApplicationExeName);
        var versionArg = arguments.TargetVersion is not null
            ? $" --update-success={arguments.TargetVersion}"
            : " --update-success";

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = versionArg.Trim(),
            WorkingDirectory = arguments.TargetDirectory,
            UseShellExecute = true
        };

        Process.Start(startInfo);
        _logger.Info($"Запущено приложение: {exePath}");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort
        }
    }
}
