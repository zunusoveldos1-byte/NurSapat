using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Services.GlazingBead;

/// <summary>
/// Инструмент установки штапика (BeadSystem) или остекления со штапиком (GlassWithBead)
/// в свободные световые проёмы.
/// </summary>
internal sealed class GlazingBeadTool
{
    private readonly GlazingBeadCommand _command = new();

    public bool TryPlace(
        CadEditorViewModel editor,
        CadPoint worldPoint,
        bool applyToAllOpenings,
        bool includeGlass = false)
    {
        if (editor.FrameRoot is null || !ConstructionHostResolver.HasConstructionHostOnCanvas(editor))
        {
            editor.StatusText = "Сначала создайте коробку или переходной профиль";
            return false;
        }

        var options = editor.ResolveGlazingBeadPlacementOptions(includeGlass);
        IReadOnlyList<LightOpeningCandidate> openings;

        if (applyToAllOpenings)
        {
            openings = OpeningDetector.FindFreeLightOpenings(editor);
            if (openings.Count == 0)
            {
                editor.StatusText = includeGlass
                    ? "Нет свободных световых проёмов для остекления со штапиком"
                    : "Нет свободных световых проёмов для штапика";
                return false;
            }
        }
        else
        {
            if (OpeningDetector.FindBeadAtPoint(editor, worldPoint) is not null)
            {
                editor.StatusText = includeGlass
                    ? "Остекление со штапиком уже установлено."
                    : "Штапик уже установлен.";
                return false;
            }

            var opening = OpeningDetector.FindOpeningAtPoint(editor, worldPoint);
            if (opening is null)
            {
                editor.StatusText = "Кликните внутри свободного светового проёма";
                return false;
            }

            if (OpeningDetector.FindBeadInLeaf(editor, opening.Leaf) is not null)
            {
                editor.StatusText = includeGlass
                    ? "Остекление со штапиком уже установлено."
                    : "Штапик уже установлен.";
                return false;
            }

            openings = [opening];
        }

        if (!_command.TryPrepare(editor, openings, options, out var batch))
        {
            editor.StatusText = includeGlass
                ? "Не удалось построить остекление со штапиком"
                : "Не удалось построить штапик";
            return false;
        }

        var message = BuildStatusMessage(includeGlass, applyToAllOpenings, openings.Count);
        if (!editor.RecordGlazingBeadPlacement(message, batch))
        {
            editor.StatusText = includeGlass
                ? "Не удалось установить остекление со штапиком"
                : "Не удалось установить штапик";
            return false;
        }

        editor.StatusText = message;
        editor.RequestSceneRefresh();
        return true;
    }

    private static string BuildStatusMessage(bool includeGlass, bool applyToAllOpenings, int count)
    {
        if (includeGlass)
        {
            return applyToAllOpenings
                ? $"Остекление со штапиком установлено ({count} проёмов)"
                : "Остекление со штапиком установлено";
        }

        return applyToAllOpenings
            ? $"Штапик установлен ({count} проёмов)"
            : "Штапик установлен";
    }
}
