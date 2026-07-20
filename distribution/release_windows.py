#!/usr/bin/env python3
"""Build a Windows release package for the Cut the Rope DX Level Editor.

Usage: python release_windows.py <version>

Must run on Windows: NativeAOT does not support cross-OS compilation.
Uses only the standard library, so no pip install is needed.
"""

import argparse
import subprocess
import sys
import zipfile
from pathlib import Path

APP_NAME = "CtrDxEditor"
RID = "win-x64"

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
PROJECT = PROJECT_ROOT / "src" / "CtrDxEditor.Desktop" / "CtrDxEditor.Desktop.csproj"
PUBLISH_DIR = PROJECT_ROOT / "publish" / RID
RELEASE_DIR = PROJECT_ROOT / "publish" / "release_github"


def publish(version: str) -> None:
    """Publish the self-contained NativeAOT build for win-x64."""
    cmd = [
        "dotnet", "publish", str(PROJECT),
        "-c", "Release",
        "-r", RID,
        f"-p:VersionPrefix={version}",
        "-p:VersionSuffix=",
        "-o", str(PUBLISH_DIR),
    ]
    print(f"Building v{version} for {RID}...")
    print("> " + " ".join(cmd) + "\n")
    result = subprocess.run(cmd, check=False)
    if result.returncode != 0:
        sys.exit(result.returncode)

    exe = PUBLISH_DIR / f"{APP_NAME}.exe"
    if not exe.is_file():
        sys.exit(f"Error: expected executable not found at {exe}")


def package(version: str) -> None:
    """Compress the publish output into a .zip archive."""
    RELEASE_DIR.mkdir(parents=True, exist_ok=True)
    archive_path = RELEASE_DIR / f"{APP_NAME}-v{version}-Windows-x64.zip"
    archive_path.unlink(missing_ok=True)

    files = sorted(f for f in PUBLISH_DIR.rglob("*") if f.is_file())
    print(f"\nPackaging {archive_path.name} ({len(files)} files)...")

    with zipfile.ZipFile(archive_path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for file in files:
            archive.write(file, str(file.relative_to(PUBLISH_DIR)))

    size_mb = archive_path.stat().st_size / (1024 * 1024)
    print(f"Created {archive_path} ({size_mb:.1f} MB)")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("version", help="Release version, e.g. 1.2.0")
    args = parser.parse_args()

    publish(args.version)
    package(args.version)


if __name__ == "__main__":
    main()
