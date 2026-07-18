# PM_PLAN — DerailValleyMod (Yard Master Suite)

Keep in sync with [doc/requirements/product.md](doc/requirements/product.md), [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md), and AGENT_HANDOFF “Current state”.

**Source:** Master Context / Developer Roadmap v3.0 (2026-07), backlog refresh 2026-07-16 (CMD IDs); CMD-01 sliced 2026-07-16 (01a–01d).

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

### CMD-01 — Integrity Monitor (slices)

Cab summary first, then ground inspect; coupler tight/loose last.

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
| CMD-01a | Car integrity readout | Single HUD: brake pipe (bar), **handbrake 0/1 for car under player**, F/R **coupled** (`+/-`) for car under player; **`T2 integrity` Player.log**; HUD width uses draw style (no Couplers clip) | **Done** — Tier 1 green; load/toggle + foot/on-car smoke OK. Dual-HUD / train-wide / look-at validation deferred to 01b/01d |
| CMD-01b | Train + local-car HUD | Top bar = usable loco-train totals (Cars freight-only, Handbrakes, …); second bar = current vehicle; not usable → red null top; **`T2 consist` / `T2 local-car` logs**; HUD `v…` chip | **Done** — Tier 1 green; Tier 2 dual-HUD smoke OK (MU yellow warning deferred until 2nd loco) |
| CMD-01c | Coupler tight/loose | Show chain **tightened** vs loose when coupled (car and/or look-at); **`T2 coupler` logs** | After 01d |
| CMD-01d | Look-at inspect | On foot: look-at fills second bar like standing on that car; standing on a car always wins over look-at; bar gone when no target; **`T2 look-at` logs** | Next |

**Build order:** 01a → 01b → 01d → 01c.

**Tier 2 logging (retro):** Every Monitor story that needs in-game sign-off ships discrete `T2 …` Player.log lines for its checklist — same pattern as CMD-01a (`Tier2IntegrityDebug`). No per-frame spam.

### Other Monitor stories

| ID | Story | Done when | Status |
|----|-------|-----------|--------|
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
