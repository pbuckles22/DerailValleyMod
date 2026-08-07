# Product — Yard Master Suite

Label & behavior details for the Diagnostic HUD. Story checkboxes live in [PM_PLAN.md](../../PM_PLAN.md) (`Epic N` / `N.M`).

**Latest foot-nav / AR smoke notes:** [UX_SMOKE_FEEDBACK_2026-07-23.md](UX_SMOKE_FEEDBACK_2026-07-23.md) (fix bundles **B → A → C → D**; screenshots in `ux-smoke-2026-07-23/`).

---

## Goal

A **Fleet Operator** utility suite for *Derail Valley*: automate the tedium, preserve the simulation, prioritize stability.

**MVP:** Epic 1 — Diagnostic HUD (situational awareness) before Governors (Epic 2).

---

## Diagnostic HUD — labels

**Naming rule:** short plain-English word first (`Speed`, `Pipe`, …). No cryptic abbreviations. Units may follow the value. Unknown = `— Word` *(when the segment is shown)*.

### Usable train (yard rule)

Continuous full links from the target car to a loco:

- mechanical + chain tightened (either side)
- air hose + cocks open both sides
- MU blue wires only when **both** ends have MU (loco↔loco)

Loco↔freight does not require MU. Incomplete link = not “drivable” for HUD.  
**Target** = look-at preferred, standing fallback.

### Top bar — loco cab gadgets

Center-weighted IA (**4.7**): bar is **horizontally centered**; mid-string = Speed · Limit.

`Fuel · Oil · Mass · Grade · Load · Rev · Throttle · Indy · TrainBrake · Speed · Limit · Motors · Handbrakes · Cars`

| | |
|--|--|
| **Show when** | Player is **in/on** a usable loco train (standing/`PlayerManager.Car`). Look-at alone does **not** open the loco bar (perf: avoids cold Speed/Limit scan). |
| **Hide when** | Sky, ground, freight-only / no loco path — **no** red dash wall |
| **Story** | **4.3** hide + **4.7** IA |

| Word | Example live | Notes |
|------|----------------|-------|
| Fuel | `Fuel 67 %` | yellow if Fuel or Oil &lt; 20%; red if either &lt; 5% — **1.9** |
| Oil | `Oil 55 %` | yellow if Fuel or Oil &lt; 20%; red if either &lt; 5% — **1.9** |
| Mass | `Mass 240 t` | **1.2** |
| Grade | `Grade +1.2 %` | **1.2** |
| Load | `Load 42 %` | amps / max; yellow ≥80%, red ≥95% — **1.7** |
| Throttle | `Throttle 42 %` | Cab throttle lever position (0–100%) |
| Indy | `Indy 10 %` | Independent brake lever position |
| TrainBrake | `TrainBrake 35 %` | Train/auto brake lever — not advisory `Brake N in …` |
| Speed | `Speed 36 km/h` | **1.1** — visual center with Limit |
| Limit | `Limit 60` | **one** badge — **1.10**; ↑↓ — **1.11** |
| Motors | `Motors OK` / `Hot` / `Dead` | **1.8** current-state only |
| Handbrakes | `Handbrakes 3` | **1.4** |
| Cars | `Cars 5` | freight only — **1.4** |

### Always-on chips (not loco gadgets)

**Centered** under the lowest other bar (or alone at top). Draw **only in an active world session** — never on the launcher / main menu ([hud-in-world-only](../../.cursor/rules/hud-in-world-only.mdc)).

| Word | Example live | Notes |
|------|----------------|-------|
| Heading | `Heading NE` / `Heading ENE` | **1.12** — 16-point rose; no degrees; never `— Heading` on menus |
| Station | `Curr. Area - MF` | **4.6** — yard id only in zone; no bearing/meters (AR has distance) |
| Marked | `Marked NE 84m` / `Marked here` | **1.14** — return bearing + distance; omit when unmarked |
| Clock | `Clock 14:30` | In-game world time (`DateTimeWrapper`); world session only |
| Version | *(removed from HUD)* | Confirm ship # in **UMM Mod Manager** / `info.json` only |

