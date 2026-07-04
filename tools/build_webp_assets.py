#!/usr/bin/env python3
"""Convert PNG images to adjacent WebP files and zip non-PNG image assets.

The script writes each converted WebP next to its source PNG, then creates a
zip containing all files under the input directory except PNG files, .DS_Store
files, and zip files. Zip entries are written under an "images/" prefix to
match the real content-root layout (the script is invoked against the
images/ subtree only, so relative paths would otherwise be missing that
segment), and a file_manifest.json covering every entry is embedded in the
zip so the bundle can be validated on its own.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import zipfile

from PIL import Image


def parse_args() -> argparse.Namespace:
    """Parses the script's command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Convert PNG files to WebP and zip assets without PNG files."
    )
    parser.add_argument(
        "images_dir",
        type=Path,
        help="Directory containing PNG and JSON image assets.",
    )
    parser.add_argument(
        "--quality",
        type=int,
        default=90,
        help="WebP quality for lossy compression, from 0 to 100.",
    )
    parser.add_argument(
        "--zip",
        type=Path,
        default=None,
        help="Output zip path. Defaults to <images_dir parent>/webp-assets.zip.",
    )
    return parser.parse_args()


def convert_pngs(images_dir: Path, quality: int) -> list[Path]:
    """Converts every PNG under images_dir to an adjacent WebP file.

    Returns the WebP paths created.
    """
    pngs = sorted(images_dir.rglob("*.png"))

    converted: list[Path] = []
    for index, png in enumerate(pngs, 1):
        dest = png.with_suffix(".webp")

        with Image.open(png) as image:
            if image.mode not in ("RGB", "RGBA"):
                image = image.convert("RGBA")
            image.save(dest, "WEBP", quality=quality)
        converted.append(dest)

        if index % 25 == 0 or index == len(pngs):
            print(f"converted {index}/{len(pngs)}")

    return converted


def remove_webp_leftovers(webp_paths: list[Path]) -> None:
    """Deletes the WebP files convert_pngs wrote, once they're safely zipped.

    Avoids leaving them lingering in images_dir between runs.
    """
    for path in webp_paths:
        path.unlink()


def should_zip(path: Path) -> bool:
    """Whether path belongs in the output zip.

    Excludes PNGs, zips, .DS_Store, and any stale manifest.
    """
    # A stale manifest sitting in the source tree must never be copied verbatim
    # (it would list a mix of old png hashes and be a second file_manifest.json
    # colliding in spirit with the one this script generates).
    if path.name == "file_manifest.json":
        return False
    suffix = path.suffix.lower()
    return suffix != ".png" and suffix != ".zip" and path.name != ".DS_Store"


def archive_name(path: Path, images_dir: Path) -> str:
    """The zip entry name for a file.

    "images/" plus its path relative to images_dir, forward-slash separated.
    """
    return "images/" + path.relative_to(images_dir).as_posix()


def build_manifest(images_dir: Path) -> dict[str, str]:
    """Builds the {zip entry name: sha256} manifest for every file that will be zipped.

    Only files should_zip accepts are included.
    """
    files: dict[str, str] = {}
    for file in sorted(images_dir.rglob("*")):
        if file.is_file() and should_zip(file):
            files[archive_name(file, images_dir)] = hashlib.sha256(
                file.read_bytes()
            ).hexdigest()
    return files


def build_zip(images_dir: Path, zip_path: Path) -> tuple[int, int, int, int]:
    """Writes every should_zip file, plus a generated file_manifest.json, into zip_path.

    Returns (entries, webp, json, png) counts for the summary report.
    """
    manifest = build_manifest(images_dir)

    with zipfile.ZipFile(
        zip_path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9
    ) as zf:
        for file in sorted(images_dir.rglob("*")):
            if file.is_file() and should_zip(file):
                zf.write(file, archive_name(file, images_dir))
        zf.writestr(
            "file_manifest.json",
            json.dumps({"files": manifest}, indent=2, sort_keys=True),
        )

    with zipfile.ZipFile(zip_path) as zf:
        names = zf.namelist()

    webp_count = sum(name.lower().endswith(".webp") for name in names)
    json_count = sum(name.lower().endswith(".json") for name in names)
    png_count = sum(name.lower().endswith(".png") for name in names)
    return len(names), webp_count, json_count, png_count


def main() -> int:
    """Parses args, converts PNGs to WebP, zips the result, and prints a summary.

    Removes the WebP leftovers from images_dir once they're safely zipped.
    """
    args = parse_args()
    images_dir = args.images_dir.expanduser().resolve()
    zip_path = (
        args.zip.expanduser().resolve()
        if args.zip is not None
        else images_dir.parent / "webp-assets.zip"
    )

    if not images_dir.is_dir():
        raise SystemExit(f"Images directory does not exist: {images_dir}")
    if not 0 <= args.quality <= 100:
        raise SystemExit("--quality must be between 0 and 100")

    converted = convert_pngs(images_dir, args.quality)
    entries, webps, jsons, pngs = build_zip(images_dir, zip_path)
    remove_webp_leftovers(converted)

    print(f"converted={len(converted)}")
    print(f"zip={zip_path}")
    print(f"zip_entries={entries}")
    print(f"zip_webp={webps}")
    print(f"zip_json={jsons}")
    print(f"zip_png={pngs}")
    print(f"zip_size={zip_path.stat().st_size}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
