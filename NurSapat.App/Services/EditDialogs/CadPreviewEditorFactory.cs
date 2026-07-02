using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.App.Services.EditDialogs;

public interface ICadPreviewEditorFactory
{
    CadEditorViewModel CreatePreviewEditor();
}

public sealed class CadPreviewEditorFactory : ICadPreviewEditorFactory
{
    private readonly ICadDialogService _dialogService;
    private readonly CadTemplateStore _templateStore;

    public CadPreviewEditorFactory(ICadDialogService dialogService, CadTemplateStore templateStore)
    {
        _dialogService = dialogService;
        _templateStore = templateStore;
    }

    public CadEditorViewModel CreatePreviewEditor() =>
        new(_dialogService, _templateStore, materialRepository: null);
}
