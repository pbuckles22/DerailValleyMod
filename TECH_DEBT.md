# Technical debt



Durable ranked backlog. Handoff notes may mention debt; **promote** anything that persists (2+ handoffs or blocks work) here.



**Cadence:** every handoff → tech-debt-evaluator → “Do first” in the note → promote here when sticky.



**Last full pass:** 3.1 ship / handoff — 2026-08-03.



---



## Fix now



Blocking, unsafe, or no-rollback.



- *(none)*



---



## Fix soon



High ROI; frequent pain; not blocking.



- [ ] **3.1 place ghost / facing cue** — MVP chip-only; players can’t preview landing until Confirm. Add re-rail-style ghost (and Flip cue) on look-at aim.

- [ ] **3.1 Snap office under-mesh** — Station Snap can drop the player under the office floor / into water; needs a known-good office spawn (or offset up).

- [x] **MU yellow smoke (2-loco)** — Bundle E **#23** **PASS** @ **0.4.80** (red / yellow / white / blue scheme; MU via `muModule`).

- [x] **Coupler plain `*` vs yellow `*`** — plain/log uses `*Y` for MU-open; HUD color distinguishes red mid-couple vs yellow MU.

- [x] **Cache Load amp reflection by type** — landed with **1.8** (`LoadFieldCache` + `MotorSetFieldMap`).

- [x] **Re-smoke Load yellow/red** — F6 per-loco force **PASS** path @ v0.4.64+ (was F10). Live hard-pull (**#22**) **waived** 2026-07-26 — confirm in normal play if colors misbehave.

- [x] **Cargo dump mass/visual desync** — fixed @ 0.4.66: F7 uses `UnloadCargo`/`LoadCargo` (events) not `DumpCargo`.

- [x] **Re-smoke Motors Hot/Dead** — **1.8** Tier 2 **PASS** (OK / brief Hot / Dead).

- [x] **Motors Hot dwell (HUD)** — **cut** (2026-07-20). Monitor stays current-state; thermal mitigation → **Epic 2.2** governor.

- [x] **Bundle B clutter diet** — **B.1–B.4** + in-world HUD + no version chip through **v0.4.36** (smoke PASS). Remaining UX: **A → C → D**.

- [x] **3.5 Align Route: threw N but Path still wrong** — post-Align `ReevaluateAlong` + flip dedupe; dual-branch same-junction fix @ **0.5.100+**. Tier 2 **PASS** @ **0.5.101**.

- [x] **3.5 Align: intermediate yard thru-tracks (#4)** — Through-only + occupancy + `#Y` alias yard map @ **0.5.101**. Tier 2 **PASS** (HB Path OK). **Waived:** staged “no free I→O ⇒ skip whole city” — reopen if seen in play. Product locks Tier-1.

- [x] **3.5 Path stale** — corridor leave + planned-switch flip → stale @ **0.5.101**. Tier 2 **PASS**.

- [x] **3.5 Align: ETA** — schedule lag + arrival clamp; trip% from ETA. Tier 2 **PASS** @ **0.5.101**.

- [x] **3.5 Insert mapping freeze (#1)** — frame-pump + toast @ **0.5.80**. Tier 2 **PASS**.

- [x] **1.17 Posted + Next** — Recommended/Brake/geometry-ahead CUT; Limit + Next only @ **0.5.104**. Tier 2 **PASS** (2026-08-03). **Ice:** #4 blind behind-seed — reopen if Limit empty with posted board <600 m behind.

- [x] **Stress RAG HUD chip** — train-bar `Stress N %` RAG @ **0.5.105**. Tier 2 **PASS** (2026-08-03). Coupler/derail physics only — not consist-in-zone.

- [ ] **1.17 #4 behind-seed polish** — world index seed path; empty SignDebug may reuse stale PathAhead (code-review WARN). Only if smoke shows empty Limit with board behind.

- [ ] **UX: Flight-sim style always-on HUD** — parked (player ref 2026-08-01). Clean corner telemetry (airspeed/engines/fuel AOA vs flaps/trim/alt/VS), destination progress bar + ETA, not a dense top strip. Revisit with ui-ux / DESIGN_SYSTEM when HUD layout is reopened (post–3.5). Ref: flight-sim HUD screenshot in session.

- [ ] **AR: hide house outside office / city sense** — parked. `Station HMB … 1600m` + house icon still shown far from office; player expects hide once outside city/office. Today house hide = office AABB only (`ArProximityHide`), Station chip = nearest station (not “home city limits”). Clarify product: nearest-station always-on vs office-only house.

- [x] **AR: other-loco range ≤600 m** — done @ **0.6.15** (`LocoRadarSelection.MaxRangeMeters`). Was parked as ≤1 km; yard-walk cap is 600 m (0–3 markers).

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

- [x] **A.4 house AR hide / Station `here`** — **Accepted** 2026-07-26: same office gate hides house icon and shows `Station … here`. Apron ~12–14 m from door flipping to `here` is intentional “at office,” not a bug. No further bounds tuning. Loco in-cab hide **PASS** @ v0.4.47; Bundle C **PASS** @ v0.4.48.

- [x] **2.2 Thermal Hot trigger** — Hot follows cab MU Warning/Critical (`MUChainTemperatureState`); soft-roll Warning **75%** / Critical **55%**. **PASS** @ **0.5.15**.

- [ ] **4.11 backup `inCoupleRange` from long-range scan** — after `GetFirstCouplerInRange` misses, ray/overlap hit ≤1.5 m can set couple-near and allow green without game couple-scan. Smoke @ **0.5.12** did not show false green; gate green to game scan only if it appears. Area: `TelemetryReader.TryGetBackupProximity`.

- [x] **License warn on held overview** — **Done v0.4.53** (Tier 2 **PASS**): red `No license: FH` / `SH` / etc. while holding overview/booklet whose licenses are missing; joins Preview when both apply.

- [ ] **Preview inventory scan cadence** — Bundle D scans inventory/equip each HUD tick for JobOverview/JobBooklet (license warn reuses same scan). Acceptable for now; throttle or event-driven refresh if profiling shows cost. Area: `TelemetryReader.TryGetJobsFromPlayerInventory`.



---



## ROI rubric (quick)



Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse). Sort descending.


