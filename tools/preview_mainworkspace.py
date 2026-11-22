from pathlib import Path
import re

p = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat\NurSapat.App\Views\MainWorkspaceWindow.axaml")
b = p.read_bytes()
print("bytes", len(b))
for enc in ("utf-8", "cp1251", "cp866"):
    try:
        t = b.decode(enc)
        headers = re.findall(r'Header="([^"]+)"', t)
        print("ENC", enc, "headers sample", headers[:15])
    except Exception as e:
        print("ENC", enc, "FAIL", e)