*(No mod version chip on the always-on bar.)*

Always-on is a full HUD bar (same chrome as loco/look-at).

### Second bar — that car only

Look-at wins; standing fallback when crosshair is not on a car. Hidden when no target.
Stack: **always-on (top)** → loco cab → look-at → job.

| Word | Example live | Notes |
|------|----------------|-------|
| Handbrake | `Handbrake 1` | omit Pipe (**0.6.35**) |
| Couplers | `F-` / `F+` … | same marks as before |
| Job | `Job FH-12` | omit when none |
| Track | `Track SM-O6I` | omit when unknown |
| Identity | `Loco DE2 · 38t · train 184t` / `46t · train 184t` | mass folded; no Car XX/N/A; no cargo type |

**Removed from second bar (0.6.35):** Pipe · Car # · cargo type name.

**Build order (power):** **1.7**–**1.9** done → **1.10** speed-limit alerts (grade already in **1.2**).

**1.10 / 1.11 / 1.17 notes:** `Limit N | Next M (distance)` — posted sticky + look-ahead only. No Recommended, no `(Posted)` label, no soft Brake chip, no geometry-ahead boards. World board index keeps discovered signs after stream-out. Limit caution: yellow **Limit−10 through Limit+5**, red above **Limit+5**. **1.16 Recommended** and soft Brake HUD **CUT**. Hidden with the top bar (**4.3**).

**1.12 notes:** Personal compass only — not part of `TrainHudLine`. Shown on the always-on bar **only while a world session is active** (player present). Source = look direction (`ActiveCamera`, else `PlayerTransform`); Unity world +Z = N. Display = 16-point abbreviations only (`N`, `NNE`, `NE`, `ENE`, …) — never degrees. Do not paint `— Heading` on the launcher. Mod version is not shown beside Heading — use Mod Manager.

**1.13 notes:** Was always-on `Pos x, z`. **Bundle B.1** removes Pos from the HUD; `T2 pos` debug remains.

**1.14 notes:** `Home` sets/updates session mark at player XZ; `Shift+Home` clears (cleared on mod disable/unload). Chip = 16-point bearing toward mark + integer meters (`Marked NE 84m`), or `Marked here` within 1 m. Not persisted.

**4.4 notes:** Second-bar `Track SM-O6I` from `logicCar.CurrentTrack.ID.FullDisplayID`. **Bundle B.3:** omit the segment when unknown / generic mainline (no `— Track`).

**4.5 notes:** **Cut** (2026-07-25). Former fluid-gated `Next: Name [N km]` (nearest other yard) removed from loco bar — wrong UX for mainland range. Fluid HUD debug inject kept (F8/F9 / Shift+F8 clear / Shift+F9 both 100%).

**4.6 notes:** In job-generation zone — always-on `Curr. Area - SM` (yard id only). Bearing/meters on office AR. Omit outside zones.

**4.8 notes:** **Primary:** prep-before-validate — `currentJobs` empty + `availableJobs` → `Preview Nm` to `destroyGeneratedJobsSqrDistanceRegular` (warn / `OUT`). Taken jobs — `Job ID · GO/HOLD/RED · Bonus` (consist vs task cars; purple ■ AR on job cars @ **0.6.16**). Abandoned/Expired → red `Cancelled`. Details: [UX_SMOKE_FEEDBACK_2026-07-23.md](UX_SMOKE_FEEDBACK_2026-07-23.md) Bundle D.

**4.9 notes:** AR screen markers with PNG icons (loco / house / pin) under `Mods/.../Icons/`; tint color secondary. Edge-clamped when behind; caption = meters only. `T2 ar`. **Office proximity (A.4 / Bundle C):** one gate hides the house icon; Station chip is yard-only (`Curr. Area - …`). Being ~12–14 m from the door (apron) is **accepted** for house hide.

**4.10 notes:** Other-loco AR — max **2**, **≤600 m**, **only in station/job zone**; refresh **1 s**. UMM toggle.

