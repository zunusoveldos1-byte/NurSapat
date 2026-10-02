# -*- coding: utf-8 -*-
from pathlib import Path
import re

root = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")
patterns = [
    "fictitious-frame",
    "FictitiousFrame",
    "TryCommitRealFrameInLeaf",
    "IsFictitiousContainer",
    "IsDummy",
    "DummySelectionPen",
    "CanDeleteConstructionItem",
    "TryDeleteSelectionFromTool",
    "HasFakeFrameOnCanvas",
    "ClearCommittedFrameGeometry",
    "SelectedFramePen",
    "SelectedRectangle",
    "IsFictitiousElement",
    "dummy-frame",
    "FakeFrame",
    "WindowToolMode.FictitiousFrame",
]

# Prefer scanning these dirs
dirs = [
    root/"NurSapat.App",
    root/"NurSapat.Cad.UI",
    root/"NurSapat.Cad.Core",
    root/"NurSapat.Core",
]

hits = {p: [] for p in patterns}
for d in dirs:
    for path in d.rglob("*"):
        if path.suffix.lower() not in {".cs", ".axaml"}:
            continue
        try:
            text = path.read_text(encoding="utf-8-sig")
        except Exception:
            continue
        for pat in patterns:
            if pat in text:
                for i, line in enumerate(text.splitlines(), 1):
                    if pat in line:
                        hits[pat].append(f"{path.relative_to(root)}:{i}:{line.strip()[:160]}")

for pat, lines in hits.items():
    print(f"\n=== {pat} ({len(lines)}) ===")
    for L in lines[:40]:
        print(L)
    if len(lines) > 40:
        print(f"... +{len(lines)-40} more")
