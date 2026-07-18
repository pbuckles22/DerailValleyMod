# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-07-17

---

## Summary

**DerailValleyMod** — *Yard Master Suite*: Fleet Operator utilities for Derail Valley. Stack: Unity / C# `net48` / UMM / Harmony. MVP: Diagnostic HUD (situational awareness), then Governors, then Yard Master teleport helpers.

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — CMD-01d look-at shipped (v0.4.3) |

---

## Completed

- Phase 0 Safe Boot (UMM load + toggle)
- E1-S1 speed HUD (km/h); E1-S2 grade % + consist tonnes (L→R); Tier 1 + Tier 2 green
- CMD backlog adopted (CMD-01…06); Switch Path Tracer / Startup Assist parked
- **CMD-01a** car integrity (Pipe / Handbrake 0–1 / Couplers on car under feet)
- **CMD-01b** dual HUD: usable loco-train top bar + local-car second bar; usable = full link path to loco; Cars exclude loco; yellow `F*`/`R*` for open MU; `v…` HUD chip — **merged to `main`** (MU 2-loco smoke deferred)
- **CMD-01d** look-at fills second bar on foot (shared `TryGetTargetCar`; standing wins) — **merged to `main`** (v0.4.3)

---

## Next up

- **CMD-01c** coupler tight/loose display
- Tier 2: yellow MU warning with two locos (deferred from 01b)
- Then CMD-02 Power Monitor; finish CMD-03 speed-limit alerts

---

## Reading order

1. [CONTRIBUTING.md](../CONTRIBUTING.md)
2. This file
3. [requirements/product.md](requirements/product.md)
4. [PM_PLAN.md](../PM_PLAN.md)
