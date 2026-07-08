# -*- coding: utf-8 -*-
from pathlib import Path
import re

def check(path, *needles, must_not=()):
    t = Path(path).read_text(encoding="utf-8-sig")
    print(f"\n== {path}")
    for n in needles:
        print(f"  HAS {n!r}: {n in t}")
    for n in must_not:
        print(f"  NOT {n!r}: {n not in t}")
    return t

# A encoding
t = check("NurSapat.Cad.UI/Services/CadToolbarLayout.cs",
    "\u0418\u043c\u043f\u043e\u0441\u0442",
    "\u0421\u041e\u0417\u0414\u0410\u041d\u0418\u0415",
    "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f",
    "\u0414\u0415\u041a\u041e\u0420\u0418\u0420\u041e\u0412\u0410\u041d\u0418\u0415",
    must_not=("frame-fictitious", "\ufffd"))
data = Path("NurSapat.Cad.UI/Services/CadToolbarLayout.cs").read_bytes()
print("  BOM", data[:3]==bytes([0xEF,0xBB,0xBF]))
# lone 0x98 not as part of C2 98
# check mojibake: Cyrillic A + U+0098
print("  U+0098 char", "\u0098" in t)
print("  Impost count", t.count("\u0418\u043c\u043f\u043e\u0441\u0442"))

# B
check("NurSapat.App/ViewModels/OrderDrawingViewModel.cs",
    'Id == "dummy-frame"',
    must_not=("fictitious-frame", "\u0413\u0430\u0431\u0430\u0440\u0438\u0442\u043d\u0430\u044f \u0440\u0430\u043c\u043a\u0430"))
check("NurSapat.Core/Models/Orders/FictitiousFrame.cs",
    "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 (\u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f)",
    must_not=("\u0413\u0430\u0431\u0430\u0440\u0438\u0442\u043d\u0430\u044f \u0440\u0430\u043c\u043a\u0430 (\u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f)",))

# C
vm = check("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs",
    "TryCommitRealFrameInLeaf",
    "leaf.IsLeaf",
    must_not=("ResolveInnerInset(leaf.Width, leaf.Height, FrameThickness)",))
# ensure inset block gone inside TryCommitRealFrameInLeaf
m = re.search(r"private bool TryCommitRealFrameInLeaf\(FrameNode leaf\)([\s\S]*?)\n    private ", vm)
if m:
    body = m.group(1)
    print("  inset in TryCommitRealFrameInLeaf:", "IsFictitiousContainer == true" in body and "ResolveInnerInset" in body)
    print("  uses leaf.Width exact:", "RoundMm(leaf.Width)" in body)

# nested no longer requires transition
m2 = re.search(r"private bool TryCommitNestedRealFrameAt[\s\S]*?\n    private bool ShouldPreserve", vm)
if m2:
    print("  nested requires transition:", "FindTransitionInLeaf" in m2.group(0))
    print("  nested uses IsLeaf:", "leaf.IsLeaf" in m2.group(0))

# D
print("  CanDelete Frame dummy:", "CadSelectableKind.Frame && FrameRoot?.IsDummy" in vm or "CadSelectableKind.Frame && FrameRoot?.IsDummy == true" in vm)
print("  TryDelete Frame case:", "case CadSelectableKind.Frame when FrameRoot?.IsDummy" in vm)

# E
cc = check("NurSapat.Cad.UI/Controls/CadCanvasControl.cs",
    "DummySelectedFramePen", "DrawResizeHandles", "DrawSelectedRectangleOverlay")
sel = check("NurSapat.Cad.UI/Rendering/ConstructionSelectionRenderer.cs",
    "DrawDummyResizeHandles", "DummySelectionPen")

# F
ex = check("NurSapat.App/Services/Export/ExportService.cs",
    "DummyFrame.IsDummyElement", "FictitiousFrame.IsFictitiousElement")

# CanDelete syntax snippet
for i,l in enumerate(vm.splitlines(),1):
    if "CanDeleteConstructionItem" in l or (2300 <= i <= 2320 and "CadSelectableKind" in l) or "case CadSelectableKind.Frame" in l:
        if 2295 <= i <= 2360:
            print(f"VM{i}|{l.encode('unicode_escape').decode()[:180]}")

# grep fictitious-frame across repo
print("\n== fictitious-frame remaining ==")
for path in Path(".").rglob("*.cs"):
    if str(path).startswith("tools"):
        continue
    try:
        txt = path.read_text(encoding="utf-8-sig")
    except Exception:
        continue
    if "fictitious-frame" in txt:
        print(path)
