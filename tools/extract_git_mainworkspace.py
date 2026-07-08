import re
import subprocess
from pathlib import Path

repo = Path(r"C:\Users\User\Desktop\Development\Vs 2022\NurSapat")
path = "NurSapat.App/Views/MainWorkspaceWindow.axaml"

for commit in ("2f779dd", "602bfae", "d2d81e9"):
    data = subprocess.check_output(["git", "-C", str(repo), "show", f"{commit}:{path}"])
    for enc in ("utf-8", "utf-8-sig", "utf-16", "utf-16-le", "cp1251"):
        try:
            text = data.decode(enc)
        except Exception:
            continue
        if "Файл" in text or "Правка" in text or "Чертеж" in text:
            headers = re.findall(r'Header="([^"]+)"', text)
            print(commit, enc, "GOOD", len(text), headers[:12])
            out = repo / "tools" / f"mainworkspace_{commit}.axaml"
            out.write_text(text, encoding="utf-8")
            print("saved", out)
            break
