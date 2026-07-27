using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Linq;

namespace NurSapat.Cad.UI.Services;

/// <summary>Трансформация профиля коробки (например, прямоугольник → треугольник).</summary>
public static class FrameProfileTransformService
{
    public static bool TryTransformRectangleToTriangleByRemovingTopRail(CadEditorViewModel editor)
    {
        var root = editor.FrameRoot;
        if (root is null || !FrameTriangleGeometry.IsRectangularFrameTransformableToTriangle(root))
        {
            return false;
        }

        root.Shape = ConstructionShape.Triangle;
        root.TopProfile = null;
        root.ClearSplits();

        SyncCommittedFrameElementShape(editor, ConstructionShape.Triangle);
        ClearRectangularDocumentEnvelope(editor);
        return true;
    }

    private static void ClearRectangularDocumentEnvelope(CadEditorViewModel editor)
    {
        editor.SelectedRectangle = null;
        while (editor.Document.Rectangles.Count > 0)
        {
            editor.Document.RemoveRectangle(editor.Document.Rectangles[0]);
        }
    }

    private static void SyncCommittedFrameElementShape(CadEditorViewModel editor, ConstructionShape shape)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame)
            {
                element.Shape = shape;
            }
        }
    }
}
