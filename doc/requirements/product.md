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

`Fuel · Oil · Mass · Grade · Load · Speed · Limit · Motors · Handbrakes · Cars`

| | |
|--|--|
| **Show when** | Target is on a **usable loco train** (in/on it, looking at the loco, or looking at a car linked to one) |
| **Hide when** | Sky, ground, freight-only / no loco path — **no** red dash wall |
| **Story** | **4.3** hide + **4.7** IA |

| Word | Example live | Notes |
|------|----------------|-------|
| Fuel | `Fuel 67 %` | yellow if Fuel or Oil &lt; 20%; red if either &lt; 5% — **1.9** |
| Oil | `Oil 55 %` | yellow if Fuel or Oil &lt; 20%; red if either &lt; 5% — **1.9** |
| Mass | `Mass 240 t` | **1.2** |
| Grade | `Grade +1.2 %` | **1.2** |
| Load | `Load 42 %` | amps / max; yellow ≥80%, red ≥95% — **1.7** |
| Speed | `Speed 36 km/h` | **1.1** — visual center with Limit |
| Limit | `Limit 60` | **one** badge — **1.10**; ↑↓ — **1.11** |
| Motors | `Motors OK` / `Hot` / `Dead` | **1.8** current-state only |
| Handbrakes | `Handbrakes 3` | **1.4** |
| Cars | `Cars 5` | freight only — **1.4** |

### Always-on chips (not loco gadgets)

Top-left; independent of **4.3**.

| Word | Example live | Notes |
|------|----------------|-------|
| Version | `v0.4.25` | Deploy confirm |
| Heading | `Heading NE` / `Heading ENE` | **1.12** — 16-point rose; no degrees |
| Pos | *(removed from always-on — Bundle B.1)* | Was **1.13**; coords still available via `T2 pos` debug |
| Marked | `Marked NE 84m` / `Marked here` | **1.14** — return bearing + distance; omit when unmarked |

Always-on is a full HUD bar (same chrome as loco/look-at), **centered**, stacked **under** the lowest other bar (or alone at top when those are hidden).

### Second bar — that car only

Look-at wins; standing fallback when crosshair is not on a car. Hidden when no target.

| Word | Example live | Unknown / omit |
|------|----------------|----------------|
| Pipe | `Pipe 2.0 bar` | `— Pipe` |
| Handbrake | `Handbrake 1` | `— Handbrake` |
| Couplers | `F+` usable · plain `F*` loose · `F-` open · yellow `F*` MU open | `— Couplers` |
| Car | `Car 3` · `Car N/A` on loco · `Car XX` if not usable train | — |
| Job | `Job FH-12` | `— Job` |
| Track | `Track SM-O6I` | `— Track` — **4.4** |
| Cargo | `Cargo Steel Rails` | `Empty Cargo` — **4.2** |
| Loco | `Loco DE6` | *(omit if not a loco)* — **1.6** |

**Build order (power):** **1.7**–**1.9** done → **1.10** speed-limit alerts (grade already in **1.2**).

**1.10 / 1.11 notes:** Single `Limit` badge (never two limit numbers). **1.10** = current governing limit — prefer posted boards (digit × 10); geometry / SignPlacer ladder is fallback only. Yellow within 5 km/h of limit (including at limit); red when over. **1.11** = next limit along the path with ↑ (green) / ↓ (warn) on that same badge — no GPS strip reorder. Hidden with the top bar (**4.3**).

**1.12 notes:** Personal compass only — not part of `TrainHudLine`. Always visible beside version. Source = look direction (`ActiveCamera`, else `PlayerTransform`); Unity world +Z = N. Display = 16-point abbreviations only (`N`, `NNE`, `NE`, `ENE`, …) — never degrees.

**1.13 notes:** Was always-on `Pos x, z`. **Bundle B.1** removes Pos from the HUD; `T2 pos` debug remains.

**1.14 notes:** `Home` sets/updates session mark at player XZ; `Shift+Home` clears (cleared on mod disable/unload). Chip = 16-point bearing toward mark + integer meters (`Marked NE 84m`), or `Marked here` within 1 m. Not persisted.

**4.4 notes:** Second-bar `Track SM-O6I` from `logicCar.CurrentTrack.ID.FullDisplayID`; `— Track` when unknown / generic mainline.

**4.5 notes:** When Fuel or Oil is in yellow/red, optional loco-bar `Next: Name [N.N km]` using `JobPaymentCalculator.GetDistanceBetweenStations` from current zone/yard station to nearest other. Omit when fluids OK or path/station unknown.

**4.6 notes:** In job-generation zone — always-on `Station SM NE 84m · x, z` using the **station office** transform (not yard center). Omit outside zones.

**4.8 notes:** Taken jobs — centered bar `Job ID · Bonus m:ss` only (no Zone meters; destroy radius does not cancel taken jobs). Abandoned/Expired → red `Cancelled`. Prep-before-validate — when available jobs exist and none taken, optional `Preview Nm` to `destroyGeneratedJobsSqrDistanceRegular` (warn near edge / `OUT`). Details: [UX_SMOKE_FEEDBACK_2026-07-23.md](UX_SMOKE_FEEDBACK_2026-07-23.md) Bundle D.

**4.9 notes:** AR screen markers with PNG icons (loco / house / pin) under `Mods/.../Icons/`; tint color secondary. Edge-clamped when behind; caption = meters only. `T2 ar`.

**4.7 notes:** All HUD rows centered. Stack: loco (if any) → look-at (if any) → active job (if any) → always-on nav. Chip order on loco bar as above.

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
   - **Governors (active):** Epic 2 — thermal / auto-brake via gated soft writes. **2.2** soft-scales throttle when Motors is Hot; do not push prediction into the HUD.
3. **Stability first** — Epic 0 Safe Boot before UI or logic manipulation

---

## Three-Gate pattern (all state writes)

1. **Integrity Gate** — safe for current world/consist?
2. **State Registry Gate** — managers/objects present and expected?
3. **Soft Write** — minimal change; abort closed on failure

Governors also need **safety gates** (e.g. stationary) before writing. Shared helper = **2.1**.

---

## Roadmap (epics)

| Epic | Name | Intent |
|------|------|--------|
| **0** | Foundation / Safe Boot | Empty UMM mod; fail-closed — **mostly done** |
| **1** | Diagnostic HUD *(HIGH · Stage 1)* | Integrity → Power → Terrain alerts |
| **2** | Governor Mode *(MEDIUM)* | Three-Gate → Thermal → Auto-Brake |
| **3** | Yard Master / Dispatcher *(Stages 2–3)* | **3.3–3.5** switch/path; **3.1** teleport |
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