**4.11 notes:** Rear-camera chip — check-point clearance, tenths. **Green** ≤0.5 m + couple-scan; **yellow** through **30 m**; plain farther. No “Couple ready”. Clearance can vary by car coupler geometry (fixed −0.25 m inset, not per-type table).

**4.12 notes:** DV reverser gate (neutral = 0.5). **Reverse** → `Rear`; **Forward** → `Front` (same colors/ranging); **Neutral** → omit. Open tip with no target within ~80 m → `Front —` / `Rear —` (not omit). After couple, tip moves to the new free end.

**1.15 notes:** Loco-bar `MU idle` (yellow) / `MU desync` (red). Quiet when synced. Yellow if either unit off/Neutral (brakes match). Red on brake/ind-brake fight or both on+in-gear control mismatch. MU cable syncs reverser/throttle — unplug to test desync.


**4.7 notes:** All HUD rows centered. Stack: **always-on (top, stable)** → loco (if any) → look-at (if any) → active job. Chip order on loco bar as above.

---

## Non-goals

- Full autopilot / replacing the need to drive
- Deleting cars or jobs to “clear” yards
- Harmony Transpilers unless Prefix/Postfix cannot solve the problem
- Pulling parking-lot features into active phases

---

## Core pillars

1. **Teleportation is the last resort** — Never delete. Teleport only after verification.
2. **Governor vs Monitor**
   - **Monitors (read-only):** Epic 1 HUD — integrity **1.3–1.6**, power **1.7–1.9**, terrain **1.10**. Motors (**1.8**) reports **current** thermal/fuse state only — not a driver-prediction tool.
   - **Governors (active):** Epic 2 — thermal / auto-brake via gated soft writes. **2.2** soft-rolls throttle toward **75%** (Warning) / **55%** (Critical) while Motors is Hot (`Throttle.Set` + ThreeGate). **2.3** on engine on→off soft-rolls **train + independent** to full and **throttle to idle** (`Brake` / `IndependentBrake` / `Throttle.Set` + ThreeGate); never auto-releases on start; coast with engine left on. Do not push prediction into the HUD.
3. **Stability first** — Epic 0 Safe Boot before UI or logic manipulation

---

## Three-Gate pattern (all state writes)

Shared helper: `YardMasterSuite.Core.ThreeGate.TryApply` (**2.1**, v0.4.81).

1. **Integrity Gate** — safe for current world/consist?
2. **State Registry Gate** — managers/objects present and expected?
3. **Safety Gate** — governor constraints (e.g. stationary); non-governor callers pass `true`
4. **Soft Write** — minimal change; abort closed on `false` or throw

Fail closed: no write unless every gate passes.

---

## Roadmap (epics)

| Epic | Name | Intent |
|------|------|--------|
| **0** | Foundation / Safe Boot | Empty UMM mod; fail-closed — **mostly done** |
| **1** | Diagnostic HUD *(HIGH · Stage 1)* | Integrity → Power → Terrain alerts |
| **2** | Governor Mode *(MEDIUM)* | Three-Gate → Thermal → Auto-Brake |
| **3** | Yard Master / Dispatcher *(Stages 2–3)* | **3.5** Align *(done)*; **3.6** Switch List *(done @ 0.6.2)*; next **3.7** multi-step Maps; **3.1b iced**; **3.2 cut** |
| **4** | HUD quality | Targeting, cargo, hide gadget bar (**4.3**) |
| **5** | Digital Catalog | Order keys/flags/tools to player (**5.1**) |

Stories: [PM_PLAN.md](../../PM_PLAN.md).

---

## Parking lot

See PM_PLAN → *Parking lot* (Switch Path Tracer, Anti-Wheelslip, Startup Assist, …).

---

## Testing philosophy

Use in-game Dev Tools (Comms Radio / Sandbox / Spawner). Do not burn development time on manual travel. Checklists: [TEST_PLAN.md](../../TEST_PLAN.md).

---

## Source

Master Project Context v3.0 + Developer Roadmap v3.0 (2026-07); backlog IDs normalized to `Epic N` / `N.M` (2026-07-18).
