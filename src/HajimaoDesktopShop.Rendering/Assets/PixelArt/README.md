# Hajimao Market pixel atlas

`market-atlas.png` is the only pixel-art image embedded in the runtime assembly.
It is a deterministic 256×256 RGBA atlas built by
`tools/pixel-assets/build_market_atlas.py` and must remain below 256 KiB.

## Provenance

The source sheets were generated specifically for Hajimao Market with OpenAI's
built-in image generation on 2026-08-04. No third-party game asset pack is used.
The retained atlas is project-owned generated artwork; raw generation and
chroma-key working files are intentionally excluded from the repository.

Prompt direction:

- consistent polished 16-bit-era small-market management-game pixel art;
- right-facing, bottom-aligned four-frame cashier, restocker and customer strips;
- ambient, chilled and frozen grocery fixtures in one three-slot sheet;
- ten configured products in order: water, bread, instant noodles, chips, milk,
  soda, sandwich, yogurt, ice cream and frozen dumplings;
- flat `#ff00ff` key background, no text, logos, scenery or extra subjects.

The builder removes border-connected chroma noise, keeps the largest subject in
each fixed cell, uses nearest-neighbor sampling, and bottom-centers each sprite.
The atlas layout is defined by `PixelSpriteAtlas`; the documentation preview is
`docs/assets/v0.1.7-market-atlas-preview.png`.

## Rebuild

Raw source paths are explicit inputs and are not committed:

```powershell
python tools\pixel-assets\build_market_atlas.py `
  --cashier <cashier-strip.png> `
  --restocker <restocker-strip.png> `
  --customer <customer-strip.png> `
  --shelves <shelves.png> `
  --products <products.png> `
  --out src\HajimaoDesktopShop.Rendering\Assets\PixelArt\market-atlas.png `
  --preview docs\assets\v0.1.7-market-atlas-preview.png
```
