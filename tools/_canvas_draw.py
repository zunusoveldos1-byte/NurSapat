# -*- coding: utf-8 -*-
from pathlib import Path
lines = Path("NurSapat.Cad.UI/Controls/CadCanvasControl.cs").read_text(encoding="utf-8-sig").splitlines()
for i in range(230, 370):
    print(f"{i+1}|{lines[i].encode('unicode_escape').decode()}")
print("--- FrameVolumeRenderer handles ---")
lines2 = Path("NurSapat.Cad.UI/Rendering/FrameVolumeRenderer.cs").read_text(encoding="utf-8-sig").splitlines()
for i in range(1, 80):
    print(f"{i}|{lines2[i-1].encode('unicode_escape').decode()}")
print("--- around DrawArcHandle ---")
for i in range(260, 320):
    print(f"{i}|{lines2[i-1].encode('unicode_escape').decode()}")
# search SelectionTool for handles
for path in Path("NurSapat.Cad.UI").rglob("*Selection*.cs"):
    print("PATH", path)
    t=path.read_text(encoding="utf-8-sig")
    for i,l in enumerate(t.splitlines(),1):
        if "Handle" in l or "resize" in l.lower() or "corner" in l.lower() or "0078" in l:
            print(f"{i}:{l.strip()[:160].encode('unicode_escape').decode()}")
