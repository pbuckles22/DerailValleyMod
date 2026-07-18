# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-07-18

---

## Summary

**DerailValleyMod** — *Yard Master Suite*: Fleet Operator utilities for Derail Valley. Stack: Unity / C# `net48` / UMM / Harmony. MVP: Diagnostic HUD (situational awareness), then Governors, then Yard Master teleport helpers.

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration — look-at priority + loco/cargo + QOL-06/07 (v0.4.12) |

---

## Completed

- Phase 0 Safe Boot (UMM load + toggle)
- E1-S1 speed HUD (km/h); E1-S2 grade % + consist tonnes (L→R); Tier 1 + Tier 2 green
- CMD backlog adopted (CMD-01…06); Switch Path Tracer / Startup Assist parked
- **CMD-01a–d** integrity / dual HUD / look-at / coupler tight-loose
- **Look-at priority flip** (look-at wins; standing fallback) + **`Loco …`** type
- **QOL-06** 250 m / 0.15 m spherecast — **PASS*** (slight sky-stickiness OK)
- **QOL-07** `Cargo …` / `Empty Cargo` on second bar — load **PASS**; Empty Cargo wording smoke deferred

---

## Next up

- CMD-02 Power Monitor
- Finish CMD-03 speed-limit alerts
- Tier 2: yellow MU warning with two locos (deferred)
- Smoke `Empty Cargo` next game session

---

## Reading order

1. [CONTRIBUTING.md](../CONTRIBUTING.md)
2. This file
3. [requirements/product.md](requirements/product.md)
4. [PM_PLAN.md](../PM_PLAN.md)
