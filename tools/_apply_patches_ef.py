# -*- coding: utf-8 -*-
from pathlib import Path
import re

ROOT = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")

def read(p):
    return (ROOT / p).read_text(encoding="utf-8-sig")

def write(p, text):
    (ROOT / p).write_text(text, encoding="utf-8-sig")
    print("wrote", p)

# ---------- E: CadCanvasControl ----------
cc_path = "NurSapat.Cad.UI/Controls/CadCanvasControl.cs"
cc = read(cc_path)

old_pens = """    private static readonly Pen SelectedFramePen = new(CadStrokeBrush, 2);
    private static readonly Pen FramePreviewPen = new(
        new SolidColorBrush(Color.Parse("#0078D7")),
        1.5,
        new DashStyle(new double[] { 4, 3 }, 0));"""

new_pens = """    private static readonly Pen SelectedFramePen = new(CadStrokeBrush, 2);
    private static readonly Pen DummySelectedFramePen = new(
        new SolidColorBrush(FrameVolumeRenderer.DummySelectionHighlightColor),
        2,
        new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IBrush DummyResizeHandleFill = new SolidColorBrush(Color.FromArgb(200, 0x00, 0x78, 0xD7));
    private static readonly Pen DummyResizeHandlePen = new(
        new SolidColorBrush(FrameVolumeRenderer.DummySelectionHighlightColor),
        1.5,
        new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly IBrush ResizeHandleFill = new SolidColorBrush(Color.FromArgb(200, 0xAE, 0xAE, 0xAE));
    private static readonly Pen ResizeHandlePen = new(CadStrokeBrush, 1.5);
    private static readonly Pen FramePreviewPen = new(
        new SolidColorBrush(Color.Parse("#0078D7")),
        1.5,
        new DashStyle(new double[] { 4, 3 }, 0));"""

if old_pens not in cc and old_pens.replace("\n","\r\n") in cc:
    cc = cc.replace(old_pens.replace("\n","\r\n"), new_pens.replace("\n","\r\n"))
elif old_pens in cc:
    cc = cc.replace(old_pens, new_pens)
else:
    raise SystemExit("pens block not found")

old_highlight = """        if (!suppressCommittedFrame && constructionSelection.Count > 0)
        {
            ConstructionSelectionRenderer.DrawHighlights(context, Editor, constructionSelection);
        }

        if (Editor.SashDragPreview is { } sashPreview)"""

new_highlight = """        if (!suppressCommittedFrame && constructionSelection.Count > 0)
        {
            ConstructionSelectionRenderer.DrawHighlights(context, Editor, constructionSelection);
        }

        if (!suppressCommittedFrame)
        {
            DrawSelectedRectangleOverlay(context);
        }

        if (Editor.SashDragPreview is { } sashPreview)"""

if old_highlight not in cc and old_highlight.replace("\n","\r\n") in cc:
    cc = cc.replace(old_highlight.replace("\n","\r\n"), new_highlight.replace("\n","\r\n"))
elif old_highlight in cc:
    cc = cc.replace(old_highlight, new_highlight)
else:
    raise SystemExit("highlight insert point not found")

old_marquee = """        if (Editor.ActiveTool is SelectionTool selectionTool)
        {
            if (selectionTool.SelectionRect is CadRect selectionRect)
            {
                var fill = new SolidColorBrush(Color.FromArgb(48, 255, 4, 4));
                DrawRect(context, selectionRect, fill, SelectedFramePen);
            }
        }"""

new_marquee = """        if (Editor.ActiveTool is SelectionTool selectionTool)
        {
            if (selectionTool.SelectionRect is CadRect selectionRect)
            {
                var useDummyStyle = Editor.FrameRoot?.IsDummy == true || Editor.HasFakeFrameOnCanvas;
                var fill = useDummyStyle
                    ? new SolidColorBrush(Color.FromArgb(48, 0x00, 0x78, 0xD7))
                    : new SolidColorBrush(Color.FromArgb(48, 255, 4, 4));
                DrawRect(context, selectionRect, fill, useDummyStyle ? DummySelectedFramePen : SelectedFramePen);
            }
        }"""

if old_marquee not in cc and old_marquee.replace("\n","\r\n") in cc:
    cc = cc.replace(old_marquee.replace("\n","\r\n"), new_marquee.replace("\n","\r\n"))
elif old_marquee in cc:
    cc = cc.replace(old_marquee, new_marquee)
else:
    raise SystemExit("marquee block not found")

old_drawrect = """    private void DrawRect(DrawingContext context, CadRect rect, IBrush? fill, Pen pen)
    {"""

