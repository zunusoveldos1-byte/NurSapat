# -*- coding: utf-8 -*-
from pathlib import Path
import re, subprocess, json

root = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")
cur_path = root / "NurSapat.Cad.UI/Services/CadToolbarLayout.cs"
head = subprocess.check_output(
    ["git", "show", "HEAD:NurSapat.Cad.UI/Services/CadToolbarLayout.cs"], cwd=root
).decode("utf-8")
cur = cur_path.read_text(encoding="utf-8-sig")

head_titles = dict(re.findall(r'Tool\("([^"]+)",\s*"([^"]*)"', head))
head_blocks = dict(re.findall(r'public const string (Block\w+) = "([^"]*)";', head))

# Dump for verification
out = {
    "blocks": head_blocks,
    "sample": {k: head_titles[k] for k in [
        "frame-real","frame-fake","impost-fake-v","impost-fake-h","arch","mullion-edge",
        "frame-fake-circle","frame-fake-triangle","adapter-profile","impost-h","sash","hardware","handle"
    ] if k in head_titles},
    "all_ids": sorted(head_titles.keys()),
}
(Path("tools/_head_titles.json")).write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")
print("wrote tools/_head_titles.json")
print("blocks:", head_blocks)
for k,v in out["sample"].items():
    print(k, "=", v)

# Check required
required = {
    "BlockCreation": "\u0421\u041e\u0417\u0414\u0410\u041d\u0418\u0415",
    "BlockEditing": "\u0420\u0415\u0414\u0410\u041a\u0422\u0418\u0420\u041e\u0412\u0410\u041d\u0418\u0415",
    "frame-real": "\u041a\u043e\u0440\u043e\u0431\u043a\u0430",
    "frame-fake": "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f",
    "impost-fake-v": "\u0418\u043c\u043f\u043e\u0441\u0442 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u044b\u0439 \u0432\u0435\u0440\u0442\u0438\u043a\u0430\u043b\u044c\u043d\u044b\u0439",
    "impost-fake-h": "\u0418\u043c\u043f\u043e\u0441\u0442 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u044b\u0439 \u0433\u043e\u0440\u0438\u0437\u043e\u043d\u0442\u0430\u043b\u044c\u043d\u044b\u0439",
    "arch": "\u0410\u0440\u043e\u0447\u043d\u0430\u044f \u043a\u043e\u0440\u043e\u0431\u043a\u0430",
    "mullion-edge": "\u0421\u0442\u043e\u0439\u043a\u0430/\u0440\u0438\u0433\u0435\u043b\u044c \u0432 \u043a\u0440\u0430\u0439\u043d\u0435\u043c \u043f\u043e\u043b\u043e\u0436\u0435\u043d\u0438\u0438",
    "frame-fake-circle": "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f \u043a\u0440\u0443\u0433\u043b\u0430\u044f",
    "frame-fake-triangle": "\u041a\u043e\u0440\u043e\u0431\u043a\u0430 \u0444\u0438\u043a\u0442\u0438\u0432\u043d\u0430\u044f \u0442\u0440\u0435\u0443\u0433\u043e\u043b\u044c\u043d\u0430\u044f",
    "adapter-profile": "\u041f\u0435\u0440\u0435\u0445\u043e\u0434\u043d\u043e\u0439 \u043f\u0440\u043e\u0444\u0438\u043b\u044c",
}
ok = True
for k,v in required.items():
    if k.startswith("Block"):
        got = head_blocks.get(k)
    else:
        got = head_titles.get(k)
    match = got == v
    print("REQ", k, "OK" if match else f"FAIL got={got!r}")
    if not match:
        ok = False
print("all_required_ok", ok)

# BlockDecoration from CUR somehow? try recover from double encoding
# Show CUR block decoration raw codepoints
m = re.search(r'BlockDecoration = "([^"]*)"', cur)
if m:
    s = m.group(1)
    print("CUR BlockDecoration codepoints", [hex(ord(c)) for c in s[:20]])
    print("CUR BlockDecoration len", len(s))
