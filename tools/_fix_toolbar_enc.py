from pathlib import Path
import os, re, subprocess

root = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")
cur_path = root / "NurSapat.Cad.UI/Services/CadToolbarLayout.cs"
cur = cur_path.read_bytes()
print("CUR bom", cur[:3] == bytes([0xEF,0xBB,0xBF]), "len", len(cur))
print("frame-fictitious in CUR", b"frame-fictitious" in cur)
print("0x98 count", cur.count(bytes([0x98])))

# Extract tool titles from current (broken) via ASCII keys
text_as_latin = cur.decode("latin1")
# Also try fixing double mojibake: utf8 misread as cp1251 then utf8 again
# Classic: original utf-8 -> interpreted as cp1251 -> saved as utf-8
# Fix: encode as cp1251 -> decode as utf-8

def try_fix(data: bytes):
    results = {}
    # strip BOM
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    t = data.decode("utf-8", errors="replace")
    results["as_utf8_has_impost"] = "\u0418\u043c\u043f\u043e\u0441\u0442" in t
    results["replacement"] = "\ufffd" in t
    # double mojibake fix
    try:
        fixed = t.encode("cp1251").decode("utf-8")
        results["cp1251_fix"] = fixed
        results["cp1251_has_impost"] = "\u0418\u043c\u043f\u043e\u0441\u0442" in fixed
        results["cp1251_has_sozdanie"] = "\u0421\u041e\u0417\u0414\u0410\u041d\u0418\u0415" in fixed
    except Exception as e:
        results["cp1251_err"] = str(e)
    # triple?
    try:
        fixed2 = t.encode("latin1").decode("utf-8")
        results["latin1_has_impost"] = "\u0418\u043c\u043f\u043e\u0441\u0442" in fixed2
    except Exception as e:
        results["latin1_err"] = str(e)
    return results

r = try_fix(cur)
for k,v in r.items():
    if k.endswith("_fix"):
        # print first tool titles
        for line in v.splitlines():
            if "BlockCreation" in line or 'Tool("frame' in line or 'Tool("impost' in line:
                print(k, line[:140])
    else:
        print(k, v)

# HEAD analysis
head = subprocess.check_output(["git","show","HEAD:NurSapat.Cad.UI/Services/CadToolbarLayout.cs"], cwd=root)
print("HEAD len", len(head), "bom", head[:3]==bytes([0xEF,0xBB,0xBF]))
ht = head.decode("utf-8", errors="replace")
print("HEAD has impost", "\u0418\u043c\u043f\u043e\u0441\u0442" in ht)
print("HEAD has sozdanie", "\u0421\u041e\u0417\u0414\u0410\u041d\u0418\u0415" in ht)
print("HEAD replacement", "\ufffd" in ht)
# show HEAD tool titles (ascii keys)
for line in ht.splitlines():
    if "BlockCreation" in line or "public const string Block" in line or 'Tool("frame' in line or 'Tool("impost-h' in line or 'Tool("sash' in line:
        print("HEAD:", line[:160])

print("--- git log ---")
print(subprocess.check_output(["git","log","--oneline","-15","--","NurSapat.Cad.UI/Services/CadToolbarLayout.cs"], cwd=root, text=True))
