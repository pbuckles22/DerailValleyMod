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



- [ ] **MU yellow smoke (2-loco)** — F11 grants all licenses (incl. MU) @ **0.4.67**; **4.10** loco radar shipped @ **0.4.75** — ready for real 2-loco couple smoke. *(pairs with **1.5** / Bundle E #23)*

- [ ] **Coupler plain `*` vs yellow `*`** — loose and MU share the glyph; HUD color distinguishes, but plain `T2 coupler` / Format strings cannot. Revisit with MU smoke — distinct debug labels or marks.

- [x] **Cache Load amp reflection by type** — landed with **1.8** (`LoadFieldCache` + `MotorSetFieldMap`).

- [x] **Re-smoke Load yellow/red** — F6 per-loco force **PASS** path @ v0.4.64+ (was F10).

- [x] **Cargo dump mass/visual desync** — fixed @ 0.4.66: F7 uses `UnloadCargo`/`LoadCargo` (events) not `DumpCargo`.

- [x] **Re-smoke Motors Hot/Dead** — **1.8** Tier 2 **PASS** (OK / brief Hot / Dead).

- [x] **Motors Hot dwell (HUD)** — **cut** (2026-07-20). Monitor stays current-state; thermal mitigation → **Epic 2.2** governor.

- [x] **Bundle B clutter diet** — **B.1–B.4** + in-world HUD + no version chip through **v0.4.36** (smoke PASS). Remaining UX: **A → C → D**.

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

- [ ] **A.4 house AR hide — outdoor false-positive** — @ **v0.4.47** Lobby Box, house marker can still hide ~**12–14 m** from Station Office door while outdoors (SM). Loco in-cab hide **PASS**. True “indoors” needs DV interior triggers we don’t have. *Revisit with Bundle **C** office `here` / retarget, or a later bounds pass. Area: `StationOfficeBounds`, `TelemetryReader.TryGetArStationOfficeWorldPosition`.*

- [x] **License warn on held overview** — **Done v0.4.53** (Tier 2 **PASS**): red `No license: FH` / `SH` / etc. while holding overview/booklet whose licenses are missing; joins Preview when both apply.

- [ ] **Preview inventory scan cadence** — Bundle D scans inventory/equip each HUD tick for JobOverview/JobBooklet (license warn reuses same scan). Acceptable for now; throttle or event-driven refresh if profiling shows cost. Area: `TelemetryReader.TryGetJobsFromPlayerInventory`.



---



## ROI rubric (quick)



Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse). Sort descending.


