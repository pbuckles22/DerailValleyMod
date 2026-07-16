# PM_PLAN — DerailValleyMod (Yard Master Suite)

Keep in sync with [doc/requirements/product.md](doc/requirements/product.md), [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md), and AGENT_HANDOFF “Current state”.

**Source:** Master Context / Developer Roadmap v3.0 (2026-07).

---

## Phase 0 — Foundation & Stability (“Safe Boot”)

**Goal:** Toolchain works; UMM loads an empty mod; fail-closed before any UI or state writes.

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
| E0-S1 | Environment setup (.NET 8+ SDK, net48 targeting workload, Cursor + C# Dev Kit, dnSpy) | Can target `net48` and inspect game assemblies | **Done** — SDK 10 + net48 build; dnSpy optional |
| E0-S2 | Project initializer: template-umm layout + `info.json` + UMM `Main`/`Load`/`OnToggle` | Empty mod appears in UMM and toggles cleanly | **Done** — Active in UMM 0.32.5; toggle OK |
| E0-S3 | Document build → Mods drop path + 3-step recovery in DEV_GUIDE / modding.md | Another session can deploy and recover without guesswork | **Done** |
| E0-S4 | Graceful fail: missing Harmony target → log + self-disable (no game crash) | Broken signature does not take down the session | **Scaffold done** — try/catch on Load; no broken target yet to force in-game |
| E0-S5 | Smoke: game launches with mod enabled; Player.log clean of mod errors | Safe Boot complete | **Done** — green Active; toggle off/on works |

**Gate:** Phase 0 must load perfectly before Phase 1 UI or Phase 2+ writes.

---

## Phase 1 — Monitor Mode (read-only HUD)

**Goal:** Situational awareness only — **no game-state writes**.

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
| E1-S1 | Telemetry HUD (speed and related readouts) | Accurate while driving; toggleable | **Done** — km/h HUD; UMM toggle; Tier 1 tests + Tier 2 sandbox smoke (matches cab speedo) |
| E1-S2 | Grade / tonnage calculation display | Clear units/sign; read-only | |
| E1-S3 | Switch Path Tracer | Next switch path visualized (Monitor only) | |

---

## Phase 2 — Governor Mode (active modifiers)

**Goal:** Automate physics/maintenance chores via **gated soft writes**. All writes use the **Three-Gate** pattern (Integrity → State Registry → Soft Write). Governors must pass safety gates (e.g. stationary checks) before writing.

| ID | Story | Done when |
|----|-------|-----------|
| E2-S1 | Shared Three-Gate + safety-gate helper | All governor writes go through one path; fail closed |
| E2-S2 | Thermal Governor | Heat managed when enabled; aborts if unsafe |
| E2-S3 | Anti-Wheelslip | Wheelslip mitigated when enabled; safety gates enforced |
| E2-S4 | Auto-Brake Release | Brake release assisted when safe; abort otherwise |

**Not in this phase (parking lot):** Engine Temp Soft Governor (dynamic throttle scaling), Auto-Service, Auto-Shop, AT override.

---

## Phase 3 — Yard Master (helpers + teleportation)

**Goal:** Manual helpers and consist teleportation — **only after** Phase 0–2 abort patterns are stable. Never delete cars.

| ID | Story | Done when |
|----|-------|-----------|
| E3-S1 | Turntable / precision mounting helpers | Hotkeys work; no-op + feedback when invalid |
| E3-S2 | Teleport consist to player / track snap via native organizers | Happy path preserves save/job integrity |
| E3-S3 | Abort rules (hazmat, active jobs, coupler, speed, unknowns) | Fail closed with clear feedback |

---

## Parking lot (not scheduled)

Discuss only after core pillars ship:

- Auto-Service (pay-for-service remote)
- Auto-Shop (remote parts management)
- Manual Transmission Override (shift to automatic)
- Mounting Suite (bracket auto-install and alignment)
- Engine Temp Governor — Soft Governor (dynamic throttle scaling)

---

## Quality gates (all phases)

- Prefix/Postfix only — no Transpilers without an explicit decision
- Graceful fail: log + self-disable on broken method signatures
- All state writes: Three-Gate + governor safety checks
- In-game validation via Comms Radio / Sandbox / Spawner
- Update PROJECT_STATUS + AGENT_HANDOFF Current state when a phase ships
