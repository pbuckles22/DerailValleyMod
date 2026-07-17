# DESIGN_SYSTEM — DerailValleyMod

Monitor HUD (IMGUI) rules for *Yard Master Suite*. Product inventory and naming: [doc/requirements/product.md](../../doc/requirements/product.md) → *Diagnostic HUD — labels*.

## HUD layout

- **Main strip:** one top-left bar; segments join left → right with `  |  `.
- **Look-at (CMD-01d):** a **second HUD bar** directly under the main strip — same height, width fits its text; gone when not looking at a car.
- Grow bar width from content (`GUI.skin.box.CalcSize`); do not clip labels.

## HUD label wording

- Leading plain-English word (`Speed`, `Pipe`, `Handbrake`, `Couplers`, …).
- No cryptic abbreviations (`HB`, `cpl`, bare `%` / `t` alone).
- Unknown: `— <Word>` (same word as live).
- New segments must follow this rule before merge.

## Motion

- N/A for current IMGUI strip (no animation yet).
