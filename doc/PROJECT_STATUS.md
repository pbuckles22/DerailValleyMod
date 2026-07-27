# Project status

Human-readable **snapshot**. Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* and checkbox truth in [PM_PLAN.md](../PM_PLAN.md).

**Last updated:** 2026-07-26

---

## Summary

**Yard Master Suite** — Fleet Operator utilities for *Derail Valley*  
Stack: Unity / C# `net48` / UMM / Harmony  

| | |
|--|--|
| **Journey** | Stage 1 — Apprentice (“don’t blow up”) → Stage 2 next (**2.1**) |
| **MVP** | Epic 1 System Monitor HUD |
| **Version (`main`)** | **0.5.0** *(Governor era line; next **2.2**)* |
| **WIP branch** | *(none)* |

---

## Active work

| Branch | Role |
|--------|------|
| **`main`** | Integration — **0.5.0**; **Next 2.2** thermal governor |

---

## Progress (official IDs)

### Epics

- [x] **Epic 0** — Foundation & Safe Boot *(0.4 scaffold remaining)*
- [x] **Epic 1** — Diagnostic HUD *(Stage 1 — complete 2026-07-23)*
- [ ] **Epic 2** — Governor Mode *(**2.1** @ 0.4.81 → **2.2** on **0.5.x**)*
- [ ] **Epic 3** — Yard Master / Dispatcher
- [x] **Epic 4** — HUD quality *(complete 2026-07-26 — **4.1–4.10**; Bundles B/A/C/D; Bundle E)*
- [ ] **Epic 5** — Digital Catalog

### Next (Stage 2)

- [x] **2.1** Three-Gate helper *(v0.4.81, Tier 1)*
- [ ] **2.2** Thermal governor *(first feature patches on **0.5.x**)*

### Versioning

Per-mod SemVer — see [RELEASE.md](../RELEASE.md). **0.4.x** = Monitor/HUD era (closed). **0.5.x** = Governor era (opened 2026-07-26). Features are **PATCH**es within the line.

### Accepted / closed leftovers

- [x] **4.9** AR wayfinding — closed 2026-07-26; apron → `here` + house hide = intentional (Bundle C gate)
- [x] **A.4** house / `here` proximity — **accepted** (not a false-hide bug)
- [x] Bundle E **#23** + freight/car confirm *(v0.4.80, Tier 2 **PASS**)*
- [x] **#22** Load live hard-pull — **waived** 2026-07-26
- [x] Yellow MU / Empty Cargo / Load F6 colors — **PASS** / waived as above

### Cut / moved

- [x] **1.8 Hot dwell / predictive HUD** — **cut**. Monitor stays current-state; thermal management → **Epic 2**.

---

## Recently completed

- [x] **2.1** Three-Gate — Integrity → State Registry → Safety → Soft Write; fail closed (v0.4.81, Tier 1)
- [x] **Epic 4** closed — **4.9** checkbox + A.4/`here` product acceptance; Bundle E leftovers (2026-07-26)
- [x] Bundle E **#23** — coupler HUD: red / yellow / white / blue (v0.4.80, Tier 2 **PASS**)
- [x] **4.10 Loco radar** — amber other-loco sticky AR; place `FF-A2P` / `FF #Y-…`; rigid pack (v0.4.75, Tier 2 **PASS**)
- [x] **Tier 2 debug inject** — F7 consist unload/load; F11 all licenses ↔ real; turntable PgUp/PgDn (v0.4.67, Tier 2 **PASS**)
- [x] **License warn** — `No license: FH` / `SH` on held overview when missing licenses (v0.4.53, Tier 2 **PASS**)
- [x] **Bundle D** — inventory-gated Preview Regular edge; taken Job+Bonus; Cancelled (v0.4.52, Tier 2 **PASS**)
- [x] **Bundle C** — Station `here` shares A.4 office gate (v0.4.48, Tier 2 **PASS**)
- [x] **A.4** Proximity hide — loco in-cab PASS; house/`here` gate **accepted** 2026-07-26
- [x] **A.1–A.3** Behind-camera, sticky row, on-object/sticky + edge fan (through v0.4.43)
- [x] **Bundle B** clutter diet through v0.4.36
- [x] **1.14** Mark / return — `Home` / `Shift+Home`; `Marked NE Nm` chip; `T2 mark` (v0.4.25, Tier 2 **PASS**)
- [x] **Epic 1** closed — Diagnostic HUD complete

---

## Reading order

1. [AGENT_HANDOFF.md](../AGENT_HANDOFF.md)
2. [PM_PLAN.md](../PM_PLAN.md)
3. [doc/requirements/UX_SMOKE_FEEDBACK_2026-07-23.md](requirements/UX_SMOKE_FEEDBACK_2026-07-23.md)
