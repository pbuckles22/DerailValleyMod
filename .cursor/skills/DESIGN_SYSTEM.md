# DESIGN_SYSTEM — DerailValleyMod

Monitor HUD (IMGUI) rules for *Yard Master Suite*. Product inventory and naming: [doc/requirements/product.md](../../doc/requirements/product.md) → *Diagnostic HUD — labels*.

## HUD layout

- **Main strip:** centered bars; segments join with `  |  `.
- **Second bar:** look-at preferred; standing fallback — same chrome; gone when no target.
- **AR markers (4.9):** screen-space glyphs over world anchors (loco / office / pin); edge-clamped when behind. Shape primary, color secondary.
- Grow bar width from content (`GUI.skin.box.CalcSize`); do not clip labels.

## HUD label wording

- Leading plain-English word (`Speed`, `Pipe`, `Handbrake`, `Couplers`, …).
- No cryptic abbreviations (`HB`, `cpl`, bare `%` / `t` alone).
- Unknown: `— <Word>` (same word as live).
- New segments must follow this rule before merge.

## Motion

- N/A for current IMGUI strip (no animation yet).
