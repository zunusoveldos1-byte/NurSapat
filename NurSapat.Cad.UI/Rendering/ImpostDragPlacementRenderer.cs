using Avalonia.Media;
using NurSapat.Cad.UI.Services.ImpostDragPlacement;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Слой предпросмотра размещения импоста: настоящий профиль (без пунктира и размеров).
/// </summary>
public static class ImpostDragPlacementRenderer
{
    public static void Draw(DrawingContext context, CadEditorViewModel editor)
    {
        var preview = editor.ImpostDragPlacementPreview;
        if (preview is null || editor.Viewport is null)
        {
            return;
        }

        FrameNodeRenderer.DrawDragPlacementPreview(context, editor, preview);
    }
}
