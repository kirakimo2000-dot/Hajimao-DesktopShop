# DesktopShopWindow Override

- Surface: transparent outer window with a hard-edged `#17191D` store frame; avoid rounded glass effects that compete with desktop content.
- Scene priority: at least 75% of the 420×280 window; only cash, game time, stock warning, customer count, expand and lock controls remain visible.
- Pixel language: 2 px borders, 4/8 px grid, integer-aligned rectangles, no blurred scaling.
- Colors: primary `#F2B84B`, success `#69C784`, warning `#E96C65`, text `#F2F2F2`, secondary text `#B8BDC7`, panel `#23262C`.
- Interactions: labeled 40 px minimum controls, clear hover/focus states, double-click plus visible “经营” button to open management.
- Motion: actor position/state changes may crossfade in 150–200 ms; never animate window size or block drag input.
