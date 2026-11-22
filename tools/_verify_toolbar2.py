# -*- coding: utf-8 -*-
from pathlib import Path
import re

p = Path("NurSapat.Cad.UI/Services/CadToolbarLayout.cs")
data = p.read_bytes()
text = data.decode("utf-8-sig")
print("bom", data[:3]==bytes([0xEF,0xBB,0xBF]))
print("has Impost", "\u0418\u043c\u043f\u043e\u0441\u0442" in text)
print("has replacement", "\ufffd" in text)
print("has U+0098 char", "\u0098" in text)
print("frame-fictitious", "frame-fictitious" in text)
print("BlockCreation", re.search(r'BlockCreation = "([^"]*)"', text).group(1))
print("BlockDecoration", re.search(r'BlockDecoration = "([^"]*)"', text).group(1))
# check for mojibake of ?: pattern of Cyrillic A + U+0098
bad = "\u0410\u0098" in text or "\u0420\u0098" in text
print("mojibake A+0098", bad)
# show some titles
for tid in ["frame-real","frame-fake","impost-fake-v","impost-h","sash","sill","decorative-spoke","redo"]:
    m = re.search(rf'Tool\("{tid}",\s*"([^"]*)"', text)
    print(tid, "->", m.group(1) if m else "MISSING")
for s in re.findall(r'/// <summary>([^<]*)</summary>', text):
    print("DOC:", s)
# any tool still with weird chars (U+2019 etc from old mojibake)
weird = []
for m in re.finditer(r'Tool\("([^"]+)",\s*"([^"]*)"', text):
    t = m.group(2)
    if any(ord(c) in (0x98, 0x2019, 0x201d, 0x2022, 0x201a, 0x201e) for c in t):
        weird.append((m.group(1), t, [hex(ord(c)) for c in t if ord(c)>127]))
print("weird titles", weird)
