"""Normalize an AI reference sheet into Hajimao's fixed modular character atlas."""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

from PIL import Image


@dataclass(frozen=True)
class Slot:
    source_box: tuple[int, int, int, int]
    destination_box: tuple[int, int, int, int]


SLOTS = (
    Slot((100, 40, 410, 290), (14, 9, 13, 14)),
    Slot((410, 40, 710, 300), (44, 9, 12, 17)),
    Slot((710, 40, 1010, 300), (76, 10, 9, 16)),
    Slot((100, 290, 410, 535), (18, 35, 5, 15)),
    Slot((410, 290, 710, 535), (48, 36, 5, 12)),
    Slot((710, 290, 1010, 535), (78, 40, 5, 6)),
    Slot((100, 530, 410, 775), (18, 60, 5, 14)),
    Slot((410, 530, 710, 775), (47, 60, 6, 13)),
    Slot((710, 530, 1010, 775), (77, 64, 5, 6)),
    Slot((100, 770, 410, 1010), (18, 84, 5, 14)),
    Slot((410, 770, 710, 1010), (47, 84, 5, 14)),
    Slot((710, 770, 1010, 1010), (75, 90, 7, 5)),
    Slot((100, 1000, 410, 1240), (105, 84, 5, 14)),
    Slot((410, 1000, 710, 1240), (46, 107, 5, 14)),
    Slot((710, 1000, 1010, 1240), (75, 114, 7, 4)),
)


def transparent_reference(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            red, green, blue, _ = pixels[x, y]
            near_neutral = max(red, green, blue) - min(red, green, blue) <= 12
            if near_neutral and min(red, green, blue) >= 220:
                pixels[x, y] = (0, 0, 0, 0)
            else:
                pixels[x, y] = (red, green, blue, 255)
    return rgba


def visible_bounds(image: Image.Image) -> tuple[int, int, int, int]:
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("A source slot did not contain a visible character part.")
    return bounds


def fit_part(part: Image.Image, width: int, height: int) -> Image.Image:
    scale = min(width / part.width, height / part.height)
    target_width = max(1, round(part.width * scale))
    target_height = max(1, round(part.height * scale))
    return part.resize((target_width, target_height), Image.Resampling.NEAREST)


def normalize(source: Path, destination: Path) -> None:
    with Image.open(source) as opened:
        reference = transparent_reference(opened)
    atlas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    for slot in SLOTS:
        source_part = reference.crop(slot.source_box)
        source_part = source_part.crop(visible_bounds(source_part))
        x, y, width, height = slot.destination_box
        part = fit_part(source_part, width, height)
        paste_x = x + (width - part.width) // 2
        paste_y = y + height - part.height
        atlas.alpha_composite(part, (paste_x, paste_y))

    alpha = atlas.getchannel("A")
    indexed = atlas.convert("RGB").quantize(colors=32, method=Image.Quantize.MEDIANCUT)
    atlas = indexed.convert("RGBA")
    atlas.putalpha(alpha.point(lambda value: 255 if value >= 128 else 0))
    destination.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(destination, format="PNG", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    normalize(args.input, args.output)


if __name__ == "__main__":
    main()
