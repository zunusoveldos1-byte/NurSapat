using NurSapat.App.Services;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services.Documents;

public interface IPrintService
{
    Task ShowPrintDialogAsync(CancellationToken cancellationToken = default);

    Task ShowPrintSettingsDialogAsync(CancellationToken cancellationToken = default);
}

public sealed class PrintService : IPrintService
{
    private readonly IDialogService _dialogService;
    private readonly IPrintSettingsService _printSettingsService;
    private readonly IDocumentManager _documentManager;
    private readonly CadEditorViewModel _cadEditor;

    public PrintService(
        IDialogService dialogService,
        IPrintSettingsService printSettingsService,
        IDocumentManager documentManager,
        CadEditorViewModel cadEditor)
    {
        _dialogService = dialogService;
        _printSettingsService = printSettingsService;
        _documentManager = documentManager;
        _cadEditor = cadEditor;
    }

    public async Task ShowPrintDialogAsync(CancellationToken cancellationToken = default)
    {
        var viewModel = new PrintDialogViewModel(_printSettingsService.CloneCurrent(), _cadEditor);
        if (await _dialogService.ShowPrintDialogAsync(viewModel, cancellationToken))
        {
            _printSettingsService.Load(viewModel.ToSettings());
            _documentManager.MarkModified();
            await viewModel.PrintAsync(cancellationToken);
        }
    }

    public async Task ShowPrintSettingsDialogAsync(CancellationToken cancellationToken = default)
    {
        var viewModel = new PrintSettingsViewModel(_printSettingsService.CloneCurrent());
        if (await _dialogService.ShowPrintSettingsDialogAsync(viewModel, cancellationToken))
        {
            _printSettingsService.Load(viewModel.ToSettings());
            _documentManager.MarkModified();
        }
    }
}
