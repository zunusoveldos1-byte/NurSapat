using System.Reflection;
using NurSapat.App.Services.Settings;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Project;
using NurSapat.Core.Serialization;

namespace NurSapat.App.Services.Documents;

public sealed class DocumentManager : IDocumentManager
{
    private readonly OrderWorkspaceViewModel _workspace;
    private readonly CadEditorViewModel _cadEditor;
    private readonly IProjectSerializer _serializer;
    private readonly IDialogService _dialogService;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IAutoSaveService _autoSaveService;
    private readonly IPrintSettingsService _printSettingsService;
    private readonly IWorkspaceWindowService _workspaceWindowService;
    private readonly IEnvironmentSettingsService _environmentSettingsService;

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
        IEnvironmentSettingsService environmentSettingsService)
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

        _workspace.ProjectModified += OnProjectModified;
        _cadEditor.Commands.CommandExecuted += (_, _) => MarkModifiedIfAllowed();
        _cadEditor.SceneInvalidated += (_, _) => MarkModifiedIfAllowed();

        _hasOpenDocument = true;
        _isModified = false;
    }

    public bool HasOpenDocument => _hasOpenDocument;

    public bool IsModified => _isModified;

    public string? FilePath => _filePath;

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
            return _isModified
                ? $"NurSapat CAD — {name} *"
                : $"NurSapat CAD — {name}";
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

    public async Task<bool> SaveOrderAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            var suggested = BuildSuggestedFileName();
            var path = await _dialogService.ShowSaveFileDialogAsync(suggested, cancellationToken);
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            _filePath = path;
        }

        return await SaveToFileAsync(_filePath, cancellationToken);
    }

    public async Task<bool> SaveAsCopyAsync(CancellationToken cancellationToken = default)
    {
        var suggested = BuildSuggestedFileName();
        var path = await _dialogService.ShowSaveFileDialogAsync(suggested, cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return await SaveToFileAsync(path, cancellationToken);
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
        _workspaceWindowService.ReturnToLogin();
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
        _workspaceWindowService.ReturnToLogin();
        return true;
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
        bool isAutoSaveRecovery = false)
    {
        try
        {
            var project = await _serializer.LoadAsync(path, cancellationToken);
            ApplyProject(project);
            _filePath = isAutoSaveRecovery ? null : path;
            _isModified = isAutoSaveRecovery;
            _hasOpenDocument = true;

            if (!isAutoSaveRecovery)
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
            var snapshot = CreateSnapshot();
            await _serializer.SaveAsync(snapshot, path, cancellationToken);
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
        var orderNumber = _workspace.Order.Header.OrderNumber;
        var normalized = string.IsNullOrWhiteSpace(orderNumber)
            ? "НовыйЗаказ"
            : string.Join("_", orderNumber.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return $"Заказ_{normalized}";
    }

    private void OnProjectModified(object? sender, EventArgs e) => MarkModifiedIfAllowed();

    private void MarkModifiedIfAllowed() => MarkModified();

    private void RaiseStateChanged() => DocumentStateChanged?.Invoke(this, EventArgs.Empty);
}
