#!/usr/bin/env python3
"""Build Hajimao DesktopShop's deterministic 256x256 production sprite atlas."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


ATLAS_SIZE = (256, 256)
CHARACTER_SIZE = (32, 40)
SHELF_SIZE = (64, 56)
PRODUCT_SIZE = (16, 16)
NEAREST = getattr(Image, "Resampling", Image).NEAREST


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cashier", required=True)
    parser.add_argument("--restocker", required=True)
    parser.add_argument("--customer", required=True)
    parser.add_argument("--shelves", required=True)
    parser.add_argument("--products", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--preview")
    return parser.parse_args()


def remove_magenta_key(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size

    def is_connected_key(red: int, green: int, blue: int) -> bool:
        return (
            red >= 70
            and blue >= 70
            and green * 5 <= max(red, blue) * 3
            and abs(red - blue) <= 110
        )

    queue: deque[tuple[int, int]] = deque()
    visited = bytearray(width * height)
    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        index = y * width + x
        if visited[index]:
            continue
        visited[index] = 1
        red, green, blue, _ = pixels[x, y]
        if not is_connected_key(red, green, blue):
            continue
        pixels[x, y] = (0, 0, 0, 0)
        for next_x, next_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= next_x < width and 0 <= next_y < height:
                queue.append((next_x, next_y))

    for y in range(rgba.height):
        for x in range(rgba.width):
            red, green, blue, alpha = pixels[x, y]
            is_key = red >= 180 and blue >= 150 and green <= 125 and red + blue >= green * 4
            if is_key:
                pixels[x, y] = (0, 0, 0, 0)
            elif alpha:
                pixels[x, y] = (red, green, blue, 255)
    return rgba


def largest_component_bounds(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    width, height = image.size
    visible = alpha.load()
    visited = bytearray(width * height)
    best: tuple[int, int, int, int, int] | None = None

    for start_y in range(height):
        for start_x in range(width):
            index = start_y * width + start_x
            if visited[index] or visible[start_x, start_y] == 0:
                continue

            queue = deque([(start_x, start_y)])
            visited[index] = 1
            count = 0
            min_x = max_x = start_x
            min_y = max_y = start_y

            while queue:
                x, y = queue.popleft()
                count += 1
                min_x = min(min_x, x)
                max_x = max(max_x, x)
                min_y = min(min_y, y)
                max_y = max(max_y, y)
                for next_x, next_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if not (0 <= next_x < width and 0 <= next_y < height):
                        continue
                    next_index = next_y * width + next_x
                    if visited[next_index] or visible[next_x, next_y] == 0:
                        continue
                    visited[next_index] = 1
                    queue.append((next_x, next_y))

            candidate = (count, min_x, min_y, max_x + 1, max_y + 1)
            if best is None or candidate[0] > best[0]:
                best = candidate

    if best is None:
        raise ValueError("Sprite cell contains no visible pixels after chroma-key removal.")
    return best[1], best[2], best[3], best[4]


def extract_cells(image_path: str, columns: int, rows: int) -> list[Image.Image]:
    keyed = remove_magenta_key(Image.open(image_path))
    cells: list[Image.Image] = []
    for row in range(rows):
        top = round(row * keyed.height / rows)
        bottom = round((row + 1) * keyed.height / rows)
        for column in range(columns):
            left = round(column * keyed.width / columns)
            right = round((column + 1) * keyed.width / columns)
            cell = keyed.crop((left, top, right, bottom))
            cells.append(cell.crop(largest_component_bounds(cell)))
    return cells


def fit_sprite(sprite: Image.Image, size: tuple[int, int], padding: int = 1) -> Image.Image:
    target_width, target_height = size
    available_width = target_width - padding * 2
    available_height = target_height - padding * 2
    scale = min(available_width / sprite.width, available_height / sprite.height)
    width = max(1, round(sprite.width * scale))
    height = max(1, round(sprite.height * scale))
    resized = sprite.resize((width, height), NEAREST)
    frame = Image.new("RGBA", size, (0, 0, 0, 0))
    frame.alpha_composite(resized, ((target_width - width) // 2, target_height - padding - height))
    return frame


def paste_strip(atlas: Image.Image, sprites: list[Image.Image], y: int, size: tuple[int, int]) -> None:
    for index, sprite in enumerate(sprites):
        atlas.alpha_composite(fit_sprite(sprite, size), (index * size[0], y))


def save_preview(atlas: Image.Image, path: Path) -> None:
    checker = Image.new("RGBA", atlas.size, (238, 229, 206, 255))
    checker_pixels = checker.load()
    for y in range(atlas.height):
        for x in range(atlas.width):
            if (x // 8 + y // 8) % 2:
                checker_pixels[x, y] = (220, 210, 185, 255)
    checker.alpha_composite(atlas)
    path.parent.mkdir(parents=True, exist_ok=True)
    checker.resize((1024, 1024), NEAREST).save(path, optimize=True)


def main() -> None:
    args = parse_args()
    atlas = Image.new("RGBA", ATLAS_SIZE, (0, 0, 0, 0))

    paste_strip(atlas, extract_cells(args.cashier, 4, 1), 0, CHARACTER_SIZE)
    paste_strip(atlas, extract_cells(args.restocker, 4, 1), 40, CHARACTER_SIZE)
    paste_strip(atlas, extract_cells(args.customer, 4, 1), 80, CHARACTER_SIZE)
    paste_strip(atlas, extract_cells(args.shelves, 3, 1), 120, SHELF_SIZE)
    paste_strip(atlas, extract_cells(args.products, 5, 2), 176, PRODUCT_SIZE)

    output = Path(args.out)
    output.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(output, optimize=True)
    if args.preview:
        save_preview(atlas, Path(args.preview))

    encoded_size = output.stat().st_size
    if encoded_size > 256 * 1024:
        raise SystemExit(f"Atlas exceeds 256 KiB budget: {encoded_size} bytes")
    print(f"Wrote {output} ({encoded_size} bytes)")


if __name__ == "__main__":
    main()
