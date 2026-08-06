# Hajimao DesktopShop pixel atlas

`market-atlas.png` is the only pixel-art image embedded in the runtime assembly.
It is a deterministic 256×256 indexed-color atlas built and normalized by the
scripts under `tools/pixel-assets` and must remain below 24 KiB.

## Provenance

The source sheets were generated specifically for Hajimao DesktopShop with OpenAI's
built-in image generation on 2026-08-04. No third-party game asset pack is used.
The retained atlas is project-owned generated artwork; raw generation and
chroma-key working files are intentionally excluded from the repository.

Prompt direction:

- consistent polished 16-bit-era small-market management-game pixel art;
- right-facing, bottom-aligned eight-cel cashier, restocker and customer key-pose strips;
- ambient, chilled and frozen grocery fixtures in one three-slot sheet;
- ten configured products in order: water, bread, instant noodles, chips, milk,
  soda, sandwich, yogurt, ice cream and frozen dumplings;
- flat `#ff00ff` key background, no text, logos, scenery or extra subjects.

The runtime animation contract is 24 logical frames for every person. Three logical
frames reference each of the eight stored key cels, while actor positions still
advance on every presentation frame. This avoids storing three identical copies of
each pose.

The builder removes border-connected chroma noise, uses nearest-neighbor sampling,
and bottom-centers each sprite. The optimizer retains only the largest connected
component in each character cel, rejects blank/clipped/detached results, and writes
a no-dither 256-color PNG. `PixelSpriteAtlas` repeats the same audit at load time.
The current documentation preview is
`docs/assets/v0.1.14-market-atlas-preview.png`.

## Rebuild

Raw source paths are explicit inputs and are not committed:

```powershell
python tools\pixel-assets\build_market_atlas.py `
  --cashier <cashier-strip.png> `
  --restocker <restocker-strip.png> `
  --customer <customer-strip.png> `
  --base-atlas src\HajimaoDesktopShop.Rendering\Assets\PixelArt\market-atlas.png `
  --normalized-characters `
  --out src\HajimaoDesktopShop.Rendering\Assets\PixelArt\market-atlas.png

python tools\pixel-assets\optimize_market_atlas.py `
  --input src\HajimaoDesktopShop.Rendering\Assets\PixelArt\market-atlas.png `
  --out src\HajimaoDesktopShop.Rendering\Assets\PixelArt\market-atlas.png `
  --preview docs\assets\v0.1.14-market-atlas-preview.png
```

For a completely new atlas, omit `--base-atlas` and
`--normalized-characters`, and provide `--shelves` and `--products` instead.
