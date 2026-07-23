# -*- coding: utf-8 -*-
from pathlib import Path

needles = ["HandleSize", "handleSize", "Resize", "drawHandle", "DrawCorner", "corner", "8.0", "handleBrush", "SelectionHandle", "IsResizing", "BeginResize", "HitTestHandle"]
for path in Path("NurSapat.Cad.UI").rglob("*.cs"):
    t = path.read_text(encoding="utf-8-sig", errors="replace")
    hits=[]
    for i,l in enumerate(t.splitlines(),1):
        low=l.lower()
        if any(n.lower() in low for n in needles) and ("select" in low or "handle" in low or "resize" in low or "grip" in low or "corner" in low):
            hits.append(f"{i}:{l.strip()[:180].encode('unicode_escape').decode()}")
    if hits:
        print(f"\n## {path}")
        for h in hits[:30]:
            print(h)

# Also look CadSceneControl for selection drawing
for path in [Path("NurSapat.Cad.UI/Controls/CadSceneControl.cs"), Path("NurSapat.Cad.UI/Controls/CadCanvasControl.cs")]:
    if not path.exists():
        continue
    t=path.read_text(encoding="utf-8-sig")
    print(f"\n==== callers of ConstructionSelection / SelectedRectangle draw in {path.name}")
    for i,l in enumerate(t.splitlines(),1):
        if any(k in l for k in ["ConstructionSelection", "SelectedRectangle", "Document.Rectangles", "DrawRect", "FillRectangle", "Rectangles"]):
            print(f"{i}:{l.strip()[:160].encode('unicode_escape').decode()}")
