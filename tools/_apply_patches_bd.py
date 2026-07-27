# -*- coding: utf-8 -*-
"""Apply dummy-frame feature finish patches."""
from pathlib import Path
import re

ROOT = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")

def read(p):
    return (ROOT / p).read_text(encoding="utf-8-sig")

def write(p, text):
    path = ROOT / p
    # preserve BOM for files that had it
    path.write_text(text, encoding="utf-8-sig")
    print("wrote", p)

# ---------- B: OrderDrawingViewModel ----------
od = read("NurSapat.App/ViewModels/OrderDrawingViewModel.cs")
od2 = od.replace(
    '        NurSapat.Cad.Core.Models.WindowToolMode.FictitiousFrame => AllTools.FirstOrDefault(t => t.Id == "fictitious-frame"),\r\n',
    ""
).replace(
    '        NurSapat.Cad.Core.Models.WindowToolMode.FictitiousFrame => AllTools.FirstOrDefault(t => t.Id == "fictitious-frame"),\n',
    ""
)
# Remove Tool("fictitious-frame"... line
od2 = re.sub(
    r'\s*Tool\("fictitious-frame",[^\n]*\n',
    "\n",
    od2,
    count=1,
)
assert "fictitious-frame" not in od2
assert "dummy-frame" in od2
write("NurSapat.App/ViewModels/OrderDrawingViewModel.cs", od2)

# ---------- B: FictitiousFrame label ----------
ff = read("NurSapat.Core/Models/Orders/FictitiousFrame.cs")
old_label = "\u0413\u0430\u0431\u0430\u0440\u0438\u0442\u043d\u0430\u044f \u0440\u0430\u043c\u043a\u0430 (\u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f)"
new_label = "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 (\u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f)"
assert old_label in ff
ff2 = ff.replace(old_label, new_label)
write("NurSapat.Core/Models/Orders/FictitiousFrame.cs", ff2)

# ---------- C+D: CadEditorViewModel ----------
vm_path = "NurSapat.Cad.UI/ViewModels/CadEditorViewModel.cs"
vm = read(vm_path)

# C: remove inset block in TryCommitRealFrameInLeaf
inset_block = """        if (FrameRoot?.IsFictitiousContainer == true)
        {
            var inset = FrameProfileGeometry.ResolveInnerInset(leaf.Width, leaf.Height, FrameThickness);
            originX = RoundMm(leaf.X + inset);
            originY = RoundMm(leaf.Y + inset);
            width = RoundMm(Math.Max(leaf.Width - inset * 2, MinEditableOpeningMm));
            height = RoundMm(Math.Max(leaf.Height - inset * 2, MinEditableOpeningMm));
        }

"""
# handle both CRLF variants already normalized in string above with \n - file may be CRLF
if inset_block not in vm:
    inset_block_crlf = inset_block.replace("\n", "\r\n")
    if inset_block_crlf in vm:
        vm = vm.replace(inset_block_crlf, "")
        print("removed inset CRLF")
    else:
        # try flexible regex
        m = re.search(
            r"\s*if \(FrameRoot\?\.IsFictitiousContainer == true\)\s*\{[^}]*ResolveInnerInset[\s\S]*?\}\s*\r?\n",
            vm,
        )
        if not m:
            raise SystemExit("Could not find inset block to remove")
        vm = vm[:m.start()] + "\n" + vm[m.end():]
        print("removed inset via regex")
else:
    vm = vm.replace(inset_block, "")
    print("removed inset LF")

# C: allow RealFrame click into dummy leaf without requiring adapter/transition
old_nested = """    private bool TryCommitNestedRealFrameAt(double worldX, double worldY)
    {
        if (!ShouldNestRealFrameInFakeContext() || FrameRoot is null)
        {
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldX, worldY);
        if (leaf is null || FindTransitionInLeaf(leaf) is null)
        {
            return false;
        }

        _isCommittingRealFrame = true;
        return TryCommitRealFrameInLeaf(leaf);
    }"""

