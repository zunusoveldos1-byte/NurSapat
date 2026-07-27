#!/usr/bin/env python3
"""Convert mis-encoded project files to UTF-8."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP_PARTS = {"bin", "obj", ".vs", ".git", "node_modules", "tools"}
TEXT_EXTENSIONS = {
    ".axaml", ".cs", ".xaml", ".json", ".md", ".txt", ".csproj", ".sln", ".axaml.cs", ".resx"
}


def iter_source_files() -> list[Path]:
    files: list[Path] = []
    for path in ROOT.rglob("*"):
        if path.suffix.lower() not in TEXT_EXTENSIONS:
            continue
        if any(part in SKIP_PARTS for part in path.parts):
            continue
        if path.is_file():
            files.append(path)
    return files


def classify(data: bytes) -> str:
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError:
        return "invalid_utf8"

    if "\ufffd" in text:
        return "utf8_with_replacement"
    if any(ord(c) == 0x98 for c in text):
        return "utf8_with_0x98"
    return "utf8_ok"


def decode_best(data: bytes) -> tuple[str, str]:
    kind = classify(data)
    if kind == "utf8_ok":
        return data.decode("utf-8"), "utf8_ok"

    if kind == "invalid_utf8":
        return data.decode("cp1251"), "cp1251_to_utf8"

    # replacement chars: cp1251 bytes were saved and then re-read as utf-8 with loss
    # try cp1251 first for invalid utf8 case only
    text = data.decode("utf-8")
    if "\ufffd" in text:
        # attempt cp1251 roundtrip only when original bytes are valid cp1251
        try:
            return data.decode("cp1251"), "cp1251_to_utf8"
        except UnicodeDecodeError:
            pass
    return text, kind


def main() -> int:
    changed: list[tuple[str, str]] = []
    issues: list[tuple[str, str]] = []

    for path in iter_source_files():
        data = path.read_bytes()
        kind = classify(data)
        if kind == "utf8_ok":
            continue

        rel = str(path.relative_to(ROOT))
        try:
            text, source = decode_best(data)
        except Exception as exc:  # noqa: BLE001
            issues.append((rel, f"decode_failed: {exc}"))
            continue

        if source in {"cp1251_to_utf8", "utf8_with_replacement", "utf8_with_0x98"}:
            path.write_text(text, encoding="utf-8", newline="\n")
            changed.append((rel, source))
        else:
            issues.append((rel, source))

    print(f"Converted {len(changed)} file(s) to UTF-8:")
    for rel, source in changed:
        print(f"  [{source}] {rel}")

    if issues:
        print(f"\nUnresolved {len(issues)} file(s):")
        for rel, reason in issues:
            print(f"  [{reason}] {rel}")

    return 0 if not issues else 1


if __name__ == "__main__":
    sys.exit(main())
