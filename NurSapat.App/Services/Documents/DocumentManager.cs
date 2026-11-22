using System.Reflection;
using NurSapat.App.Services.Orders;
using NurSapat.App.Services.Settings;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Project;
using NurSapat.Core.Serialization;
using NurSapat.Core.Services.Orders;
using NurSapat.App.Services.Utilities;
using NurSapat.App.Services.Persistence;

namespace NurSapat.App.Services.Documents;

public sealed class DocumentManager : IDocumentManager
{
    /// <summary>Default configuration display name shown in the window title.</summary>
    public const string DefaultConfigurationName = "eldos";

    private readonly OrderWorkspaceViewModel _workspace;
    private readonly CadEditorViewModel _cadEditor;
    private readonly IProjectSerializer _serializer;
    private readonly IDialogService _dialogService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IAutoSaveService _autoSaveService;
    private readonly IPrintSettingsService _printSettingsService;
    private readonly IWorkspaceWindowService _workspaceWindowService;
    private readonly IEnvironmentSettingsService _environmentSettingsService;
    private readonly IOrdersStore _ordersStore;
    private readonly OrderLifecycleService _orderLifecycleService;
    private readonly IDatabaseBackupPayloadService _databaseBackupPayloadService;
    private readonly IApplicationPersistenceInitializer _persistenceInitializer;

    private bool _isModified;
    private bool _suppressModification;
    private string? _filePath;
    private bool _hasOpenDocument;

    public DocumentManager(
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        IProjectSerializer serializer,
        IDialogService dialogService,
        IRecentFilesService recentFilesService,
        IAutoSaveService autoSaveService,
        IPrintSettingsService printSettingsService,
        IWorkspaceWindowService workspaceWindowService,
        IEnvironmentSettingsService environmentSettingsService,
        IOrdersStore ordersStore,
        OrderLifecycleService orderLifecycleService,
        IDatabaseBackupPayloadService databaseBackupPayloadService,
        IApplicationPersistenceInitializer persistenceInitializer)
    {
        _workspace = workspace;
        _cadEditor = cadEditor;
        _serializer = serializer;
        _dialogService = dialogService;
        _recentFilesService = recentFilesService;
        _autoSaveService = autoSaveService;
        _printSettingsService = printSettingsService;
        _workspaceWindowService = workspaceWindowService;
        _environmentSettingsService = environmentSettingsService;
        _ordersStore = ordersStore;
        _orderLifecycleService = orderLifecycleService;
        _databaseBackupPayloadService = databaseBackupPayloadService;
        _persistenceInitializer = persistenceInitializer;

        _workspace.ProjectModified += OnProjectModified;
        _cadEditor.Commands.CommandExecuted += (_, _) => MarkModifiedIfAllowed();
        _cadEditor.SceneInvalidated += (_, _) => MarkModifiedIfAllowed();
        _environmentSettingsService.ProfileChanged += (_, _) => RaiseStateChanged();

        _hasOpenDocument = true;
        _isModified = false;
    }

    public bool HasOpenDocument => _hasOpenDocument;

    public bool IsModified => _isModified;

    public string? FilePath => _filePath;

    /// <summary>
    /// Active configuration name for the window title (falls back to <see cref="DefaultConfigurationName"/>).
    /// </summary>
    public string ConfigurationName
    {
        get
        {
            var profileName = _environmentSettingsService.ActiveProfile?.ProfileName;
            return string.IsNullOrWhiteSpace(profileName)
                ? DefaultConfigurationName
                : profileName.Trim();
        }
    }

    public string WindowTitle
    {
        get
        {
            if (!_hasOpenDocument)
            {
                return "NurSapat CAD";
            }

            var name = _filePath is null
                ? "Новый заказ"
                : Path.GetFileName(_filePath);
            var title = $"NurSapat CAD — {name} ({ConfigurationName})";
            return _isModified ? $"{title} *" : title;
        }
    }

    public event EventHandler? DocumentStateChanged;

