# -*- coding: utf-8 -*-
from pathlib import Path

def show(path, start, end, out):
    lines = Path(path).read_text(encoding="utf-8-sig").splitlines()
    out.write(f"\n===== {path}:{start}-{end} =====\n")
    for i in range(start-1, min(end, len(lines))):
        s = lines[i].encode("unicode_escape").decode("ascii")
        out.write(f"{i+1}|{s}\n")

with Path("tools/_sections_out2.txt").open("w", encoding="utf-8") as out:
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 2268, 2320, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 2405, 2475, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 3020, 3055, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 3200, 3260, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 3419, 3520, out)
    show("NurSapat.Cad.UI/Controls/CadCanvasControl.cs", 1, 100, out)
    show("NurSapat.Cad.UI/Controls/CadCanvasControl.cs", 300, 500, out)
    show("NurSapat.Cad.UI/Rendering/ConstructionSelectionRenderer.cs", 1, 120, out)
    show("NurSapat.App/Services/Export/ExportService.cs", 1, 80, out)
    show("NurSapat.App/Services/Export/ExportService.cs", 200, 320, out)

# Search RealFrame click handlers and AdapterProfile
text = Path("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs").read_text(encoding="utf-8-sig")
for needle in ["TryCommitNestedRealFrameAt", "TryCommitRealFrameInLeaf", "AdapterProfile", "ShouldNestRealFrameInFakeContext", "FindTransitionInLeaf", "FindFrameInLeaf"]:
    out_lines = []
    for i,line in enumerate(text.splitlines(),1):
        if needle in line:
            out_lines.append(f"{i}:{line.strip()[:200].encode('unicode_escape').decode()}")
    print(needle, len(out_lines))
    for L in out_lines[:25]:
        print(" ", L)
