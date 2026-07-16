# Project status

**Human-readable current state.** Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* when milestones ship.

**Last updated:** 2026-07-16

---

## Summary

**DerailValleyMod** — *Yard Master Suite*: Fleet Operator utilities for Derail Valley. Stack: Unity / C# `net48` / UMM / Harmony. Plan: Safe Boot → Monitor HUD → Governors → Yard Master.

---

## Active branch

| Branch | Role |
|--------|------|
| **`main`** | Integration branch |
| **`feature/e0-safe-boot`** | Phase 0 scaffolding (WIP) |

---

## Completed

- Agentic rules, skills, handoff protocol
- Private GitHub repo: https://github.com/pbuckles22/DerailValleyMod
- Product plan locked to **v3.0** (Monitor / Governor / Yard Master + parking lot)
- Canonical docs: [requirements/product.md](requirements/product.md), [requirements/modding.md](requirements/modding.md), [PM_PLAN.md](../PM_PLAN.md)
- Phase 0 scaffold: template-umm layout (`YardMasterSuite/`, `info.json`, `package.ps1`); `dotnet build YardMasterSuite.sln` green
- Phase 0 in-game smoke: Yard Master Suite Active in UMM; toggle off/on works

---

## Next up

- Commit/merge `feature/e0-safe-boot`
- Phase 1 Monitor HUD (read-only)

---

## Reading order

1. [CONTRIBUTING.md](../CONTRIBUTING.md)
2. This file
3. [requirements/product.md](requirements/product.md)
4. [PM_PLAN.md](../PM_PLAN.md)
5. [requirements/modding.md](requirements/modding.md) + [DEV_GUIDE](../.cursor/skills/DEV_GUIDE.md)
