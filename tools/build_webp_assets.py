#!/usr/bin/env python3
"""Convert PNG images to adjacent WebP files and zip non-PNG image assets.

The script writes each converted WebP next to its source PNG, then creates a
zip containing all files under the input directory except PNG files, .DS_Store
files, zip files, and the legacy top-level webp output folder.
"""

from __future__ import annotations

import argparse
from pathlib import Path
import zipfile

from PIL import Image


def parse_args() -> argparse.Namespace:
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


def is_legacy_webp_output(path: Path, images_dir: Path) -> bool:
    return path.relative_to(images_dir).parts[:1] == ("webp",)


def convert_pngs(images_dir: Path, quality: int) -> int:
    pngs = [
        path
        for path in sorted(images_dir.rglob("*.png"))
        if not is_legacy_webp_output(path, images_dir)
    ]

    for index, png in enumerate(pngs, 1):
        dest = png.with_suffix(".webp")

        with Image.open(png) as image:
            if image.mode not in ("RGB", "RGBA"):
                image = image.convert("RGBA")
            image.save(dest, "WEBP", quality=quality)

        if index % 25 == 0 or index == len(pngs):
            print(f"converted {index}/{len(pngs)}")

    return len(pngs)


def should_zip(path: Path, images_dir: Path) -> bool:
    if is_legacy_webp_output(path, images_dir):
        return False
    suffix = path.suffix.lower()
    return suffix != ".png" and suffix != ".zip" and path.name != ".DS_Store"


def build_zip(images_dir: Path, zip_path: Path) -> tuple[int, int, int, int]:
    with zipfile.ZipFile(
        zip_path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9
    ) as zf:
        for file in sorted(images_dir.rglob("*")):
            if file.is_file() and should_zip(file, images_dir):
                zf.write(file, file.relative_to(images_dir))

    with zipfile.ZipFile(zip_path) as zf:
        names = zf.namelist()

    webp_count = sum(name.lower().endswith(".webp") for name in names)
    json_count = sum(name.lower().endswith(".json") for name in names)
    png_count = sum(name.lower().endswith(".png") for name in names)
    return len(names), webp_count, json_count, png_count


def main() -> int:
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

    print(f"converted={converted}")
    print(f"zip={zip_path}")
    print(f"zip_entries={entries}")
    print(f"zip_webp={webps}")
    print(f"zip_json={jsons}")
    print(f"zip_png={pngs}")
    print(f"zip_size={zip_path.stat().st_size}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
