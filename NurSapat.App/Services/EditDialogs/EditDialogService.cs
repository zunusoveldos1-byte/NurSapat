using NurSapat.App.Services.Documents;
using NurSapat.App.ViewModels;
using NurSapat.App.ViewModels.Dialogs.Edit;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.App.Services.EditDialogs;

public interface IEditDialogService
{
    Task ShowCopyDrawingDialogAsync(bool includeDimensionLines, CancellationToken cancellationToken = default);

    Task ShowConstructionColorDialogAsync(CancellationToken cancellationToken = default);

    Task ShowOrderColorDialogAsync(CancellationToken cancellationToken = default);
}

public sealed class EditDialogService : IEditDialogService
{
    private readonly IDialogService _dialogService;
    private readonly IDrawingExportService _drawingExportService;
    private readonly IConstructionColorService _constructionColorService;
    private readonly IOrderColorService _orderColorService;
    private readonly ICadPreviewEditorFactory _previewEditorFactory;
    private readonly IRecentColorsService _recentColorsService;
    private readonly OrderWorkspaceViewModel _workspace;
    private readonly CadEditorViewModel _cadEditor;
    private readonly IDocumentManager _documentManager;

    public EditDialogService(
        IDialogService dialogService,
        IDrawingExportService drawingExportService,
        IConstructionColorService constructionColorService,
        IOrderColorService orderColorService,
        ICadPreviewEditorFactory previewEditorFactory,
        IRecentColorsService recentColorsService,
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        IDocumentManager documentManager)
    {
        _dialogService = dialogService;
        _drawingExportService = drawingExportService;
        _constructionColorService = constructionColorService;
        _orderColorService = orderColorService;
        _previewEditorFactory = previewEditorFactory;
        _recentColorsService = recentColorsService;
        _workspace = workspace;
        _cadEditor = cadEditor;
        _documentManager = documentManager;
    }

    public async Task ShowCopyDrawingDialogAsync(bool includeDimensionLines, CancellationToken cancellationToken = default)
    {
        var viewModel = new CopyDrawingDialogViewModel(_cadEditor, _drawingExportService, includeDimensionLines);
        if (await _dialogService.ShowCopyDrawingDialogAsync(viewModel, cancellationToken))
        {
            await viewModel.CopyAsync(cancellationToken);
        }
    }

    public async Task ShowConstructionColorDialogAsync(CancellationToken cancellationToken = default)
    {
        var construction = _workspace.Order.SelectedConstruction ?? _workspace.Order.EnsureDefaultConstruction();
        var viewModel = new ConstructionColorDialogViewModel(
            _workspace,
            _cadEditor,
            _constructionColorService,
            _previewEditorFactory,
            _recentColorsService,
            construction);

        if (await _dialogService.ShowConstructionColorDialogAsync(viewModel, cancellationToken))
        {
            viewModel.Commit();
            _documentManager.MarkModified();
        }
    }

    public async Task ShowOrderColorDialogAsync(CancellationToken cancellationToken = default)
    {
        var viewModel = new OrderColorDialogViewModel(
            _workspace,
            _cadEditor,
            _orderColorService,
            _previewEditorFactory);

        if (await _dialogService.ShowOrderColorDialogAsync(viewModel, cancellationToken))
        {
            viewModel.Commit();
            _documentManager.MarkModified();
        }
    }
}
