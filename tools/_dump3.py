# -*- coding: utf-8 -*-
from pathlib import Path

def dump(path, ranges, outpath):
    lines = Path(path).read_text(encoding="utf-8-sig").splitlines()
    with Path(outpath).open("w", encoding="utf-8") as out:
        for start,end in ranges:
            out.write(f"\n===== {path}:{start}-{end} =====\n")
            for i in range(start-1, min(end, len(lines))):
                out.write(f"{i+1}|{lines[i].encode('unicode_escape').decode()}\n")

dump("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", [
    (2288, 2335),
    (2405, 2480),
    (2835, 2865),
    (2975, 3020),
    (3035, 3055),
    (3135, 3250),
    (3385, 3415),
    (4515, 4560),
    (4655, 4720),
], "tools/_vm_sections.txt")

dump("NurSapat.Cad.UI/Controls/CadCanvasControl.cs", [
    (1, 80),
    (300, 520),
], "tools/_canvas_sections.txt")

dump("NurSapat.Cad.UI/Rendering/ConstructionSelectionRenderer.cs", [
    (1, 120),
], "tools/_sel_sections.txt")

dump("NurSapat.App/Services/Export/ExportService.cs", [
    (1, 50),
    (180, 350),
], "tools/_export_sections.txt")
print("ok")
