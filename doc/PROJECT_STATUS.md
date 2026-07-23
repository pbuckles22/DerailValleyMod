# Project status

Human-readable **snapshot**. Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* and checkbox truth in [PM_PLAN.md](../PM_PLAN.md).

**Last updated:** 2026-07-23

---

## Summary

**Yard Master Suite** — Fleet Operator utilities for *Derail Valley*  
Stack: Unity / C# `net48` / UMM / Harmony  

| | |
|--|--|
| **Journey** | Stage 1 — Apprentice (“don’t blow up”) |
| **MVP** | Epic 1 System Monitor HUD |
| **Version (`main`)** | **0.4.25** |

---

## Active work

| Branch | Role |
|--------|------|
| **`main`** | Integration — **Epic 1** complete (v0.4.25) |

---

## Progress (official IDs)

### Epics

- [x] **Epic 0** — Foundation & Safe Boot *(0.4 scaffold remaining)*
- [x] **Epic 1** — Diagnostic HUD *(Stage 1 — complete 2026-07-23)*
- [ ] **Epic 2** — Governor Mode
- [ ] **Epic 3** — Yard Master / Dispatcher
- [ ] **Epic 4** — HUD quality *(4.1–4.3 + 4.7 done; 4.4–4.6 open)*
- [ ] **Epic 5** — Digital Catalog

### Next (Stage 1 leftovers / Stage 2)

- [ ] **4.6** In-zone station coords (+ foot waypoint)
- [ ] **4.4–4.5** Track ID / next station (fluids)
- [ ] Re-smoke **1.7** yellow/red Load when practical
- [ ] **2.1** Three-Gate → Epic 2

### Next (Epic 2 prep)

- [ ] **2.1** Three-Gate helper
- [ ] **2.2** Thermal governor

### Deferred smokes

- [ ] Yellow MU `F*` / `R*` with two locos
- [ ] `Empty Cargo` wording (**4.2**)
- [ ] Load ≥80% / ≥95% colors (**1.7**)

### Cut / moved

- [x] **1.8 Hot dwell / predictive HUD** — **cut**. Monitor stays current-state; thermal management → **Epic 2**.

---

## Recently completed

- [x] **1.14** Mark / return — `Home` / `Shift+Home`; `Marked NE Nm` chip; `T2 mark` (v0.4.25, Tier 2 **PASS**)
- [x] **Epic 1** closed — Diagnostic HUD complete
- [x] **4.7** + **1.11–1.13** — centered stacked HUD; Limit `^`/`v`; Heading; Pos XZ (v0.4.23)
- [x] **1.10** Speed Limit — boards + geometry fallback; GYR; quiet `T2 limit` (v0.4.20)
- [x] **1.9** Fuel/Oil % — Tier 1 + Tier 2 **PASS** (v0.4.18)
- [x] **1.8** Motors OK / Hot / Dead — Tier 1 + Tier 2 **PASS**
- [x] **4.3** Hide loco gadget top bar — **PASS**
- [x] **1.7** Load % — **PASS\***
- [x] **1.1–1.6** Speed, grade/mass, integrity, couplers, look-at
- [x] **4.1–4.2** Spherecast + cargo

---

## Reading order

1. [CONTRIBUTING.md](../CONTRIBUTING.md)
2. This file
3. [PM_PLAN.md](../PM_PLAN.md) — official backlog
4. [AGENT_HANDOFF.md](../AGENT_HANDOFF.md)
