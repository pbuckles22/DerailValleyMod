# Technical debt



Durable ranked backlog. Handoff notes may mention debt; **promote** anything that persists (2+ handoffs or blocks work) here.



**Cadence:** every handoff → tech-debt-evaluator → “Do first” in the note → promote here when sticky.



**Last full pass:** Epic 2 close / **2.3** — 2026-07-28.



---



## Fix now



Blocking, unsafe, or no-rollback.



- *(none)*



---



## Fix soon



High ROI; frequent pain; not blocking.



- [x] **MU yellow smoke (2-loco)** — Bundle E **#23** **PASS** @ **0.4.80** (red / yellow / white / blue scheme; MU via `muModule`).

- [x] **Coupler plain `*` vs yellow `*`** — plain/log uses `*Y` for MU-open; HUD color distinguishes red mid-couple vs yellow MU.

- [x] **Cache Load amp reflection by type** — landed with **1.8** (`LoadFieldCache` + `MotorSetFieldMap`).

- [x] **Re-smoke Load yellow/red** — F6 per-loco force **PASS** path @ v0.4.64+ (was F10). Live hard-pull (**#22**) **waived** 2026-07-26 — confirm in normal play if colors misbehave.

- [x] **Cargo dump mass/visual desync** — fixed @ 0.4.66: F7 uses `UnloadCargo`/`LoadCargo` (events) not `DumpCargo`.

- [x] **Re-smoke Motors Hot/Dead** — **1.8** Tier 2 **PASS** (OK / brief Hot / Dead).

- [x] **Motors Hot dwell (HUD)** — **cut** (2026-07-20). Monitor stays current-state; thermal mitigation → **Epic 2.2** governor.

- [x] **Bundle B clutter diet** — **B.1–B.4** + in-world HUD + no version chip through **v0.4.36** (smoke PASS). Remaining UX: **A → C → D**.

- [ ] **3.5 Align Route: threw N but Path still wrong** — parked until **1.16** hone done. Repro ×2 @ **0.5.67**: FF→IME `IME-A1L` and HMB transit / HB (`Path 1 wrong` after `T2 align: threw N`; Facing/Reverse cues still show). Plan/check disagree, wrong branch, or incomplete throw. Area: `RouteAlignGovernor` / `PathCheck` / `DispatchDeskPanel`.

- [ ] **3.5 Align: intermediate yard thru-tracks** — parked. Dest through another city yard must prefer **pass-through / main** lanes, not industry/storage that can have cars. Player: HMB on way to HB left Path wrong / wrong track class. Ties to through-lane bias + full multi-yard corridor throw. Area: `PathFinder` / Align throw set.

- [ ] **3.5 Path stale false positive** — parked. HUD `Path stale` while player reports staying on the aligned path (straight after Align). Stale is only supposed to fire when current track ∉ plan (`RoutePlanService.WatchPathDrift`). Likely incomplete `TrackIds` / yard graph gaps / origin track id churn. Session `2026-07-31_067h`.

- [ ] **3.5 Align: shortest / fastest route** — parked (product goal after data). Today pathfind is not “Google Maps” — long detours (FF→HB via HMB). Need time/distance-optimal (or strongly biased) corridor before Align is trusted for long hauls.

- [ ] **AR: hide house outside office / city sense** — parked. `Station HMB … 1600m` + house icon still shown far from office; player expects hide once outside city/office. Today house hide = office AABB only (`ArProximityHide`), Station chip = nearest station (not “home city limits”). Clarify product: nearest-station always-on vs office-only house.

- [ ] **AR: other-loco range ≤1 km** — parked. Screenshots show DE2/S060/S282A markers well over 1000 m. Later: type filter dropdown + optional paid fast-travel / re-rail teleport via comms. Cap range first.

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


