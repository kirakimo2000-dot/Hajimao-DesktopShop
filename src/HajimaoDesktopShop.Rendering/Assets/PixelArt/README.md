# Hajimao DesktopShop pixel atlas

`market-atlas.png` is the only pixel-art image embedded in the runtime assembly.
It is a deterministic 256×256 RGBA atlas built by
`tools/pixel-assets/build_market_atlas.py` and must remain below 256 KiB.

## Provenance

The source sheets were generated specifically for Hajimao DesktopShop with OpenAI's
built-in image generation on 2026-08-04. No third-party game asset pack is used.
The retained atlas is project-owned generated artwork; raw generation and
chroma-key working files are intentionally excluded from the repository.

Prompt direction:

- consistent polished 16-bit-era small-market management-game pixel art;
- right-facing, bottom-aligned eight-frame cashier, restocker and customer strips;
- ambient, chilled and frozen grocery fixtures in one three-slot sheet;
- ten configured products in order: water, bread, instant noodles, chips, milk,
  soda, sandwich, yogurt, ice cream and frozen dumplings;
- flat `#ff00ff` key background, no text, logos, scenery or extra subjects.

The builder removes border-connected chroma noise, filters isolated fragments,
uses nearest-neighbor sampling, and bottom-centers each sprite. When extending an
existing atlas, `--normalized-characters` preserves the original first frame and
all fixture/product cells while importing eight transparent 64×64 frames per role.
The atlas layout is defined by `PixelSpriteAtlas`; the current documentation
preview is `docs/assets/v0.1.10-market-atlas-preview.png`.

## Rebuild

Raw source paths are explicit inputs and are not committed:

```powershell
python tools\pixel-assets\build_market_atlas.py `
  --cashier <cashier-strip.png> `
  --restocker <restocker-strip.png> `
  --customer <customer-strip.png> `
  --base-atlas src\HajimaoDesktopShop.Rendering\Assets\PixelArt\market-atlas.png `
  --normalized-characters `
  --out src\HajimaoDesktopShop.Rendering\Assets\PixelArt\market-atlas.png `
  --preview docs\assets\v0.1.10-market-atlas-preview.png
```

For a completely new atlas, omit `--base-atlas` and
`--normalized-characters`, and provide `--shelves` and `--products` instead.