    public ProjectDocumentData CreateSnapshot()
    {
        _workspace.SyncCadToOrder();

        var activeConstruction = _workspace.Order.SelectedConstruction
            ?? _workspace.Order.EnsureDefaultConstruction();

        return new ProjectDocumentData
        {
            SchemaVersion = ProjectFormat.CurrentSchemaVersion,
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            SavedAt = DateTimeOffset.UtcNow,
            Order = OrderProjectMapper.ToData(_workspace.Order),
            Cad = CadProjectSnapshotHelper.Capture(_cadEditor.CaptureSnapshot()),
            PrintSettings = _printSettingsService.CloneCurrent(),
            ActiveConstructionId = activeConstruction.Id
        };
    }

    public async Task<bool> NewOrderAsync(CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOrSaveAsync(
                "Создать новый заказ",
                "Текущий заказ содержит несохранённые изменения.\n\nЧто необходимо сделать?",
                cancellationToken))
        {
            return false;
        }

        if (!await CreateNowSnapshotAsync(cancellationToken))
        {
            return false;
        }

        ResetWorkspaceState();
        _environmentSettingsService.ApplyToNewOrderHeader(_workspace.Order.Header);
        _workspace.Sheet.ReloadFromOrder(null);
        return true;
    }

    public async Task<bool> OpenOrderAsync(string? filePath = null, CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOrSaveAsync(
                "Открыть заказ",
                "Текущий заказ содержит несохранённые изменения.\n\nЧто необходимо сделать?",
                cancellationToken))
        {
            return false;
        }

        var path = filePath ?? await _dialogService.ShowOpenFileDialogAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return await LoadProjectFromFileAsync(path, cancellationToken);
    }

    public async Task<bool> OpenOrderFromDatabaseAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOrSaveAsync(
                "Открыть заказ",
                "Текущий заказ содержит несохранённые изменения.\n\nЧто необходимо сделать?",
                cancellationToken))
        {
            return false;
        }

        var project = _ordersStore.TryLoadProject(orderId);
        if (project is null)
        {
            await _dialogService.ShowErrorAsync("Заказ не найден в базе данных.", cancellationToken);
            return false;
        }

        ApplyProject(project);
        var header = _ordersStore.GetById(orderId);
        _filePath = header?.FilePath is { Length: > 0 } path && File.Exists(path) ? path : null;
        _isModified = false;
        _hasOpenDocument = true;
        _autoSaveService.ClearAutoSave();
        RaiseStateChanged();
        return true;
    }

    public async Task<bool> SaveOrderAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _orderLifecycleService.AssertCanEdit(_workspace.Order);
        }
        catch (InvalidOperationException ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, cancellationToken);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_filePath))
        {
            return await SaveAsCopyAsync(cancellationToken);
        }

        return await SaveToFileAsync(_filePath, cancellationToken);
    }

    public Task<bool> SaveOrderToPathAsync(string path, CancellationToken cancellationToken = default) =>
        SaveToFileAsync(path, cancellationToken);

    public async Task<bool> CreateBackupAsync(string path, CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, $".backup-{Guid.NewGuid():N}.tmp");
        try
        {
            await _serializer.SaveAsync(CreateSnapshot(), tempPath, cancellationToken);
            await _databaseBackupPayloadService.AddDatabasePayloadAsync(tempPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, EnsureZpExtension(path), overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                $"Не удалось создать резервную копию: {ex.Message}",
                cancellationToken);
            return false;
        }
        finally
        {
            try { File.Delete(tempPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public async Task<bool> RestorePreparedBackupAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOrSaveAsync(
                "Восстановить резервную копию",
                "Текущий проект содержит несохранённые изменения. Что необходимо сделать?",
                cancellationToken))
        {
            return false;
        }

        return await LoadProjectFromFileAsync(
            path,
            cancellationToken,
            restoreDatabase: true);
    }

    public async Task<bool> SaveAsCopyAsync(CancellationToken cancellationToken = default)
    {
        var suggested = BuildSuggestedFileName();
        var path = await _dialogService.ShowSaveFileDialogAsync(suggested, cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return await SaveToFileAsync(EnsureUniqueArchivePath(path), cancellationToken);
    }

    public async Task<bool> CloseOrderAsync(CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOrSaveAsync(
                "Закрыть заказ",
                "Закрыть заказ?\n\nЕсть несохранённые изменения.",
                cancellationToken))
        {
            return false;
        }

        StopAutoSave();
        _autoSaveService.ClearAutoSave();
        ResetWorkspaceState();
        _hasOpenDocument = false;
        await _workspaceWindowService.ReturnToLoginAsync();
        return true;
    }

    public async Task<bool> ExitToLoginAsync(CancellationToken cancellationToken = default)
    {
        if (!await ConfirmDiscardOrSaveAsync(
                "Выход",
                "Проект содержит несохранённые изменения.\n\nЧто сделать?",
                cancellationToken))
        {
            return false;
        }

        StopAutoSave();
        _autoSaveService.ClearAutoSave();
        ResetWorkspaceState();
        _hasOpenDocument = false;
        await _workspaceWindowService.ReturnToLoginAsync();
        return true;
    }

    public async Task<bool> ConfirmCloseAsync(CancellationToken cancellationToken = default)
    {
        if (!_isModified)
        {
            StopAutoSave();
            return true;
        }

        while (true)
        {
            var choice = await _dialogService.ShowUnsavedChangesDialogAsync(
                "Выход",
                "Проект содержит несохранённые изменения.\n\nЧто сделать?",
                cancellationToken);

            switch (choice)
            {
                case UnsavedChangesChoice.Cancel:
                    return false;
                case UnsavedChangesChoice.Discard:
                    StopAutoSave();
                    return true;
                case UnsavedChangesChoice.Save:
                {
                    // Prefer .zp archive save: existing path, otherwise Save File dialog.
                    var saved = string.IsNullOrWhiteSpace(_filePath)
                        ? await SaveAsCopyAsync(cancellationToken)
                        : await SaveOrderAsync(cancellationToken);
                    if (saved)
                    {
                        StopAutoSave();
                        return true;
                    }

                    break;
                }
                case UnsavedChangesChoice.SaveAsCopy:
                    if (await SaveAsCopyAsync(cancellationToken))
                    {
                        StopAutoSave();
                        return true;
                    }

                    break;
            }
        }
    }

    public async Task<bool> TryRecoverAutoSaveAsync(CancellationToken cancellationToken = default)
    {
        if (!_autoSaveService.HasAutoSaveFile)
        {
            return false;
        }

        var choice = await _dialogService.ShowAutoSaveRecoveryDialogAsync(cancellationToken);
        if (choice != YesNoChoice.Yes)
        {
            _autoSaveService.ClearAutoSave();
            return false;
        }

        var path = _autoSaveService.AutoSaveFilePath!;
        return await LoadProjectFromFileAsync(path, cancellationToken, isAutoSaveRecovery: true);
    }

    public void PrepareWorkspace()
    {
        if (_hasOpenDocument)
        {
            return;
        }

        ResetWorkspaceState();
        _environmentSettingsService.ApplyToNewOrderHeader(_workspace.Order.Header);
        _workspace.Sheet.ReloadFromOrder(null);
    }

    public void MarkModified()
    {
        if (_suppressModification || !_hasOpenDocument)
        {
            return;
        }

        if (_isModified)
        {
            return;
        }

        _isModified = true;
        RaiseStateChanged();
    }

    public void StartAutoSave() => _autoSaveService.Start(this);

    public void StopAutoSave() => _autoSaveService.Stop();

    private async Task<bool> LoadProjectFromFileAsync(
        string path,
        CancellationToken cancellationToken,
        bool isAutoSaveRecovery = false,
        bool restoreDatabase = false)
    {
        try
        {
            var project = await _serializer.LoadAsync(path, cancellationToken);
            if (!isAutoSaveRecovery
                && !restoreDatabase
                && !await CreateNowSnapshotAsync(cancellationToken))
            {
                return false;
            }

            if (restoreDatabase)
            {
                var safetyPath = Path.Combine(GetBackupDirectory(),
                    $"before-restore-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zp");
                if (!await CreateBackupAsync(safetyPath, cancellationToken))
                {
                    return false;
                }
                await _databaseBackupPayloadService.RestoreDatabasePayloadAsync(path, cancellationToken);
                await _persistenceInitializer.PrepareRestoredDatabaseAsync(cancellationToken);
            }

            ApplyProject(project);
            _filePath = isAutoSaveRecovery || restoreDatabase ? null : path;
            _isModified = isAutoSaveRecovery || restoreDatabase;
            _hasOpenDocument = true;

            if (!isAutoSaveRecovery && !restoreDatabase)
            {
                _recentFilesService.AddRecentFile(path);
                _autoSaveService.ClearAutoSave();
            }

            RaiseStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, cancellationToken);
            return false;
        }
    }

    private async Task<bool> SaveToFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            _orderLifecycleService.AssertCanEdit(_workspace.Order);
            path = EnsureZpExtension(path);
            var snapshot = CreateSnapshot();
            await _serializer.SaveAsync(snapshot, path, cancellationToken);
            _ordersStore.UpsertFromProject(snapshot, path);
            _filePath = path;
            _isModified = false;
            _recentFilesService.AddRecentFile(path);
            _autoSaveService.ClearAutoSave();
            RaiseStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message, cancellationToken);
            return false;
        }
    }

    private void ApplyProject(ProjectDocumentData project)
    {
        _suppressModification = true;
        try
        {
            OrderProjectMapper.ApplyData(_workspace.Order, project.Order);
            _printSettingsService.Load(project.PrintSettings);
            _workspace.ReloadFromProject(project);
            _cadEditor.ApplySnapshot(CadProjectSnapshotHelper.ToSnapshot(project.Cad), _workspace.GetActiveGeometryElements());
            _isModified = false;
        }
        finally
        {
            _suppressModification = false;
        }
    }

    private void ResetWorkspaceState()
    {
        _suppressModification = true;
        try
        {
            _filePath = null;
            _isModified = false;
            _hasOpenDocument = true;
            OrderProjectMapper.Reset(_workspace.Order);
            _printSettingsService.Load(new PrintSettings());
            _workspace.ResetWorkspace();
            _cadEditor.ResetWorkspace();
            _cadEditor.ApplySnapshot(
                new CadEditorViewModel.CadEditorSnapshot(),
                _workspace.GetActiveGeometryElements());
        }
        finally
        {
            _suppressModification = false;
            RaiseStateChanged();
        }
    }

    private async Task<bool> ConfirmDiscardOrSaveAsync(
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        if (!_isModified)
        {
            return true;
        }

        while (true)
        {
            var choice = await _dialogService.ShowUnsavedChangesDialogAsync(title, message, cancellationToken);
            switch (choice)
            {
                case UnsavedChangesChoice.Cancel:
                    return false;
                case UnsavedChangesChoice.Discard:
                    return true;
                case UnsavedChangesChoice.Save:
                    if (await SaveOrderAsync(cancellationToken))
                    {
                        return true;
                    }

                    break;
                case UnsavedChangesChoice.SaveAsCopy:
                    if (await SaveAsCopyAsync(cancellationToken))
                    {
                        return true;
                    }

                    break;
            }
        }
    }

    private string BuildSuggestedFileName()
    {
        return $"{DateTime.Now:yyyyMMdd}{ProjectFormat.Extension}";
    }

    private async Task<bool> CreateNowSnapshotAsync(CancellationToken cancellationToken)
    {
        if (!_hasOpenDocument)
        {
            return true;
        }

        try
        {
            var backupDirectory = GetBackupDirectory();
            Directory.CreateDirectory(backupDirectory);
            var nowPath = Path.Combine(backupDirectory, $"now-project{ProjectFormat.Extension}");
            await _serializer.SaveAsync(CreateSnapshot(), nowPath, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                $"Не удалось сохранить снимок текущего проекта: {ex.Message}",
                cancellationToken);
            return false;
        }
    }

    private string GetBackupDirectory()
    {
        var configured = _environmentSettingsService.Current.BackupPath;
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NurSapat", "Backups")
            : configured;
    }

    private static string EnsureZpExtension(string path) =>
        Path.ChangeExtension(path, ProjectFormat.Extension);

    private static string EnsureUniqueArchivePath(string path)
    {
        path = EnsureZpExtension(path);
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var timestamped = Path.Combine(
            directory,
            $"{baseName}_{DateTime.Now:HHmmss}{ProjectFormat.Extension}");
        if (!File.Exists(timestamped))
        {
            return timestamped;
        }

        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(
                directory,
                $"{baseName}_{index}{ProjectFormat.Extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private void OnProjectModified(object? sender, EventArgs e) => MarkModifiedIfAllowed();

    private void MarkModifiedIfAllowed() => MarkModified();

    private void RaiseStateChanged() => DocumentStateChanged?.Invoke(this, EventArgs.Empty);
}
