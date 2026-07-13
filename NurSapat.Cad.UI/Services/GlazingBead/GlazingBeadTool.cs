using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Services.GlazingBead;

/// <summary>Инструмент установки штапика в световые проёмы (без остекления).</summary>
internal sealed class GlazingBeadTool
{
    private readonly GlazingBeadCommand _command = new();

    public bool TryPlace(CadEditorViewModel editor, CadPoint worldPoint, bool applyToAllOpenings)
    {
        if (editor.FrameRoot is null || !editor.HasRealFrameOnCanvas)
        {
            editor.StatusText = "Сначала создайте коробку (рама)";
            return false;
        }

        var options = editor.ResolveGlazingBeadPlacementOptions();
        IReadOnlyList<LightOpeningCandidate> openings;

        if (applyToAllOpenings)
        {
            openings = OpeningDetector.FindFreeLightOpenings(editor);
            if (openings.Count == 0)
            {
                editor.StatusText = "Нет свободных световых проёмов для штапика";
                return false;
            }
        }
        else
        {
            if (OpeningDetector.FindBeadAtPoint(editor, worldPoint) is not null)
            {
                editor.StatusText = "Штапик уже установлен.";
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
                editor.StatusText = "Штапик уже установлен.";
                return false;
            }

            openings = [opening];
        }

        if (!_command.TryPrepare(editor, openings, options, out var batch))
        {
            editor.StatusText = "Не удалось построить штапик";
            return false;
        }

        var message = applyToAllOpenings
            ? $"Штапик установлен ({openings.Count} проёмов)"
            : "Штапик установлен";

        if (!editor.RecordGlazingBeadPlacement(message, batch))
        {
            editor.StatusText = "Не удалось установить штапик";
            return false;
        }

        editor.StatusText = message;
        editor.RequestSceneRefresh();
        return true;
    }
}
