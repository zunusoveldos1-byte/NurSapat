using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Services;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private const double PasteOffsetMm = 80;

    private ConstructionMemento? _constructionClipboard;

    private bool TryCopyConstructionFromTool()
    {
        if (!HasCommittedConstruction || FrameRoot is null)
        {
            StatusText = "Нет конструкции для копирования";
            return false;
        }

        _constructionClipboard = ConstructionCloneHelper.DeepClone(CaptureConstructionMemento());
        StatusText = "Конструкция скопирована";
        return true;
    }

    private bool TryPasteConstructionFromTool()
    {
        if (_constructionClipboard is null)
        {
            StatusText = "Буфер копирования пуст";
            return false;
        }

        var pasted = ConstructionCloneHelper.DeepClone(_constructionClipboard, PasteOffsetMm, PasteOffsetMm);
        if (!RecordPlacement("Конструкция вставлена", () => ApplyImportedConstructionMemento(pasted)))
        {
            StatusText = "Не удалось вставить конструкцию";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        StatusText = "Конструкция вставлена";
        return true;
    }

    private bool ApplyImportedConstructionMemento(ConstructionMemento memento)
    {
        ApplyConstructionMemento(memento);
        FinalizeImportedConstruction();
        return FrameRoot is not null || memento.Elements.Count > 0;
    }

    private void FinalizeImportedConstruction()
    {
        PurgeOrphanedSashes();
        SyncExtremeImpostCanvasElements();
        SyncSillCanvasElement();
        SyncFillingsToFrameLayout();
        RecalculateFrameGeometry();
        SyncFrameGeometryToDocument();
        if (FrameRoot is not null)
        {
            ApplyFrameSelection(FrameRoot);
        }

        NotifyFrameSelectionProperties();
        RequestSceneRefresh();
    }
}
