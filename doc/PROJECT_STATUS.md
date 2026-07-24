# Project status

Human-readable **snapshot**. Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* and checkbox truth in [PM_PLAN.md](../PM_PLAN.md).

**Last updated:** 2026-07-24

---

## Summary

**Yard Master Suite** — Fleet Operator utilities for *Derail Valley*  
Stack: Unity / C# `net48` / UMM / Harmony  

| | |
|--|--|
| **Journey** | Stage 1 — Apprentice (“don’t blow up”) |
| **MVP** | Epic 1 System Monitor HUD |
| **Version (`main`)** | **0.4.52** *(Bundle D Preview prep PASS)* |

---

## Active work

| Branch | Role |
|--------|------|
| **`main`** | Integration — Bundle D shipped; next license-warn or Bundle E deferrals |

---

## Progress (official IDs)

### Epics

- [x] **Epic 0** — Foundation & Safe Boot *(0.4 scaffold remaining)*
- [x] **Epic 1** — Diagnostic HUD *(Stage 1 — complete 2026-07-23)*
- [ ] **Epic 2** — Governor Mode
- [ ] **Epic 3** — Yard Master / Dispatcher
- [ ] **Epic 4** — HUD quality *(4.4–4.9 + Bundles B/A/C/D on `main`; 4.5 + license-warn open)*
- [ ] **Epic 5** — Digital Catalog

### Next (Stage 1 leftovers / Stage 2)

- [ ] **Follow-up** — warn on overview if player lacks required licenses
- [ ] Re-smoke **1.7** yellow/red Load when practical
- [ ] **2.1** Three-Gate → Epic 2

### Deferred smokes / known issues

- [ ] **A.4 house AR hide** — outdoor false-positive ~12–14 m from Station Office door (SM); loco hide PASS @ v0.4.47. See `TECH_DEBT.md`.
- [ ] Yellow MU `F*` / `R*` with two locos
- [ ] `Empty Cargo` wording (**4.2**)
- [ ] Load ≥80% / ≥95% colors (**1.7**)

### Cut / moved

- [x] **1.8 Hot dwell / predictive HUD** — **cut**. Monitor stays current-state; thermal management → **Epic 2**.

---

## Recently completed

- [x] **Bundle D** — inventory-gated Preview Regular edge; taken Job+Bonus; Cancelled (v0.4.52, Tier 2 **PASS**)
- [x] **Bundle C** — Station `here` shares A.4 office gate (v0.4.48, Tier 2 **PASS**)
- [x] **A.4** Proximity hide — loco in-cab PASS; house hide deferred known issue (v0.4.47)
- [x] **A.1–A.3** Behind-camera, sticky row, on-object/sticky + edge fan (through v0.4.43)
- [x] **Bundle B** clutter diet through v0.4.36
- [x] **1.14** Mark / return — `Home` / `Shift+Home`; `Marked NE Nm` chip; `T2 mark` (v0.4.25, Tier 2 **PASS**)
- [x] **Epic 1** closed — Diagnostic HUD complete

---

## Reading order

1. [AGENT_HANDOFF.md](../AGENT_HANDOFF.md)
2. [PM_PLAN.md](../PM_PLAN.md)
3. [doc/requirements/UX_SMOKE_FEEDBACK_2026-07-23.md](requirements/UX_SMOKE_FEEDBACK_2026-07-23.md)
