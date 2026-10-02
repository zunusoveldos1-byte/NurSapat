# -*- coding: utf-8 -*-
from pathlib import Path

# search draw of selected rectangle and handle squares
for path in Path("NurSapat.Cad.UI").rglob("*.cs"):
    t = path.read_text(encoding="utf-8-sig", errors="replace")
    for i,l in enumerate(t.splitlines(),1):
        if any(k in l for k in ["SelectedRectangle", "GetHandlePositions", "HandleSize", "handleSize", "DrawEllipse", "5.0", "selection handle"]):
            if "OnSelected" in l or "nameof" in l or "PropertyChanged" in l:
                continue
            print(f"{path}:{i}:{l.strip()[:150].encode('unicode_escape').decode()}")

# CadCanvas full for SelectedRectangle drawing and any Fill that looks like handles
text = Path("NurSapat.Cad.UI/Controls/CadCanvasControl.cs").read_text(encoding="utf-8-sig")
print("\nCadCanvas methods:")
for i,l in enumerate(text.splitlines(),1):
    if l.strip().startswith("private ") or l.strip().startswith("protected ") or l.strip().startswith("public "):
        if "(" in l:
            print(f"{i}|{l.encode('unicode_escape').decode()[:160]}")

# TryDeleteConstructionSelection start - does it handle Frame?
lines = Path("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs").read_text(encoding="utf-8-sig").splitlines()
for i in range(2235, 2296):
    print(f"{i+1}|{lines[i].encode('unicode_escape').decode()}")
