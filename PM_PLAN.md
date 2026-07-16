# PM_PLAN — DerailValleyMod (Yard Master Suite)

Keep in sync with [doc/requirements/product.md](doc/requirements/product.md), [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md), and AGENT_HANDOFF “Current state”.

**Source:** Master Context / Developer Roadmap v3.0 (2026-07), backlog refresh 2026-07-16 (CMD IDs).

**MVP focus:** Situational awareness (Diagnostic HUD) before Governor writes.

---

## Phase 0 — Foundation & Stability (“Safe Boot”) — COMPLETED

**Goal:** Toolchain works; UMM loads an empty mod; fail-closed before any UI or state writes.

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
| E0-S1 | Environment setup (.NET 8+ SDK, net48 targeting workload, Cursor + C# Dev Kit, dnSpy) | Can target `net48` and inspect game assemblies | **Done** |
| E0-S2 | Project initializer: template-umm layout + `info.json` + UMM `Main`/`Load`/`OnToggle` | Empty mod appears in UMM and toggles cleanly | **Done** |
| E0-S3 | Document build → Mods drop path + 3-step recovery in DEV_GUIDE / modding.md | Another session can deploy and recover without guesswork | **Done** |
| E0-S4 | Graceful fail: missing Harmony target → log + self-disable (no game crash) | Broken signature does not take down the session | **Scaffold done** — try/catch on Load |
| E0-S5 | Smoke: game launches with mod enabled; Player.log clean of mod errors | Safe Boot complete | **Done** |

---

## Shipped baseline (Monitor HUD foundation)

L→R read-only strip; UMM toggle; km/h (game-native).

| ID | Story | Status |
|----|-------|--------|
| E1-S1 | Speed telemetry HUD | **Done** — Tier 1 + Tier 2 |
| E1-S2 | Grade % + consist tonnage | **Done** — Tier 1 + Tier 2 (`km/h \| ±% \| t`) |

---

## Epic 1 — Diagnostic HUD (Priority: HIGH)

**Goal:** Situational awareness only — **no game-state writes**. HUD continues **left → right**.

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
| CMD-01 | Integrity Monitor | Real-time Brake Pipe pressure, Handbrake count (applied on consist), Coupling status | **Next** |
| CMD-02 | Power Monitor | Ammeter readouts + Traction Motor (TM) health warnings | |
| CMD-03 | Terrain Monitor | Grade percentage + speed limit alerts | **Partial** — grade % shipped in E1-S2; speed-limit alerts remaining |

---

## Epic 2 — Governor Mode (Priority: MEDIUM)

**Goal:** Gated soft writes via **Three-Gate** (Integrity → State Registry → Soft Write) + safety gates. Prefix/Postfix only.

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
| E2-S1 | Shared Three-Gate + safety-gate helper | All governor writes go through one path; fail closed | **Prerequisite** for CMD-04/05 |
| CMD-04 | Thermal Governor | Dynamic throttle-capping to prevent TM Offline events; aborts if unsafe | |
| CMD-05 | Auto-Brake Governor | Engine-toggle linked brake release when safe; abort otherwise | |

---

## Epic 3 — Yard Master (helpers & teleportation)

**Goal:** Manual helpers and consist teleportation — **only after** Diagnostic HUD + Governor abort patterns are stable. Never delete cars.

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
| CMD-06 | Manual Consist Management | Teleport cars / consist via native organizers; abort rules (hazmat, jobs, coupler, speed, unknowns); fail closed | |

---

## Parking lot (not scheduled)

Discuss only after MVP Diagnostic HUD + core Governors ship:

- Switch Path Tracer (former E1-S3)
- Anti-Wheelslip
- Startup Assist (breaker prep on starter + while-running recovery; needs E2-S1)
- Turntable / precision mounting helpers
- Auto-Service / Auto-Shop
- Manual Transmission Override
- Mounting Suite
- Engine Temp Soft Governor (distinct from CMD-04 Thermal Governor if still needed)

---

## Quality gates (all phases)

- Prefix/Postfix only — no Transpilers without an explicit decision
- Graceful fail: log + self-disable on broken method signatures
- All state writes: Three-Gate + governor safety checks
- In-game validation via Comms Radio / Sandbox / Spawner
- Update PROJECT_STATUS + AGENT_HANDOFF Current state when a story ships
