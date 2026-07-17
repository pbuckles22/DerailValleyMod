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
| **`feature/cmd-01-integrity-monitor`** | CMD-01a Integrity (v0.4.0) — Tier 1 + `T2 integrity` Player.log debug; finish in-game log sign-off then merge |
| **`main`** | Integration (`1b817ed` — E1-S2 + CMD backlog) |

---

## Completed

- Phase 0 Safe Boot (UMM load + toggle)
- E1-S1 speed HUD (km/h); E1-S2 grade % + consist tonnes (L→R); Tier 1 + Tier 2 green
- CMD backlog adopted (CMD-01…06); Switch Path Tracer / Startup Assist parked

---

## Next up

- **CMD-01a** — redeploy with `T2 integrity` logs; sign off from Player.log; merge
- **CMD-01b** consist summary + retro `T2 consist` logging → **01d** look-at (Car # / Job # / **second HUD bar** under main + `T2 look-at`) → **01c** tight/loose
- Then CMD-02 Power Monitor; finish CMD-03 speed-limit alerts

---

## Reading order

1. [CONTRIBUTING.md](../CONTRIBUTING.md)
2. This file
3. [requirements/product.md](requirements/product.md)
4. [PM_PLAN.md](../PM_PLAN.md)
5. [requirements/modding.md](requirements/modding.md) + [DEV_GUIDE](../.cursor/skills/DEV_GUIDE.md)