new_methods = """    private void DrawSelectedRectangleOverlay(DrawingContext context)
    {
        if (Editor?.SelectedRectangle is not CadRect selected || selected.Width <= 0 || selected.Height <= 0)
        {
            return;
        }

        var useDummyStyle = Editor.FrameRoot?.IsDummy == true || Editor.HasFakeFrameOnCanvas;
        var outlinePen = useDummyStyle ? DummySelectedFramePen : SelectedFramePen;
        DrawRect(context, selected, null, outlinePen);
        DrawResizeHandles(context, selected, useDummyStyle);
    }

    private void DrawResizeHandles(DrawingContext context, CadRect rect, bool useDummyStyle)
    {
        if (Editor?.Viewport is null)
        {
            return;
        }

        var fill = useDummyStyle ? DummyResizeHandleFill : ResizeHandleFill;
        var pen = useDummyStyle ? DummyResizeHandlePen : ResizeHandlePen;
        const double handleHalfPx = 4.0;

        CadPoint[] corners =
        [
            rect.TopLeft,
            rect.TopRight,
            rect.BottomLeft,
            rect.BottomRight
        ];

        foreach (var corner in corners)
        {
            var screen = Editor.Viewport.WorldToScreen(corner);
            var handleRect = new Rect(
                screen.X - handleHalfPx,
                screen.Y - handleHalfPx,
                handleHalfPx * 2,
                handleHalfPx * 2);
            context.DrawRectangle(fill, pen, handleRect);
        }
    }

    private void DrawRect(DrawingContext context, CadRect rect, IBrush? fill, Pen pen)
    {"""

if old_drawrect not in cc and old_drawrect.replace("\n","\r\n") in cc:
    cc = cc.replace(old_drawrect.replace("\n","\r\n"), new_methods.replace("\n","\r\n"))
elif old_drawrect in cc:
    cc = cc.replace(old_drawrect, new_methods)
else:
    raise SystemExit("DrawRect insert not found")

write(cc_path, cc)

# ConstructionSelectionRenderer dummy handles
sel_path = "NurSapat.Cad.UI/Rendering/ConstructionSelectionRenderer.cs"
sel = read(sel_path)
old_dummy = """        if (frame.IsDummy)
        {
            DrawWorldRectOutline(
                context,
                editor,
                new CadRect(frame.X, frame.Y + frame.Height, frame.Width, frame.Height),
                DummySelectionPen);
            return;
        }"""

new_dummy = """        if (frame.IsDummy)
        {
            var bounds = new CadRect(frame.X, frame.Y + frame.Height, frame.Width, frame.Height);
            DrawWorldRectOutline(
                context,
                editor,
                bounds,
                DummySelectionPen);
            DrawDummyResizeHandles(context, editor, bounds);
            return;
        }"""

if old_dummy not in sel and old_dummy.replace("\n","\r\n") in sel:
    sel = sel.replace(old_dummy.replace("\n","\r\n"), new_dummy.replace("\n","\r\n"))
elif old_dummy in sel:
    sel = sel.replace(old_dummy, new_dummy)
else:
    raise SystemExit("dummy highlight block not found")

marker = "    private static void DrawWorldRectOutline("
helper = """    private static void DrawDummyResizeHandles(
        DrawingContext context,
        CadEditorViewModel editor,
        CadRect bounds)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        const double handleHalfPx = 4.0;
        var fill = new SolidColorBrush(Color.FromArgb(200, 0x00, 0x78, 0xD7));
        CadPoint[] corners =
        [
            bounds.TopLeft,
            bounds.TopRight,
            bounds.BottomLeft,
            bounds.BottomRight
        ];

        foreach (var corner in corners)
        {
            var screen = editor.Viewport.WorldToScreen(corner);
            var handleRect = new Rect(
                screen.X - handleHalfPx,
                screen.Y - handleHalfPx,
                handleHalfPx * 2,
                handleHalfPx * 2);
            context.DrawRectangle(fill, DummySelectionPen, handleRect);
        }
    }

"""

if "DrawDummyResizeHandles" not in sel:
    if marker not in sel and marker.replace("\n","\r\n") in sel:
        sel = sel.replace(marker.replace("\n","\r\n"), helper.replace("\n","\r\n") + marker.replace("\n","\r\n"))
    elif marker in sel:
        sel = sel.replace(marker, helper + marker)
    else:
        raise SystemExit("DrawWorldRectOutline marker not found")

write(sel_path, sel)

# ---------- F: ExportService ----------
ex_path = "NurSapat.App/Services/Export/ExportService.cs"
ex = read(ex_path)
old_billable = """        var billableElements = construction.Geometry.Elements
            .Where(element => !FictitiousFrame.IsFictitiousElement(element))"""

new_billable = """        var billableElements = construction.Geometry.Elements
            .Where(element => !FictitiousFrame.IsFictitiousElement(element)
                              && !DummyFrame.IsDummyElement(element))"""

if old_billable in ex:
    ex = ex.replace(old_billable, new_billable)
    print("export: added DummyFrame.IsDummyElement filter")
elif old_billable.replace("\n","\r\n") in ex:
    ex = ex.replace(old_billable.replace("\n","\r\n"), new_billable.replace("\n","\r\n"))
    print("export: added DummyFrame.IsDummyElement filter CRLF")
else:
    print("export: billable filter pattern not exact; leaving as-is")

write(ex_path, ex)
print("E/F done")
