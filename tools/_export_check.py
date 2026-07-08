# -*- coding: utf-8 -*-
from pathlib import Path

# ExportService full material loops
text = Path("NurSapat.App/Services/Export/ExportService.cs").read_text(encoding="utf-8-sig")
print("lines", len(text.splitlines()))
for i,l in enumerate(text.splitlines(),1):
    if any(k in l for k in ["FrameNode", "IsDummy", "IsFictitious", "Elements", "Material", "foreach", "Walk", "Traverse", "Children"]):
        print(f"{i}|{l.encode('unicode_escape').decode()[:200]}")

# CadSelectableKind
p = Path("NurSapat.Cad.UI/Models/CadSelectableItem.cs")
print("\n", p)
for i,l in enumerate(p.read_text(encoding="utf-8-sig").splitlines(),1):
    print(f"{i}|{l.encode('unicode_escape').decode()}")

# Search fictitious-frame elsewhere in App
print("\n== other fictitious-frame / Gabarit ==")
for path in Path(".").rglob("*.cs"):
    if "tools" in str(path):
        continue
    try:
        t = path.read_text(encoding="utf-8-sig")
    except Exception:
        continue
    if "fictitious-frame" in t or "\u0413\u0430\u0431\u0430\u0440\u0438\u0442\u043d\u0430\u044f \u0440\u0430\u043c\u043a\u0430" in t:
        for i,l in enumerate(t.splitlines(),1):
            if "fictitious-frame" in l or "\u0413\u0430\u0431\u0430\u0440\u0438\u0442\u043d\u0430\u044f \u0440\u0430\u043c\u043a\u0430" in l:
                print(f"{path}:{i}:{l.strip()[:160].encode('unicode_escape').decode()}")
