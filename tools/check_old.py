from pathlib import Path
import re

p = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat\tools\mainworkspace_old.axaml")
b = p.read_bytes()
print("len", len(b))
for enc in ("utf-8-sig", "utf-16", "utf-16-le", "cp1251"):
    try:
        t = b.decode(enc)
        headers = re.findall(r'Header="([^"]+)"', t)
        print("ENC", enc, "Файл" in t, headers[:15])
    except Exception as e:
        print("ENC", enc, "FAIL", e)
