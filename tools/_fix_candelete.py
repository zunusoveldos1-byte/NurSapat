# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")
vm_path = ROOT / "NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs"
vm = vm_path.read_text(encoding="utf-8-sig")

old = """    private bool CanDeleteConstructionItem(CadSelectableItem item) =>
        item.Kind is CadSelectableKind.Sash
            or CadSelectableKind.Glazing
            or CadSelectableKind.Bead
            or CadSelectableKind.Impost
            or CadSelectableKind.ExtremeImpost
            or CadSelectableKind.SashHandle
            or CadSelectableKind.SashHinge
            || (item.Kind is CadSelectableKind.Frame && FrameRoot?.IsDummy == true);"""

new = """    private bool CanDeleteConstructionItem(CadSelectableItem item) =>
        (item.Kind is CadSelectableKind.Sash
            or CadSelectableKind.Glazing
            or CadSelectableKind.Bead
            or CadSelectableKind.Impost
            or CadSelectableKind.ExtremeImpost
            or CadSelectableKind.SashHandle
            or CadSelectableKind.SashHinge)
        || (item.Kind is CadSelectableKind.Frame && FrameRoot?.IsDummy == true);"""

if old not in vm and old.replace("\n","\r\n") in vm:
    vm = vm.replace(old.replace("\n","\r\n"), new.replace("\n","\r\n"))
elif old in vm:
    vm = vm.replace(old, new)
else:
    raise SystemExit("CanDelete block not found for fix")

vm_path.write_text(vm, encoding="utf-8-sig")
print("fixed CanDelete parentheses")

# Export usings
ex = (ROOT/"NurSapat.App/Services/Export/ExportService.cs").read_text(encoding="utf-8-sig")
print("Export has using Orders", "NurSapat.Core.Models.Orders" in ex)
print("DummyFrame class exists", (ROOT/"NurSapat.Core/Models/Orders/DummyFrame.cs").exists())

# CadCanvas usings for SelectionTool / FrameVolumeRenderer
cc = (ROOT/"NurSapat.Cad.UI/Controls/CadCanvasControl.cs").read_text(encoding="utf-8-sig")
for line in cc.splitlines()[:25]:
    if line.startswith("using"):
        print(line)
