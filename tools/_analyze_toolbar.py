# -*- coding: utf-8 -*-
from pathlib import Path
import re, subprocess

root = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")
cur_path = root / "NurSapat.Cad.UI/Services/CadToolbarLayout.cs"
head = subprocess.check_output(
    ["git", "show", "HEAD:NurSapat.Cad.UI/Services/CadToolbarLayout.cs"], cwd=root
)
ht = head.decode("utf-8")
ct = cur_path.read_text(encoding="utf-8-sig")

# Map id -> title from HEAD Tool("id", "title",
head_titles = dict(re.findall(r'Tool\("([^"]+)",\s*"([^"]*)"', ht))
cur_ids = re.findall(r'Tool\("([^"]+)",\s*"([^"]*)"', ct)
print("HEAD tools", len(head_titles))
print("CUR tools", len(cur_ids))
only_cur = [i for i,_ in cur_ids if i not in head_titles]
only_head = [i for i in head_titles if i not in dict(cur_ids)]
print("only_cur", only_cur)
print("only_head", only_head)

# Block constants from HEAD
for m in re.finditer(r'public const string (Block\w+) = "([^"]*)";', ht):
    print("HEAD", m.group(1), "=", m.group(2))

# Print HEAD titles as unicode escapes for verification
for tid in ["frame-real","frame-fake","impost-fake-v","impost-fake-h","arch","mullion-edge",
            "frame-fake-circle","frame-fake-triangle","adapter-profile","impost-h","sash"]:
    print(tid, "->", head_titles.get(tid, "MISSING"), repr(head_titles.get(tid)))

# Show class summary from HEAD
for line in ht.splitlines()[:25]:
    if "summary" in line or "///" in line:
        print("DOC:", line)

# Diff structure: lengths of CreationTools etc
print("--- CUR first 40 lines (repr keys only) ---")
for i,line in enumerate(ct.splitlines()[:45],1):
    if "Block" in line or "Tool(" in line or "summary" in line or "class " in line:
        # sanitize: show only ASCII parts + whether cyrillic present
        has_cyr = any("\u0400" <= ch <= "\u04FF" for ch in line)
        print(f"{i}: has_cyr={has_cyr} ascii={''.join(ch if ord(ch)<128 else '?' for ch in line)[:160]}")
