using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Calculations;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

public sealed record SizeInputResult(double Width, double Height, double X, double Y);

public sealed record VertexCoordinateDialogResult(double X, double Y);

public sealed record ImpostCountDialogResult(Guid TargetCellId, int Count);

public interface ICadDialogService
{
    Task<SizeInputResult?> ShowSizeInputAsync(
        double initialWidth,
        double initialHeight,
        double initialX = 0,
        double initialY = 0);

    Task<NewConstructionSizeInputResult?> ShowNewConstructionSizeInputAsync(NewConstructionOptions options);

    Task<ImpostCountDialogResult?> ShowImpostCountAsync(Guid targetCellId, int initialCount = 1);

    Task<DecorativeSpokeDialogResult?> ShowDecorativeSpokeDialogAsync(bool isArchedGlazing);

    Task<SillDialogResult?> ShowSillDialogAsync(double frameWidthMm, SillElement? existing = null);

    Task<CadDrawingTemplate?> ShowTemplatePickerAsync(CadTemplateStore store, CadEditorViewModel editor);

    Task<bool> ShowConfirmationAsync(string title, string message);

    Task<VertexCoordinateDialogResult?> ShowVertexCoordinateDialogAsync(
        double initialX,
        double initialY,
        string? title = null);

    Task<ObjectPropertiesDialogResult?> ShowObjectPropertiesDialogAsync(ObjectPropertiesDialogModel model);

    Task ShowConstructionDetailsDialogAsync(
        ConstructionVolumeSummary summary,
        IEnumerable<GeometryElement> elements);

    Task<double?> ShowRadiusInputAsync(double initialRadius);

    Task<string?> ShowTemplateNameInputAsync(string initialName);
}
