# -*- coding: utf-8 -*-
from pathlib import Path
import subprocess

# Search transcripts? skip. Search for incomplete TODO related to dummy handles
for path in Path("NurSapat.Cad.UI").rglob("*.cs"):
    t = path.read_text(encoding="utf-8-sig", errors="replace")
    if "SelectedFramePen" in t or "FramePreviewPen" in t or "DummySelection" in t:
        print(path)

# Look in CadCanvas for any commented handle drawing
text = Path("NurSapat.Cad.UI/Controls/CadCanvasControl.cs").read_text(encoding="utf-8-sig")
# show around DrawRectangleDimensions - maybe draws handles?
for i,l in enumerate(text.splitlines(),1):
    if "Dimension" in l or "Selected" in l or "handle" in l.lower() or "0078" in l:
        print(f"{i}:{l.encode('unicode_escape').decode()[:180]}")

# Check if dimensions use SelectionDimensionPen and if dummy should use blue
print("\nDrawRectangleDimensions body:")
lines=text.splitlines()
for i in range(477, 510):
    print(f"{i+1}|{lines[i].encode('unicode_escape').decode()}")
