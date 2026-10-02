# -*- coding: utf-8 -*-
from pathlib import Path

# Search for drawing selection handles on frames - maybe "size all" cursor, HitTest edge
needles = ["TryHitResize", "ResizeEdge", "EdgeHandle", "selection handles", "DrawSelectionOverlay", "HandlePoints", "cornerPoints", "GetHandle", "HitHandle", "IsDraggingEdge", "edge hit"]
for path in Path(".").rglob("*.cs"):
    if "tools" in str(path) or ".git" in str(path):
        continue
    try:
        t = path.read_text(encoding="utf-8-sig")
    except Exception:
        continue
    for i,l in enumerate(t.splitlines(),1):
        for n in needles:
            if n.lower() in l.lower():
                print(f"{path}:{i}:{l.strip()[:160].encode('unicode_escape').decode()}")

# Also search CadCanvas for mouse resize of selected frame
for path in [Path("NurSapat.Cad.UI/Controls/CadCanvasControl.cs")]:
    t=path.read_text(encoding="utf-8-sig")
    for i,l in enumerate(t.splitlines(),1):
        if any(k in l for k in ["FillRectangle", "6,", "handle", "Drag", "Selected", "IsDummy", "FramePreviewPen"]):
            if "Property" in l or "Changed" in l:
                continue
            print(f"C:{i}:{l.strip()[:140].encode('unicode_escape').decode()}")

# Dimension pens / selection dimension for dummy?
print("\nSelectionDimension / Dummy in FrameVolume")
t=Path("NurSapat.Cad.UI/Rendering/FrameVolumeRenderer.cs").read_text(encoding="utf-8-sig")
for i,l in enumerate(t.splitlines(),1):
    if "Selection" in l or "Dummy" in l or "Handle" in l:
        print(f"{i}:{l.strip()[:160].encode('unicode_escape').decode()}")
