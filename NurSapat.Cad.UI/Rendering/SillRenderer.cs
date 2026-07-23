using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class SillRenderer
{
    private static readonly IBrush DefaultSillFill = new SolidColorBrush(Color.Parse(SillElement.DefaultColorHex));
    private static readonly IPen SillPen = new Pen(new SolidColorBrush(Color.Parse("#90A4AE")), 1.2);

    public static void DrawAttachedSill(DrawingContext context, CadEditorViewModel editor, FrameNode? root)
    {
        if (editor.Viewport is null || root?.GetConstructionRoot() is not { } constructionRoot)
        {
            return;
        }

        if (constructionRoot.AttachedSill is not { } sill)
        {
            return;
        }

        var thickness = sill.ThicknessMm > 0 ? sill.ThicknessMm : SillElement.StandardThicknessMm;
        var leftOverhang = sill.LeftOverhangMm > 0 ? sill.LeftOverhangMm : SillElement.DefaultSideOverhangMm;
        var rightOverhang = sill.RightOverhangMm > 0 ? sill.RightOverhangMm : SillElement.DefaultSideOverhangMm;
        var width = sill.WidthMm > 0
            ? sill.WidthMm
            : sill.ResolveWidthForFrame(constructionRoot.Width);
        var left = constructionRoot.X - leftOverhang;
        var bottom = constructionRoot.Y - thickness;

        var fill = ResolveFillBrush(sill);
        var topLeft = editor.Viewport.WorldToScreen(new CadPoint(left, constructionRoot.Y));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(left + width, bottom));
        var rect = new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(fill, SillPen, rect);
    }

    public static double ResolveDefaultWidth(FrameNode root)
    {
        var overhangLeft = root.AttachedSill?.LeftOverhangMm ?? SillElement.DefaultSideOverhangMm;
        var overhangRight = root.AttachedSill?.RightOverhangMm ?? SillElement.DefaultSideOverhangMm;
        return root.Width + overhangLeft + overhangRight;
    }

    public static bool TryHitSill(CadEditorViewModel editor, CadPoint worldPoint)
    {
        if (editor.FrameRoot?.GetConstructionRoot() is not { AttachedSill: { } sill } root)
        {
            return false;
        }

        var thickness = sill.ThicknessMm > 0 ? sill.ThicknessMm : SillElement.StandardThicknessMm;
        var leftOverhang = sill.LeftOverhangMm > 0 ? sill.LeftOverhangMm : SillElement.DefaultSideOverhangMm;
        var rightOverhang = sill.RightOverhangMm > 0 ? sill.RightOverhangMm : SillElement.DefaultSideOverhangMm;
        var width = sill.WidthMm > 0 ? sill.WidthMm : sill.ResolveWidthForFrame(root.Width);
        var left = root.X - leftOverhang;
        var bottom = root.Y - thickness;
        const double tolerance = 8;

        return worldPoint.X >= left - tolerance
               && worldPoint.X <= left + width + tolerance
               && worldPoint.Y >= bottom - tolerance
               && worldPoint.Y <= root.Y + tolerance;
    }

    public static bool TryHitBottomFrameProfile(CadEditorViewModel editor, CadPoint worldPoint)
    {
        if (editor.FrameRoot?.GetConstructionRoot() is not FrameNode root)
        {
            return false;
        }

        if (!editor.CanAttachSillToFrame())
        {
            return false;
        }

        var tolerance = Math.Max(editor.FrameThickness, 40);
        var outsideDepth = SillElement.StandardThicknessMm + 30;
        var sideTolerance = SillElement.DefaultSideOverhangMm + tolerance;

        if (worldPoint.X < root.X - sideTolerance || worldPoint.X > root.X + root.Width + sideTolerance)
        {
            return false;
        }

        return worldPoint.Y >= root.Y - outsideDepth
               && worldPoint.Y <= root.Y + editor.FrameThickness + tolerance;
    }

    public static string ResolveRenderColor(SillElement sill)
    {
        if (!string.IsNullOrWhiteSpace(sill.Color))
        {
            return sill.Color;
        }

        return ResolveRenderColorForMaterial(sill.MaterialId);
    }

    public static string ResolveRenderColorForMaterial(Guid? materialId) =>
        materialId switch
        {
            null => SillElement.DefaultColorHex,
            var id when id == SillElement.DefaultMaterialId => SillElement.DefaultColorHex,
            var id when id == Guid.Parse("b1000082-0000-4000-8000-000000000082") => "#D4C4A8",
            _ => SillElement.DefaultColorHex
        };

    private static IBrush ResolveFillBrush(SillElement sill)
    {
        try
        {
            return new SolidColorBrush(Color.Parse(ResolveRenderColor(sill)));
        }
        catch
        {
            return DefaultSillFill;
        }
    }
}
