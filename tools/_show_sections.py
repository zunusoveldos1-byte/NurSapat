# -*- coding: utf-8 -*-
from pathlib import Path

def show(path, start, end):
    p = Path(path)
    lines = p.read_text(encoding="utf-8-sig").splitlines()
    for i in range(start-1, min(end, len(lines))):
        print(f"{i+1}|{lines[i]}")
    print("---")

show("NurSapat.App/ViewModels/OrderDrawingViewModel.cs", 110, 125)
show("NurSapat.App/ViewModels/OrderDrawingViewModel.cs", 310, 330)
show("NurSapat.Core/Models/Orders/FictitiousFrame.cs", 1, 80)
show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 3045, 3120)
show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 3375, 3420)
show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 2255, 2330)
show("NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs", 2400, 2470)
