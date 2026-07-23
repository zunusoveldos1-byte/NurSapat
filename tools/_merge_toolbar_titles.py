# -*- coding: utf-8 -*-
from pathlib import Path
import re, subprocess

root = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")
cur_path = root / "NurSapat.Cad.UI/Services/CadToolbarLayout.cs"
head = subprocess.check_output(
    ["git", "show", "HEAD:NurSapat.Cad.UI/Services/CadToolbarLayout.cs"], cwd=root
).decode("utf-8")
cur = cur_path.read_text(encoding="utf-8-sig")

head_titles = dict(re.findall(r'Tool\("([^"]+)",\s*"([^"]*)"', head))
head_blocks = dict(re.findall(r'public const string (Block\w+) = "([^"]*)";', head))
head_blocks["BlockDecoration"] = "\u0414\u0415\u041a\u041e\u0420\u0418\u0420\u041e\u0412\u0410\u041d\u0418\u0415"

# Known titles for tools only in CUR (not in HEAD)
extra_titles = {
    "sill": "\u041f\u043e\u0434\u043e\u043a\u043e\u043d\u043d\u0438\u043a",
    "redo": "\u0412\u0435\u0440\u043d\u0443\u0442\u044c",
    "decorative-spoke": "\u0414\u0435\u043a\u043e\u0440\u0430\u0442\u0438\u0432\u043d\u0430\u044f \u0448\u043f\u0440\u043e\u0441\u0430",
}

# Merge: replace Tool("id", "ANY") titles from HEAD/extra
def repl_tool(m):
    tid, old = m.group(1), m.group(2)
    title = head_titles.get(tid) or extra_titles.get(tid)
    if title is None:
        print("WARN no title for", tid, "keeping (may be broken)")
        return m.group(0)
    return f'Tool("{tid}", "{title}"'

new = re.sub(r'Tool\("([^"]+)",\s*"([^"]*)"', repl_tool, cur)

def repl_block(m):
    name = m.group(1)
    if name in head_blocks:
        return f'public const string {name} = "{head_blocks[name]}";'
    return m.group(0)

new = re.sub(r'public const string (Block\w+) = "([^"]*)";', repl_block, new)

# Fix XML docs from HEAD if possible
# HEAD class summary
head_class_sum = None
m = re.search(r'/// <summary>([^<]+)</summary>\s*\r?\npublic static class CadToolbarLayout', head)
if m:
    head_class_sum = m.group(1)
    print("HEAD class summary:", head_class_sum)
# Also file-level / first summary in HEAD
# CUR has two summaries - restore via known good text
# Use HEAD's first two summaries + add decoration-aware one for class? Looking at HEAD structure

# Replace any corrupted summary lines that contain U+0098 or mojibake patterns
# Safer: replace the known doc lines with correct Russian from HEAD + BlockDecoration context

docs = {
    # Matches approximate ASCII skeletons - replace whole summary content when we know them from HEAD
}
# Extract all /// <summary>...</summary> from HEAD
head_sums = re.findall(r'/// <summary>([^<]*)</summary>', head)
print("HEAD summaries:", head_sums)

# Current summaries
cur_sums = re.findall(r'/// <summary>([^<]*)</summary>', new)
print("CUR summaries count", len(cur_sums))
for i,s in enumerate(cur_sums):
    print(i, "".join(("?" if ord(c)>127 else c) for c in s)[:80])

# Restore summaries: if we have exactly 2 in HEAD and >=2 in CUR, replace first N from HEAD
# For extra CUR summaries, write sensible Russian
good_sums = list(head_sums)
# CUR may have summary for layout that mentions decoration - add third
# Looking at CUR line 8 and 18 from earlier analysis:
# Line 8: file/type summary about CAD toolbar layout
# Line 18: summary about toolbar blocks: creation, editing and decorating

# If CUR has 2 summaries and HEAD has 2, use HEAD for both.
# If CUR has more, pad with fixed text for decoration summary.
fixed_extra = [
    "\u0420\u0430\u0441\u043a\u043b\u0430\u0434\u043a\u0430 \u043f\u0430\u043d\u0435\u043b\u0438 \u0438\u043d\u0441\u0442\u0440\u0443\u043c\u0435\u043d\u0442\u043e\u0432 CAD.",
    "\u0421\u0442\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0439 \u0440\u0430\u0441\u043a\u043b\u0430\u0434: \u0441\u043e\u0437\u0434\u0430\u043d\u0438\u0435, \u0440\u0435\u0434\u0430\u043a\u0442\u0438\u0440\u043e\u0432\u0430\u043d\u0438\u0435 \u0438 \u0434\u0435\u043a\u043e\u0440\u0438\u0440\u043e\u0432\u0430\u043d\u0438\u0435.",
]
# Prefer HEAD when available
if len(head_sums) >= 1:
    fixed_extra[0] = head_sums[0]
if len(head_sums) >= 2:
    # If HEAD second doesn't mention decoration but CUR has BlockDecoration, update
    fixed_extra[1] = "\u0421\u0442\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0439 \u0440\u0430\u0441\u043a\u043b\u0430\u0434: \u0441\u043e\u0437\u0434\u0430\u043d\u0438\u0435, \u0440\u0435\u0434\u0430\u043a\u0442\u0438\u0440\u043e\u0432\u0430\u043d\u0438\u0435 \u0438 \u0434\u0435\u043a\u043e\u0440\u0438\u0440\u043e\u0432\u0430\u043d\u0438\u0435."

idx = [0]
def repl_sum(m):
    i = idx[0]
    idx[0] += 1
    if i < len(fixed_extra):
        return f"/// <summary>{fixed_extra[i]}</summary>"
    return m.group(0)

new2 = re.sub(r'/// <summary>[^<]*</summary>', repl_sum, new, count=2)

# Ensure no frame-fictitious
assert "frame-fictitious" not in new2

# Write utf-8-sig
cur_path.write_text(new2, encoding="utf-8-sig")
print("Wrote", cur_path)

# Verify
data = cur_path.read_bytes()
assert data[:3] == bytes([0xEF,0xBB,0xBF])
text = data[3:].decode("utf-8")
assert "\u0418\u043c\u043f\u043e\u0441\u0442" in text
assert "\ufffd" not in text
assert bytes([0x98]) not in data  # U+0098 as lone byte shouldn't appear - but wait, in UTF-8 U+0098 is C2 98
# Check for lone 0x98 not part of UTF-8 sequence: search for bad pattern from before: D0 A0 C2 98 was mojibake for ?
assert "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f" in text
assert "frame-fictitious" not in text
assert "\u0414\u0415\u041a\u041e\u0420\u0418\u0420\u041e\u0412\u0410\u041d\u0418\u0415" in text
# Count tool titles restored
for tid, title in head_titles.items():
    if f'Tool("{tid}", "{title}"' not in text and tid in dict(re.findall(r'Tool\("([^"]+)"', text)):
        print("MISSING restore", tid)
print("OK encoding verify")
print("impost count", text.count("\u0418\u043c\u043f\u043e\u0441\u0442"))
