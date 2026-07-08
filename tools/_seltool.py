# -*- coding: utf-8 -*-
from pathlib import Path

sel = Path("NurSapat.Cad.Tools/Tools/SelectionTool.cs").read_text(encoding="utf-8-sig")
print("SelectionTool lines", len(sel.splitlines()))
for i,l in enumerate(sel.splitlines(),1):
    print(f"{i}|{l.encode('unicode_escape').decode()[:200]}")

# search DrawHandle / ResizeHandle in canvas and related
print("\n== ResizeHandle usages ==")
for path in Path(".").rglob("*.cs"):
    if "tools\\" in str(path).lower() and path.name.startswith("_"):
        continue
    try:
        t=path.read_text(encoding="utf-8-sig")
    except Exception:
        continue
    if "ResizeHandle" in t or "GetHandlePositions" in t:
        for i,l in enumerate(t.splitlines(),1):
            if "ResizeHandle" in l or "GetHandlePositions" in l or "DrawHandle" in l and "Sash" not in l:
                print(f"{path}:{i}:{l.strip()[:160].encode('unicode_escape').decode()}")
