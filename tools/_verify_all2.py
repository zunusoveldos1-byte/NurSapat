# -*- coding: utf-8 -*-
from pathlib import Path
import re, sys
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

def check(path, *needles, must_not=()):
    t = Path(path).read_text(encoding="utf-8-sig")
    print(f"\n== {path}")
    for n in needles:
        ok = n in t
        print(f"  HAS ok={ok} len={len(n)}")
    for n in must_not:
        print(f"  NOT ok={n not in t}")
    return t

t = check("NurSapat.Cad.UI/Services/CadToolbarLayout.cs",
    "\u0418\u043c\u043f\u043e\u0441\u0442",
    "\u0421\u041e\u0417\u0414\u0410\u041d\u0418\u0415",
    "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f",
    "\u0414\u0415\u041a\u041e\u0420\u0418\u0420\u041e\u0412\u0410\u041d\u0418\u0415",
    must_not=("frame-fictitious", "\ufffd"))
data = Path("NurSapat.Cad.UI/Services/CadToolbarLayout.cs").read_bytes()
print("  BOM", data[:3]==bytes([0xEF,0xBB,0xBF]))
print("  U+0098 char", "\u0098" in t)
print("  Impost count", t.count("\u0418\u043c\u043f\u043e\u0441\u0442"))

check("NurSapat.App/ViewModels/OrderDrawingViewModel.cs",
    'Id == "dummy-frame"',
    must_not=("fictitious-frame",))
od = Path("NurSapat.App/ViewModels/OrderDrawingViewModel.cs").read_text(encoding="utf-8-sig")
print("  has Gabarit tool title", "\u0413\u0430\u0431\u0430\u0440\u0438\u0442\u043d\u0430\u044f \u0440\u0430\u043c\u043a\u0430" in od)

ff = Path("NurSapat.Core/Models/Orders/FictitiousFrame.cs").read_text(encoding="utf-8-sig")
print("Fictitious default label ok", "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 (\u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f)" in ff)
print("old Gabarit label gone", "\u0413\u0430\u0431\u0430\u0440\u0438\u0442\u043d\u0430\u044f \u0440\u0430\u043c\u043a\u0430 (\u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f)" not in ff)

vm = Path("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs").read_text(encoding="utf-8-sig")
m = re.search(r"private bool TryCommitRealFrameInLeaf\(FrameNode leaf\)([\s\S]*?)\n    private ", vm)
body = m.group(1) if m else ""
print("inset in TryCommitRealFrameInLeaf", "ResolveInnerInset" in body)
print("exact leaf dims", "RoundMm(leaf.Width)" in body and "originX = RoundMm(leaf.X)" in body)

m2 = re.search(r"private bool TryCommitNestedRealFrameAt[\s\S]*?\n    private bool ShouldPreserve", vm)
nested = m2.group(0) if m2 else ""
print("nested requires transition", "FindTransitionInLeaf" in nested)
print("nested uses IsLeaf", "leaf.IsLeaf" in nested)

print("CanDelete allows Frame dummy", "CadSelectableKind.Frame && FrameRoot?.IsDummy == true" in vm)
print("TryDelete Frame case", "case CadSelectableKind.Frame when FrameRoot?.IsDummy == true" in vm)
print("TryDeleteSelection cascade", "_constructionSelection.Any(item => item.Kind == CadSelectableKind.Frame)" in vm)

# show CanDelete block for syntax check
for i,l in enumerate(vm.splitlines(),1):
    if "CanDeleteConstructionItem" in l:
        for j in range(i, i+12):
            print(f"{j}|{vm.splitlines()[j-1]}")
        break

cc = Path("NurSapat.Cad.UI/Controls/CadCanvasControl.cs").read_text(encoding="utf-8-sig")
print("canvas DummySelectedFramePen", "DummySelectedFramePen" in cc)
print("canvas DrawResizeHandles", "DrawResizeHandles" in cc)
print("canvas overlay", "DrawSelectedRectangleOverlay" in cc)

sel = Path("NurSapat.Cad.UI/Rendering/ConstructionSelectionRenderer.cs").read_text(encoding="utf-8-sig")
print("sel DrawDummyResizeHandles", "DrawDummyResizeHandles" in sel)

ex = Path("NurSapat.App/Services/Export/ExportService.cs").read_text(encoding="utf-8-sig")
print("export DummyFrame filter", "DummyFrame.IsDummyElement" in ex)

# fix CanDelete if broken operator precedence (|| after or chain)
mcan = re.search(r"private bool CanDeleteConstructionItem[\s\S]*?;", vm)
print("CanDelete snippet:")
print(mcan.group(0) if mcan else "MISSING")

print("\nfictitious-frame files:")
for path in Path(".").rglob("*.cs"):
    if "tools" in path.parts:
        continue
    try:
        txt = path.read_text(encoding="utf-8-sig")
    except Exception:
        continue
    if "fictitious-frame" in txt:
        print(" ", path)
