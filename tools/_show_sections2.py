# -*- coding: utf-8 -*-
from pathlib import Path

def show(path, start, end, out):
    p = Path(path)
    lines = p.read_text(encoding="utf-8-sig").splitlines()
    out.write(f"\n===== {path}:{start}-{end} =====\n")
    for i in range(start-1, min(end, len(lines))):
        # ascii-safe: escape non-ascii
        s = lines[i].encode("unicode_escape").decode("ascii")
        out.write(f"{i+1}|{s}\n")

outp = Path("tools/_sections_out.txt")
with outp.open("w", encoding="utf-8") as out:
    show("NurSapat.App/ViewModels/OrderDrawingViewModel.cs", 110, 125, out)
    show("NurSapat.App/ViewModels/OrderDrawingViewModel.cs", 310, 330, out)
    show("NurSapat.Core/Models/Orders/FictitiousFrame.cs", 1, 80, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 3045, 3120, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 3375, 3420, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 2255, 2330, out)
    show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 2400, 2470, out)
    show("NurSapat.Cad.UI/Controls/CadCanvasControl.cs", 1, 80, out)
    show("NurSapat.Cad.UI/Controls/CadCanvasControl.cs", 330, 450, out)
    show("NurSapat.Cad.UI/Rendering/ConstructionSelectionRenderer.cs", 1, 100, out)
    show("NurSapat.App/Services/Export/ExportService.cs", 200, 280, out)
print("wrote", outp, "bytes", outp.stat().st_size)
