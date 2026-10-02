using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool TryAddConstructionToStandardFromTool()
    {
        if (!HasCommittedConstruction || FrameRoot is null)
        {
            StatusText = "Нет конструкции для сохранения";
            return false;
        }

        _ = AddConstructionToStandardAsync();
        return true;
    }

    private bool TryInsertConstructionFromStandardFromTool()
    {
        _ = InsertConstructionFromStandardAsync();
        return true;
    }

    internal string? TryCaptureConstructionGeometryJson()
    {
        if (!HasCommittedConstruction)
        {
            return null;
        }

        return ConstructionTemplateSerializer.Serialize(CaptureConstructionMemento());
    }

    private async Task AddConstructionToStandardAsync()
    {
        try
        {
            var defaultName = $"Конструкция {_templateStore.Templates.Count + 1}";
            var name = await _dialogService.ShowTemplateNameInputAsync(defaultName);
            if (string.IsNullOrWhiteSpace(name))
            {
                StatusText = "Сохранение в типовые отменено";
                return;
            }

            var memento = CaptureConstructionMemento();
            var frame = CanvasElements.FirstOrDefault();
            var template = new CadDrawingTemplate
            {
                Name = name,
                WidthMm = FrameRoot?.Width ?? Length,
                HeightMm = FrameRoot?.Height ?? Height,
                IsFakeFrame = frame?.Kind is GeometryElementKind.DummyFrame or GeometryElementKind.FictitiousFrame,
                GeometryJson = ConstructionTemplateSerializer.Serialize(memento)
            };
            _templateStore.Templates.Add(template);
            StatusText = $"Конструкция сохранена в типовые: {name}";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("AddConstructionToStandardAsync failed", ex);
            StatusText = "Не удалось сохранить в типовые конструкции";
        }
    }

    private async Task InsertConstructionFromStandardAsync()
    {
        try
        {
            var template = await _dialogService.ShowTemplatePickerAsync(_templateStore, this);
            if (template is null)
            {
                StatusText = "Вставка из типовых отменена";
                return;
            }

            if (!template.HasFullGeometry)
            {
                ApplyDrawingTemplate(template);
                StatusText = $"Применён шаблон: {template.Name}";
                return;
            }

            var memento = ConstructionTemplateSerializer.TryDeserialize(template.GeometryJson);
            if (memento is null)
            {
                StatusText = "Не удалось прочитать геометрию шаблона";
                return;
            }

            var imported = ConstructionCloneHelper.DeepClone(memento);
            if (!RecordPlacement(
                    $"Вставлено из типовых: {template.Name}",
                    () => ApplyImportedConstructionMemento(imported)))
            {
                StatusText = "Не удалось вставить типовую конструкцию";
                return;
            }

            RefreshAfterPlacementHistoryChange();
            StatusText = $"Вставлено из типовых: {template.Name}";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("InsertConstructionFromStandardAsync failed", ex);
            StatusText = "Не удалось вставить из типовых конструкций";
        }
    }
}
