# -*- coding: utf-8 -*-
from pathlib import Path
p = Path(r"C:\Users\User\.cursor\projects\c-Users-User-Desktop-Development-Vs-2022-NurSapat\agent-tools\f52d9e5b-c254-4f5d-93fb-d699e1c192aa.txt")
text = p.read_text(encoding="utf-8", errors="replace")
# Write ascii-safe extract focusing on key ranges
out = []
for line in text.splitlines():
    if any(k in line for k in [
        "TryCommitNested", "FindTransition", "AdapterProfile", "ShouldNest",
        "DummySelectionPen", "SelectedFramePen", "resize", "Resize", "handle",
        "IsDummy", "DrawRect", "HasFakeFrame", "FrameNode tree", "IsDummy",
        "Enumerate", "FrameRoot", "IsFictitiousElement", "materials", "Material",
        "CadSelectableKind.Frame", "case CadSelectableKind"
    ]) or line.startswith("=====") or "|private static readonly" in line or "|private static readonly Pen" in line:
        out.append(line[:220])
Path("tools/_focus.txt").write_text("\n".join(out), encoding="utf-8")
print("focus lines", len(out))
# Also dump specific Adapt/nested and canvas handle drawing by line search in sources
for path, needles in [
    ("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", ["TryCommitNestedRealFrameAt", "FindTransitionInLeaf", "TryHandleAdapterProfileClickAsync", "ShouldNestRealFrameInFakeContext()", "if (mode == WindowToolMode.RealFrame"]),
    ("NurSapat.Cad.UI/Controls/CadCanvasControl.cs", ["SelectedFramePen", "DrawResize", "resize", "Handle", "selectionRect", "IsDummy", "HasFake"]),
    ("NurSapat.App/Services/Export/ExportService.cs", ["IsFictitious", "IsDummy", "FrameNode", "Elements", "foreach"]),
]:
    lines = Path(path).read_text(encoding="utf-8-sig").splitlines()
    print("\nFILE", path)
    for i,l in enumerate(lines,1):
        for n in needles:
            if n in l:
                # print context +/- 8
                a=max(1,i-2); b=min(len(lines), i+12)
                print(f"-- around {i} ({n}) --")
                for j in range(a,b+1):
                    print(f"{j}|{lines[j-1].encode('unicode_escape').decode()[:180]}")
                break
