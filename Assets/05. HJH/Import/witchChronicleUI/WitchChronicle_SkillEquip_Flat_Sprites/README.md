# Witch Chronicle Skill Equip UI - Flat Style Sprite Pack

The character portraits, skill icons, and elemental icons are intentionally excluded.

## Unity import
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Mesh Type: Full Rect
- Filter Mode: Bilinear
- Compression: None for UI
- Alpha Is Transparency: On
- For entries marked `Sliced`, set Image Type to Sliced and copy the integer from `unity_9slice_border` in `manifest.json` to Left/Right/Top/Bottom.

## Recommended hierarchy/layering
- Background Plate
- Section/Panel BG
- Content (portrait or skill icon)
- Frame
- Selection Frame / Status Tag / TMP text

Use TMP for slot numbers, Korean labels, the plus sign if preferred, and dynamic `{character} 장착중` text. Separate PNGs are supplied for visual state layers so sprites can be reused at different sizes.
