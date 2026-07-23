# Technical debt



Durable ranked backlog. Handoff notes may mention debt; **promote** anything that persists (2+ handoffs or blocks work) here.



**Cadence:** every handoff → tech-debt-evaluator → “Do first” in the note → promote here when sticky.



**Last full pass:** Epic 4 WIP handoff — 2026-07-23.



---



## Fix now



Blocking, unsafe, or no-rollback.



- *(none)*



---



## Fix soon



High ROI; frequent pain; not blocking.



- [ ] **MU yellow smoke (2-loco)** — `F*` / `R*` yellow implemented; in-game smoke deferred until a second loco is available. *(pairs with **1.5**)*

- [ ] **Coupler plain `*` vs yellow `*`** — loose and MU share the glyph; HUD color distinguishes, but plain `T2 coupler` / Format strings cannot. Revisit with MU smoke — distinct debug labels or marks.

- [x] **Cache Load amp reflection by type** — landed with **1.8** (`LoadFieldCache` + `MotorSetFieldMap`).

- [ ] **Re-smoke Load yellow/red** — **1.7** live `%` **PASS\***; ≥80% / ≥95% color bands not exercised in-game yet. *(Debug fluid-force path removed — no documented override for forced low tanks.)*

- [x] **Re-smoke Motors Hot/Dead** — **1.8** Tier 2 **PASS** (OK / brief Hot / Dead).

- [x] **Motors Hot dwell (HUD)** — **cut** (2026-07-20). Monitor stays current-state; thermal mitigation → **Epic 2.2** governor.

- [ ] **Epic 4 uncommitted WIP** — ~~stacked on feature branch~~ **landed on `main` @ v0.4.29** (2026-07-23 stopgap). Remaining: UX fix bundles **B → A → C → D** per [UX_SMOKE_FEEDBACK_2026-07-23.md](doc/requirements/UX_SMOKE_FEEDBACK_2026-07-23.md); one-story-one-ship from here.

- [ ] **Cache speed-limit state per HUD tick** — `TryGetSpeedLimitState` / board scan can run twice per refresh (train bar + `T2 limit`). Cache in `BeginHudTick` like standing/loco.

- [ ] **Cache player XZ per HUD tick** — `TryGetPlayerPosition` re-runs from Pos + Marked labels and both T2 snapshots each refresh (**1.12–1.14**). Cache in `BeginHudTick` like standing/loco. Epic 4 multiplied consumers (station/AR).

- [ ] **Extract TelemetryReader subsystems** — ~**2k** LOC (was ~1512; Epic 4 added station/next/job/AR/track). Split when editing (limit scan first; nav/park/station/job/AR next).

- [x] **`package.ps1` stale `build/` deploy** — script now rebuilds before pack; Release PostBuild passes `-Configuration Release` (v0.4.23).



---



## Accept for now



Isolated + workaround + revisit trigger.



- [ ] **Core sources linked into `YardMasterSuite.dll`** — Unity Mono failed to load sibling `YardMasterSuite.Core.dll`; csproj compiles Core `*.cs` into the mod assembly. *Revisit if UMM/Unity can load a sibling Core DLL.*

- [ ] **Dead integrity Tier-2 helpers** — `CurrentIntegrityDebugSnapshot` / `Tier2IntegrityDebug` superseded by `T2 consist` / `local-car` / `look-at` / `coupler`. Delete on a cleanup pass.

- [x] **Per-tick target cache** — standing / look-at / target / loco cached per HUD refresh *(landed with **1.7** WIP)*. Re-open only if profiling shows leftover cost.

- [ ] **Private TractionMotorSet reflection** — `MotorSetFieldMap` reads private field names; pin typed/public ports after more loco smoke or if a DV patch breaks them.

- [ ] **MonitorHudDriver Tier 2 emit boilerplate** — **12** near-identical `Emit*DebugIfNeeded` blocks (Epic 4 channels). Extract shared previous-snapshot + log helper before the next T2 channel.

- [ ] **Unused `ParkMarkDisplay.FormatCoords`** — HUD uses `FormatReturn` only; delete or wire on next mark/display edit.



---



## ROI rubric (quick)



Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse). Sort descending.


