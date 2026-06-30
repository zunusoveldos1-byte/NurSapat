using NurSapat.Cad.UI.Models;

namespace NurSapat.Cad.UI.Services;

public sealed record SizeInputResult(double Width, double Height, double X, double Y);

public interface ICadDialogService
{
    Task<SizeInputResult?> ShowSizeInputAsync(
        double initialWidth,
        double initialHeight,
        double initialX = 0,
        double initialY = 0);

    Task<int?> ShowImpostCountAsync(int initialCount = 1);

    Task<CadDrawingTemplate?> ShowTemplatePickerAsync(CadTemplateStore store, ViewModels.CadEditorViewModel editor);

    Task<bool> ShowConfirmationAsync(string title, string message);
}
