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
| **`feature/cmd-01-integrity-monitor`** | CMD-01a **Done** (v0.4.0) — merge to `main`, then start CMD-01b |
| **`main`** | Integration (`1b817ed` — E1-S2 + CMD backlog) |

---

## Completed

- Phase 0 Safe Boot (UMM load + toggle)
- E1-S1 speed HUD (km/h); E1-S2 grade % + consist tonnes (L→R); Tier 1 + Tier 2 green
- CMD backlog adopted (CMD-01…06); Switch Path Tracer / Startup Assist parked
- **CMD-01a** car integrity (Pipe / Handbrake 0–1 / Couplers on car under feet); dual-HUD checks deferred to 01b/01d

---

## Next up

- Merge `feature/cmd-01-integrity-monitor` → `main`
- **CMD-01b** train top HUD + local-car second bar (Cars N, Handbrakes total, red/null when no loco)
- **CMD-01d** look-at fills second bar on foot → **01c** tight/loose
- Then CMD-02 Power Monitor; finish CMD-03 speed-limit alerts

---

## Reading order

1. [CONTRIBUTING.md](../CONTRIBUTING.md)
2. This file
3. [requirements/product.md](requirements/product.md)
4. [PM_PLAN.md](../PM_PLAN.md)
5. [requirements/modding.md](requirements/modding.md) + [DEV_GUIDE](../.cursor/skills/DEV_GUIDE.md)