new_nested = """    private bool TryCommitNestedRealFrameAt(double worldX, double worldY)
    {
        if (!ShouldNestRealFrameInFakeContext() || FrameRoot is null)
        {
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldX, worldY);
        if (leaf is null || !leaf.IsLeaf)
        {
            return false;
        }

        _isCommittingRealFrame = true;
        return TryCommitRealFrameInLeaf(leaf);
    }"""

if old_nested not in vm and old_nested.replace("\n","\r\n") in vm:
    vm = vm.replace(old_nested.replace("\n","\r\n"), new_nested.replace("\n","\r\n"))
elif old_nested in vm:
    vm = vm.replace(old_nested, new_nested)
else:
    raise SystemExit("TryCommitNestedRealFrameAt not found")
print("updated TryCommitNestedRealFrameAt")

old_preserve = """    private bool ShouldPreserveFakeContainerForPlacement(CadPoint worldPoint) =>
        CurrentToolMode == WindowToolMode.RealFrame
        && ShouldNestRealFrameInFakeContext()
        && FrameRoot?.FindLeafAt(worldPoint.X, worldPoint.Y) is FrameNode leaf
        && FindTransitionInLeaf(leaf) is not null;"""

new_preserve = """    private bool ShouldPreserveFakeContainerForPlacement(CadPoint worldPoint) =>
        CurrentToolMode == WindowToolMode.RealFrame
        && ShouldNestRealFrameInFakeContext()
        && FrameRoot?.FindLeafAt(worldPoint.X, worldPoint.Y) is FrameNode leaf
        && leaf.IsLeaf;"""

if old_preserve not in vm and old_preserve.replace("\n","\r\n") in vm:
    vm = vm.replace(old_preserve.replace("\n","\r\n"), new_preserve.replace("\n","\r\n"))
elif old_preserve in vm:
    vm = vm.replace(old_preserve, new_preserve)
else:
    raise SystemExit("ShouldPreserveFakeContainerForPlacement not found")
print("updated ShouldPreserveFakeContainerForPlacement")

# D: CanDeleteConstructionItem + TryDeleteConstructionItem for dummy Frame
old_can = """    private static bool CanDeleteConstructionItem(CadSelectableItem item) =>
        item.Kind is CadSelectableKind.Sash
            or CadSelectableKind.Glazing
            or CadSelectableKind.Bead
            or CadSelectableKind.Impost
            or CadSelectableKind.ExtremeImpost
            or CadSelectableKind.SashHandle
            or CadSelectableKind.SashHinge;"""

new_can = """    private bool CanDeleteConstructionItem(CadSelectableItem item) =>
        item.Kind is CadSelectableKind.Sash
            or CadSelectableKind.Glazing
            or CadSelectableKind.Bead
            or CadSelectableKind.Impost
            or CadSelectableKind.ExtremeImpost
            or CadSelectableKind.SashHandle
            or CadSelectableKind.SashHinge
            || (item.Kind is CadSelectableKind.Frame && FrameRoot?.IsDummy == true);"""

if old_can not in vm and old_can.replace("\n","\r\n") in vm:
    vm = vm.replace(old_can.replace("\n","\r\n"), new_can.replace("\n","\r\n"))
elif old_can in vm:
    vm = vm.replace(old_can, new_can)
else:
    raise SystemExit("CanDeleteConstructionItem not found")
print("updated CanDelete")

# Insert Frame case before default in TryDeleteConstructionItem
old_default = """            case CadSelectableKind.SashHinge when item.ElementId is Guid hingeSashId:
                return TryClearSashHardware(hingeSashId);
            default:
                return false;"""

new_default = """            case CadSelectableKind.SashHinge when item.ElementId is Guid hingeSashId:
                return TryClearSashHardware(hingeSashId);
            case CadSelectableKind.Frame when FrameRoot?.IsDummy == true:
                ClearCommittedFrameGeometry(clearPlacementHistory: false);
                return true;
            default:
                return false;"""

