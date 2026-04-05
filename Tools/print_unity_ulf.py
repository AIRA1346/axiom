#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Windows에서 Unity .ulf 라이선스 파일을 흔한 위치에서 찾아 텍스트로 터미널에 출력합니다.
저장소에 커밋하지 말고, 본인 PC에서만 사용하세요.
"""

from __future__ import annotations

import os
import sys
from pathlib import Path


def unity_search_bases() -> list[Path]:
    """Unity가 설정/라이선스를 두는 디렉터리(Windows, Unity 6 포함)."""
    env = os.environ
    bases: list[Path] = []

    for key in ("LOCALAPPDATA", "APPDATA", "PROGRAMDATA"):
        v = env.get(key)
        if v:
            bases.append(Path(v) / "Unity")

    # Hub 캐시·에디터 설치 쪽(버전 폴더 안에 .ulf 가 드물게 있을 수 있음)
    for key in ("ProgramFiles", "ProgramFiles(x86)"):
        v = env.get(key)
        if v:
            bases.append(Path(v) / "Unity" / "Hub")

    seen: set[Path] = set()
    out: list[Path] = []
    for b in bases:
        try:
            b = b.resolve()
        except OSError:
            continue
        if b not in seen and b.exists():
            seen.add(b)
            out.append(b)
    return out


def find_ulf_files() -> list[Path]:
    found: list[Path] = []
    for base in unity_search_bases():
        try:
            for p in base.rglob("*.ulf"):
                found.append(p)
        except (OSError, PermissionError):
            continue

    return sorted({p.resolve() for p in found}, key=lambda p: str(p).lower())


def print_file(path: Path) -> None:
    raw = path.read_bytes()
    for enc in ("utf-8", "utf-8-sig", "cp949", "latin-1"):
        try:
            text = raw.decode(enc)
            break
        except UnicodeDecodeError:
            text = None
    else:
        text = raw.decode("utf-8", errors="replace")

    sep = "=" * 72
    print(sep)
    print(f"파일: {path}")
    print(f"크기: {len(raw)} bytes")
    print(sep)
    print(text)


def main() -> int:
    if len(sys.argv) >= 2:
        arg = Path(sys.argv[1]).expanduser()
        if not arg.is_file():
            print(f"파일 없음: {arg}", file=sys.stderr)
            return 1
        print_file(arg)
        return 0

    paths = find_ulf_files()
    if not paths:
        print(
            "표준 Unity 폴더에서 .ulf 를 찾지 못했습니다.\n"
            "  (예: %LOCALAPPDATA%\\Unity, %PROGRAMDATA%\\Unity 등)\n"
            "직접 경로를 지정하세요:\n"
            f"  python \"{Path(__file__).name}\" \"C:\\\\경로\\\\파일.ulf\"",
            file=sys.stderr,
        )
        return 1

    if len(paths) == 1:
        print_file(paths[0])
        return 0

    print("여러 개의 .ulf 가 있습니다. 모두 출력합니다.\n", file=sys.stderr)
    for p in paths:
        print_file(p)
        print()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
