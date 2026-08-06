#!/usr/bin/env python3
"""Audit, clean, and compact Hajimao DesktopShop's production pixel atlas."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path
from typing import NamedTuple

from PIL import Image


ATLAS_SIZE = (256, 256)
CHARACTER_SIZE = (32, 40)
CHARACTER_ROWS = (("cashier", 0), ("restocker", 40), ("customer", 80))
STORED_CELS_PER_CHARACTER = 8
MAXIMUM_OUTPUT_BYTES = 24 * 1024
NEAREST = getattr(Image, "Resampling", Image).NEAREST


class FrameAudit(NamedTuple):
    role: str
    cel_index: int
    visible_pixels: int
    component_count: int
    left_padding: int
    top_padding: int
    right_padding: int
    bottom_padding: int


class OptimizationReport(NamedTuple):
    input_bytes: int
    output_bytes: int
    removed_pixels: int


def visible_components(image: Image.Image) -> list[list[tuple[int, int]]]:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    pixels = alpha.load()
    width, height = rgba.size
    visited = bytearray(width * height)
    components: list[list[tuple[int, int]]] = []

    for start_y in range(height):
        for start_x in range(width):
            index = start_y * width + start_x
            if visited[index] or pixels[start_x, start_y] == 0:
                continue
            visited[index] = 1
            queue = deque([(start_x, start_y)])
            component: list[tuple[int, int]] = []
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                for next_x, next_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if not (0 <= next_x < width and 0 <= next_y < height):
                        continue
                    next_index = next_y * width + next_x
                    if visited[next_index] or pixels[next_x, next_y] == 0:
                        continue
                    visited[next_index] = 1
                    queue.append((next_x, next_y))
            components.append(component)
    return components


def component_sizes(image: Image.Image) -> list[int]:
    return sorted((len(component) for component in visible_components(image)), reverse=True)


def clean_character_frame(frame: Image.Image) -> Image.Image:
    rgba = frame.convert("RGBA")
    components = visible_components(rgba)
    if not components:
        raise ValueError("Character frame is blank.")
    primary = max(components, key=len)
    retained = set(primary)
    cleaned = rgba.copy()
    for component in components:
        if component is primary:
            continue
        for point in component:
            if point not in retained:
                cleaned.putpixel(point, (0, 0, 0, 0))
    return cleaned


def audit_character_rows(atlas: Image.Image) -> list[FrameAudit]:
    rgba = atlas.convert("RGBA")
    if rgba.size != ATLAS_SIZE:
        raise ValueError(f"Atlas must be {ATLAS_SIZE[0]}x{ATLAS_SIZE[1]}; received {rgba.size}.")

    results: list[FrameAudit] = []
    width, height = CHARACTER_SIZE
    for role, row_y in CHARACTER_ROWS:
        for cel_index in range(STORED_CELS_PER_CHARACTER):
            left = cel_index * width
            frame = rgba.crop((left, row_y, left + width, row_y + height))
            components = visible_components(frame)
            if not components:
                raise ValueError(f"{role} cel {cel_index} is blank")
            if len(components) != 1:
                sizes = sorted((len(component) for component in components), reverse=True)
                raise ValueError(
                    f"{role} cel {cel_index} contains {len(components)} detached components: {sizes}")
            xs = [point[0] for point in components[0]]
            ys = [point[1] for point in components[0]]
            result = FrameAudit(
                role,
                cel_index,
                len(components[0]),
                1,
                min(xs),
                min(ys),
                width - max(xs) - 1,
                height - max(ys) - 1,
            )
            if min(
                result.left_padding,
                result.top_padding,
                result.right_padding,
                result.bottom_padding,
            ) <= 0:
                raise ValueError(f"{role} cel {cel_index} touches its frame edge")
            results.append(result)
    return results


def clean_character_rows(atlas: Image.Image) -> tuple[Image.Image, int]:
    cleaned = atlas.convert("RGBA").copy()
    removed_pixels = 0
    width, height = CHARACTER_SIZE
    for _, row_y in CHARACTER_ROWS:
        for cel_index in range(STORED_CELS_PER_CHARACTER):
            left = cel_index * width
            bounds = (left, row_y, left + width, row_y + height)
            source = cleaned.crop(bounds)
            before = sum(component_sizes(source))
            frame = clean_character_frame(source)
            after = sum(component_sizes(frame))
            removed_pixels += before - after
            cleaned.paste(frame, bounds)
    return cleaned, removed_pixels


def optimize_file(source: Path, output: Path) -> OptimizationReport:
    source = Path(source)
    output = Path(output)
    input_bytes = source.stat().st_size
    with Image.open(str(source)) as opened:
        atlas = opened.convert("RGBA")
    if atlas.size != ATLAS_SIZE:
        raise ValueError(f"Atlas must be {ATLAS_SIZE[0]}x{ATLAS_SIZE[1]}; received {atlas.size}.")

    cleaned, removed_pixels = clean_character_rows(atlas)
    audit_character_rows(cleaned)
    optimized = cleaned.quantize(colors=256, method=Image.FASTOCTREE, dither=Image.NONE)
    output.parent.mkdir(parents=True, exist_ok=True)
    optimized.save(str(output), optimize=True)

    output_bytes = output.stat().st_size
    if output_bytes > MAXIMUM_OUTPUT_BYTES:
        raise ValueError(
            f"Optimized atlas exceeds {MAXIMUM_OUTPUT_BYTES} bytes: {output_bytes} bytes")
    with Image.open(str(output)) as decoded:
        if decoded.mode != "P":
            raise ValueError(f"Optimized atlas must use indexed color; received {decoded.mode}.")
        audit_character_rows(decoded.convert("RGBA"))
    return OptimizationReport(input_bytes, output_bytes, removed_pixels)


def save_preview(atlas_path: Path, preview_path: Path) -> None:
    with Image.open(str(atlas_path)) as opened:
        atlas = opened.convert("RGBA")
    checker = Image.new("RGBA", atlas.size, (238, 229, 206, 255))
    checker_pixels = checker.load()
    for y in range(atlas.height):
        for x in range(atlas.width):
            if (x // 8 + y // 8) % 2:
                checker_pixels[x, y] = (220, 210, 185, 255)
    checker.alpha_composite(atlas)
    preview_path.parent.mkdir(parents=True, exist_ok=True)
    checker.resize((1024, 1024), NEAREST).save(str(preview_path), optimize=True)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--preview", type=Path)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    report = optimize_file(args.input, args.out)
    if args.preview:
        save_preview(args.out, args.preview)
    print(
        f"Optimized {args.input} -> {args.out}: "
        f"{report.input_bytes} -> {report.output_bytes} bytes; "
        f"removed {report.removed_pixels} detached pixels")


if __name__ == "__main__":
    main()