if old_default not in vm and old_default.replace("\n","\r\n") in vm:
    vm = vm.replace(old_default.replace("\n","\r\n"), new_default.replace("\n","\r\n"))
elif old_default in vm:
    vm = vm.replace(old_default, new_default)
else:
    raise SystemExit("TryDeleteConstructionItem default not found")
print("updated TryDeleteConstructionItem")

# Also improve TryDeleteSelectionFromTool for dummy FrameRoot via construction selection early, and SelectedElement
old_sel_del = """        if (SelectedElement is FrameNode)
        {
            RecordPlacement("""

# Make it explicitly handle dummy cascade (already clears all) - also when construction selection has Frame
old_try_del = """    private bool TryDeleteSelectionFromTool()
    {
        if (_constructionSelection.Count > 0 && HasCommittedConstruction)
        {
            return TryDeleteConstructionSelection();
        }"""

new_try_del = """    private bool TryDeleteSelectionFromTool()
    {
        if (_constructionSelection.Count > 0 && HasCommittedConstruction)
        {
            if (_constructionSelection.Any(item => item.Kind == CadSelectableKind.Frame)
                && FrameRoot?.IsDummy == true)
            {
                RecordPlacement("\\u041a\\u043e\\u043d\\u0441\\u0442\\u0440\\u0443\\u043a\\u0446\\u0438\\u044f \\u0443\\u0434\\u0430\\u043b\\u0435\\u043d\\u0430".encode().decode("unicode_escape") if False else "\u041a\u043e\u043d\u0441\u0442\u0440\u0443\u043a\u0446\u0438\u044f \u0443\u0434\u0430\u043b\u0435\u043d\u0430",
                    () => ClearCommittedFrameGeometry(clearPlacementHistory: false));
                ClearConstructionSelection();
                RefreshAfterPlacementHistoryChange();
                RequestSceneRefresh();
                StatusText = "\u041a\u043e\u043d\u0441\u0442\u0440\u0443\u043a\u0446\u0438\u044f \u0443\u0434\u0430\u043b\u0435\u043d\u0430";
                return true;
            }

            return TryDeleteConstructionSelection();
        }"""

# Fix the awkward ternary - rebuild cleanly
new_try_del = """    private bool TryDeleteSelectionFromTool()
    {
        if (_constructionSelection.Count > 0 && HasCommittedConstruction)
        {
            if (_constructionSelection.Any(item => item.Kind == CadSelectableKind.Frame)
                && FrameRoot?.IsDummy == true)
            {
                RecordPlacement("\u041a\u043e\u043d\u0441\u0442\u0440\u0443\u043a\u0446\u0438\u044f \u0443\u0434\u0430\u043b\u0435\u043d\u0430",
                    () => ClearCommittedFrameGeometry(clearPlacementHistory: false));
                ClearConstructionSelection();
                RefreshAfterPlacementHistoryChange();
                RequestSceneRefresh();
                StatusText = "\u041a\u043e\u043d\u0441\u0442\u0440\u0443\u043a\u0446\u0438\u044f \u0443\u0434\u0430\u043b\u0435\u043d\u0430";
                return true;
            }

            return TryDeleteConstructionSelection();
        }"""

if old_try_del not in vm and old_try_del.replace("\n","\r\n") in vm:
    vm = vm.replace(old_try_del.replace("\n","\r\n"), new_try_del.replace("\n","\r\n"))
elif old_try_del in vm:
    vm = vm.replace(old_try_del, new_try_del)
else:
    raise SystemExit("TryDeleteSelectionFromTool start not found")
print("updated TryDeleteSelectionFromTool")

# SelectedElement FrameNode - ensure dummy clears (already does ClearCommittedFrameGeometry)
old_frame_node_del = """        if (SelectedElement is FrameNode)
        {
            RecordPlacement("""
# leave as-is since it already clears construction

write(vm_path, vm)

print("VM patches applied OK")
