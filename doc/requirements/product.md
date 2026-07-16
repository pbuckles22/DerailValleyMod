# Product — Yard Master Suite

## Goal

A **Fleet Operator** utility suite for *Derail Valley*: automate the tedium, preserve the simulation, prioritize stability.

## Non-goals

- Full autopilot / replacing the need to drive
- Deleting cars or jobs to “clear” yards
- Harmony Transpilers unless Prefix/Postfix cannot solve the problem
- Pulling parking-lot features into active phases

## Core pillars

1. **Teleportation is the last resort** — Never delete. Teleport only after verification.
2. **Governor vs Monitor**
   - **Monitors (read-only):** Situational awareness — speed, path, grade, tonnage.
   - **Governors (active):** Automate physics/maintenance — thermal, wheelslip, brake release, **startup assist** (breakers / electrics prereqs on starter + while running) — only through gated soft writes.
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
| **0** | Foundation / Safe Boot | Empty UMM mod loads; fail-closed; recovery documented |
| **1** | Monitor Mode | Read-only HUD: telemetry, Switch Path Tracer, grade/tonnage |
| **2** | Governor Mode | Thermal Governor, Anti-Wheelslip, Auto-Brake Release, Startup Assist |
| **3** | Yard Master | Helper tools + manual teleportation (needs Phase 0–2 abort stability) |

Stories and acceptance criteria: [PM_PLAN.md](../../PM_PLAN.md).

## Parking lot (future aspirations)

- Auto-Service (pay-for-service remote)
- Auto-Shop (remote parts management)
- Manual Transmission Override (shift to automatic)
- Mounting Suite (bracket auto-install and alignment)
- Engine Temp Soft Governor (dynamic throttle scaling) — distinct from Phase 2 Thermal Governor

## Testing philosophy

Use in-game Dev Tools (Comms Radio / Sandbox / Spawner). Do not burn development time on manual travel.

## Source

Master Project Context v3.0 + Developer Roadmap v3.0 (2026-07).
