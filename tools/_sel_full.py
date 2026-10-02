# -*- coding: utf-8 -*-
from pathlib import Path

# Dump ConstructionSelectionRenderer fully and search for selection graphics
for path in [
    "NurSapat.Cad.UI/Rendering/ConstructionSelectionRenderer.cs",
    "NurSapat.Cad.UI/Rendering/ConstructionSelectionHelper.cs",
    "NurSapat.Cad.UI/Rendering/FrameVolumeRenderer.cs",
]:
    text = Path(path).read_text(encoding="utf-8-sig")
    print(f"\n@@@@ {path} len={len(text.splitlines())}")
    for i,l in enumerate(text.splitlines(),1):
        print(f"{i}|{l.encode('unicode_escape').decode()[:200]}")

# Search grips/handles across UI
for path in Path("NurSapat.Cad.UI").rglob("*.cs"):
    t = path.read_text(encoding="utf-8-sig", errors="replace")
    for i,l in enumerate(t.splitlines(),1):
        if any(k in l for k in ["Grip", "grip", "HandleRect", "DrawKnob", "knob", "SelectionBox", "DrawSelected", "overlay", "highlight"]):
            print(f"{path}:{i}:{l.strip()[:140].encode('unicode_escape').decode()}")
