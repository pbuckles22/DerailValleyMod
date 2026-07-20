# Product — Yard Master Suite

Label & behavior details for the Diagnostic HUD. Story checkboxes live in [PM_PLAN.md](../../PM_PLAN.md) (`Epic N` / `N.M`).

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

Modern readout of instruments you’d only see in/around a loco: Speed · Grade · Mass · Cars · Handbrakes · Load · Motors · Fuel · Oil.

| | |
|--|--|
| **Show when** | Target is on a **usable loco train** (in/on it, looking at the loco, or looking at a car linked to one) |
| **Hide when** | Sky, ground, freight-only / no loco path — **no** red dash wall |
| **Story** | **4.3** **Done** (hidden when not usable) |

| Word | Example live | Notes |
|------|----------------|-------|
| Speed | `Speed 36 km/h` | **1.1** |
| Grade | `Grade +1.2 %` | **1.2** |
| Mass | `Mass 240 t` | **1.2** |
| Cars | `Cars 5` | freight only; loco not counted — **1.4** |
| Handbrakes | `Handbrakes 3` | usable-consist applied count — **1.4** |
| Load | `Load 42 %` | amps / max; yellow ≥80%, red ≥95% — **1.7** **Done** (live % **PASS\***; color bands deferred) |
| Motors | `Motors OK` / `Hot` / `Dead` | green / yellow / red — **1.8** |
| Fuel | `Fuel 67 %` | yellow if Fuel or Oil &lt; 20% — **1.9** |
| Oil | `Oil 55 %` | yellow if Fuel or Oil &lt; 20% — **1.9** |

### Second bar — that car only

Look-at wins; standing fallback when crosshair is not on a car. Hidden when no target.

| Word | Example live | Unknown / omit |
|------|----------------|----------------|
| Pipe | `Pipe 2.0 bar` | `— Pipe` |
| Handbrake | `Handbrake 1` | `— Handbrake` |
| Couplers | `F+` usable · plain `F*` loose · `F-` open · yellow `F*` MU open | `— Couplers` |
| Car | `Car 3` · `Car N/A` on loco · `Car XX` if not usable train | — |
| Job | `Job FH-12` | `— Job` |
| Cargo | `Cargo Steel Rails` | `Empty Cargo` — **4.2** |
| Loco | `Loco DE6` | *(omit if not a loco)* — **1.6** |

**Build order (power):** **1.7** → **1.8** → **1.9**, then **1.10** speed-limit alerts (grade already in **1.2**).

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
   - **Monitors (read-only):** Epic 1 HUD — integrity **1.3–1.6**, power **1.7–1.9**, terrain **1.10**
   - **Governors (active):** Epic 2 — thermal / auto-brake via gated soft writes
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
