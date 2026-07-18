# Product — Yard Master Suite

## Goal

A **Fleet Operator** utility suite for *Derail Valley*: automate the tedium, preserve the simulation, prioritize stability.

**MVP:** Situational awareness (Diagnostic HUD) before active Governors.

## Diagnostic HUD — labels

**Rule (all HUD segments, now and future):** Each readout uses a **short plain-English word** first so you can recognize it at a glance. Avoid cryptic abbreviations (`HB`, `cpl`, bare `%` / `t`). Units may follow the value (`km/h`, `bar`, `t`). Unknown = same word with an em dash (`— Pipe`).

**Shipped — two bars (CMD-01a + CMD-01b wiring):**

**Usable train (yard rule):** continuous full links from the target car to a loco. Full link = mechanical + chain tightened (either side) + air hose + cocks open both sides + MU blue wires only when **both** ends have MU (loco↔loco). Loco↔freight does not require MU. Incomplete link = officially decoupled for HUD (not “drivable”). Top bar is red/null when on the ground, or on a car with no usable path to a loco. Look-at (CMD-01d) will reuse the same target-car logic.

**Top = usable train totals** (grey plate; red plate + nulls when not usable):

| Word | Example live | Example unknown |
|------|----------------|-----------------|
| Speed | `Speed 36 km/h` | `— Speed` |
| Grade | `Grade +1.2 %` | `— Grade` |
| Mass | `Mass 240 t` | `— Mass` |
| Cars | `Cars 5` (freight only; loco not counted) | `— Cars` |
| Handbrakes | `Handbrakes 3` (usable-consist applied count) | `— Handbrakes` |

**Second = that car only** (standing on it, or look-at on foot — standing wins):

| Word | Example live | Example unknown |
|------|----------------|-----------------|
| Pipe | `Pipe 2.0 bar` | `— Pipe` |
| Handbrake | `Handbrake 1` (this car: 1 on / 0 off) | `— Handbrake` |
| Couplers | `F+` usable; plain `F*` = coupled but chain loose; `F-` open / incomplete; yellow `F*` = loco↔loco usable but blue MU open | `— Couplers` |
| Car | `Car 3` (freight from loco); `Car N/A` on loco; `Car XX` if not on a usable train | `Car XX` |
| Job | `Job FH-12` | `— Job` |

**Planned (not on HUD yet):**

| Story | Segments (use the same naming rule) |
|-------|-------------------------------------|
| CMD-02 | Ammeter / Traction motor health |
| CMD-03 | Speed-limit alerts (grade already shipped) |

## Non-goals

- Full autopilot / replacing the need to drive
- Deleting cars or jobs to “clear” yards
- Harmony Transpilers unless Prefix/Postfix cannot solve the problem
- Pulling parking-lot features into active phases

## Core pillars

1. **Teleportation is the last resort** — Never delete. Teleport only after verification.
2. **Governor vs Monitor**
  - **Monitors (read-only):** Diagnostic HUD — speed, grade, tonnage, integrity (CMD-01a–d: car under feet → consist summary → look-at **second HUD bar** under the main strip with Car # / Job # → coupler tight/loose), power (ammeter / TM), terrain (grade + speed limits).
  - **Governors (active):** Thermal throttle-cap, auto-brake release — only through gated soft writes.
3. **Stability first** — Phase 0 (Foundation / Safe Boot) must load perfectly before UI or logic manipulation.

## Three-Gate pattern (all state writes)

Every write to game state must pass:

1. **Integrity Gate** — Is this action safe for the current world/consist?
2. **State Registry Gate** — Are required managers/objects present and expected?
3. **Soft Write** — Apply the minimal state change; abort closed on failure.

Governors additionally require **safety gates** (e.g. stationary checks) before writing.

## Roadmap

| Phase | Name | Intent |
|-------|------|--------|
| **0** | Foundation / Safe Boot | **Completed** — empty UMM mod loads; fail-closed; recovery documented |
| **1** | Diagnostic HUD (HIGH) | CMD-01 Integrity (01a→01b→01d→01c) → CMD-02 Power → CMD-03 Terrain (grade shipped; speed limits TBD) |
| **2** | Governor Mode (MEDIUM) | Three-Gate → CMD-04 Thermal → CMD-05 Auto-Brake |
| **3** | Yard Master | CMD-06 Manual Consist Management / teleport (needs prior abort stability) |

Stories and acceptance criteria: [PM_PLAN.md](../../PM_PLAN.md).

## Parking lot (future aspirations)

- Switch Path Tracer
- Anti-Wheelslip / Startup Assist
- Auto-Service / Auto-Shop
- Manual Transmission Override
- Mounting Suite / turntable helpers
- Engine Temp Soft Governor (if distinct from CMD-04)

## Testing philosophy

Use in-game Dev Tools (Comms Radio / Sandbox / Spawner). Do not burn development time on manual travel.

## Source

Master Project Context v3.0 + Developer Roadmap v3.0 (2026-07); CMD backlog refresh 2026-07-16; CMD-01 slices (01a–01d) 2026-07-16.
