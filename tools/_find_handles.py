# -*- coding: utf-8 -*-
from pathlib import Path

needles = ["ResizeHandle", "resize handle", "DrawSelection", "SelectedRectangle", "HandleSize", "corner handle", "SelectionHandles", "DrawHandles", "Blue", "0078D7", "DashStyle", "DummySelection"]
dirs = [Path("NurSapat.Cad.UI")]
for d in dirs:
    for path in d.rglob("*.cs"):
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        hits = []
        for i,line in enumerate(text.splitlines(),1):
            for n in needles:
                if n.lower() in line.lower():
                    hits.append(f"{i}:{line.strip()[:160].encode('unicode_escape').decode()}")
                    break
        if hits:
            print(f"\n## {path} ({len(hits)})")
            for h in hits[:40]:
                print(h)

# CadSelectableKind enum
for path in Path("NurSapat.Cad.UI").rglob("*CadSelectable*"):
    print("found", path)
