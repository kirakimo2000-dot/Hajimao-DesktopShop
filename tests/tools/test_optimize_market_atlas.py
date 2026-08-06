from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = ROOT / "tools" / "pixel-assets" / "optimize_market_atlas.py"
SPEC = importlib.util.spec_from_file_location("optimize_market_atlas", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class OptimizeMarketAtlasTests(unittest.TestCase):
    def test_clean_character_frame_keeps_largest_component_and_removes_fragment(self):
        frame = Image.new("RGBA", (32, 40), (0, 0, 0, 0))
        for y in range(10, 14):
            for x in range(12, 16):
                frame.putpixel((x, y), (255, 200, 100, 255))
        frame.putpixel((4, 20), (255, 200, 100, 255))

        cleaned = MODULE.clean_character_frame(frame)

        self.assertEqual((0, 0, 0, 0), cleaned.getpixel((4, 20)))
        self.assertEqual((255, 200, 100, 255), cleaned.getpixel((12, 10)))
        self.assertEqual([16], MODULE.component_sizes(cleaned))

    def test_audit_character_rows_rejects_blank_frame(self):
        atlas = self.valid_atlas()
        atlas.paste((0, 0, 0, 0), (0, 0, 32, 40))

        with self.assertRaisesRegex(ValueError, "cashier cel 0 is blank"):
            MODULE.audit_character_rows(atlas)

    def test_audit_character_rows_reports_all_twenty_four_stored_cels(self):
        results = MODULE.audit_character_rows(self.valid_atlas())

        self.assertEqual(24, len(results))
        self.assertTrue(all(result.component_count == 1 for result in results))
        self.assertTrue(all(result.bottom_padding > 0 for result in results))

    def test_optimize_atlas_writes_compact_indexed_png_at_fixed_size(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "source.png"
            output = Path(directory) / "output.png"
            self.valid_atlas().save(str(source))

            report = MODULE.optimize_file(source, output)

            with Image.open(str(output)) as optimized:
                self.assertEqual((256, 256), optimized.size)
                self.assertEqual("P", optimized.mode)
                MODULE.audit_character_rows(optimized.convert("RGBA"))
            self.assertLessEqual(report.output_bytes, 24 * 1024)
            self.assertLess(report.output_bytes, report.input_bytes)

    @staticmethod
    def valid_atlas():
        atlas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        for row_y in (0, 40, 80):
            for index in range(8):
                left = index * 32 + 12
                for y in range(row_y + 10, row_y + 30):
                    for x in range(left, left + 8):
                        atlas.putpixel((x, y), (40 + index * 10, 80 + row_y, 120, 255))
        for y in range(120, 256):
            for x in range(256):
                if (x + y) % 7 == 0:
                    atlas.putpixel((x, y), (x % 255, y % 255, (x + y) % 255, 255))
        return atlas


if __name__ == "__main__":
    unittest.main()
