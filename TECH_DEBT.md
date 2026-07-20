# Technical debt

Durable ranked backlog. Handoff notes may mention debt; **promote** anything that persists (2+ handoffs or blocks work) here.

**Cadence:** every handoff → tech-debt-evaluator → “Do first” in the note → promote here when sticky.

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
- [ ] **Re-smoke Load yellow/red** — **1.7** live `%` **PASS\***; ≥80% / ≥95% color bands not exercised in-game yet.
- [x] **Re-smoke Motors Hot/Dead** — **1.8** Tier 2 **PASS** (OK / brief Hot / Dead).
- [x] **Motors Hot dwell (HUD)** — **cut** (2026-07-20). Monitor stays current-state; thermal mitigation → **Epic 2.2** governor.

---

## Accept for now

Isolated + workaround + revisit trigger.

- [ ] **Core sources linked into `YardMasterSuite.dll`** — Unity Mono failed to load sibling `YardMasterSuite.Core.dll`; csproj compiles Core `*.cs` into the mod assembly. *Revisit if UMM/Unity can load a sibling Core DLL.*
- [ ] **Dead integrity Tier-2 helpers** — `CurrentIntegrityDebugSnapshot` / `Tier2IntegrityDebug` superseded by `T2 consist` / `local-car` / `look-at` / `coupler`. Delete on a cleanup pass.
- [x] **Per-tick target cache** — standing / look-at / target / loco cached per HUD refresh *(landed with **1.7** WIP)*. Re-open only if profiling shows leftover cost.
- [ ] **Private TractionMotorSet reflection** — `MotorSetFieldMap` reads private field names; pin typed/public ports after more loco smoke or if a DV patch breaks them.
- [ ] **Extract Load/Motors readers from TelemetryReader** — file grew with **1.7**/**1.8**; split when **1.9** lands or editing becomes painful.

---

## ROI rubric (quick)

Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse). Sort descending.
