# PM_PLAN — Yard Master Suite

Official **backlog**. Cross off here when a story ships; refresh [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md) + [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Current state* in the same change.

**Details / labels:** [doc/requirements/product.md](doc/requirements/product.md)  
**Why this order:** Journey stages below (Apprentice → Yardman → Yard Master).  
**Local Gemini scratch:** `doc/GeminiDocs/` (gitignored except README) — bridge only; **this file** remains the checked-off truth.

**MVP (Stage 1):** Diagnostic HUD so you can drive without blowing fuses — before Governors / dispatcher tools.

---

## How to read this

| Mark | Meaning |
|------|---------|
| `[x]` | Done (Tier 1 + applicable Tier 2) |
| `[~]` | In progress / partial |
| `[ ]` | Backlog |

**Shape**

1. **Epic** — checkbox · `Epic N` · title · short description  
2. **Story** — indented · checkbox · `N.M` · title · short description  
3. **User story** — indented under the story (blockquote)

Legacy IDs (`CMD-01a`, `QOL-08`, …) stay in parentheses so older notes still resolve.

---

## Journey alignment *(why the backlog looks like this)*

| Stage | Player focus | Mod focus | Stories |
|-------|----------------|-----------|---------|
| **1 — Apprentice** | Throttle, brakes, sander, don’t blow the fuse | Situational Awareness HUD | **Epic 1** + **4.3** + **4.7** *(current)* |
| **2 — Junior Yardman** | Switches, yard paths, consist moves | Remote switch / path integrity | **3.3–3.4** |
| **3 — Yard Master** | Multi-stop efficiency | Auto dispatch + workbench | **3.5**, **5.1**, **3.1** |

Back-dated Epic 1 / Epic 4 items (e.g. **4.3**, **1.7–1.9**) exist so Stage 1 play is actually enjoyable — cab gadgets on the HUD, no dash wall in the yard.

---

## ID crosswalk (legacy → official)

| Official | Legacy | Title |
|----------|--------|-------|
| **0.x** | E0-S* | Foundation / Safe Boot |
| **1.1–1.2** | E1-S1 / E1-S2 | Speed / grade+mass baseline |
| **1.3–1.6** | CMD-01a–d | Integrity monitor |
| **1.7–1.9** | CMD-02a–c | Power monitor |
| **1.10–1.11** | CMD-03 | Terrain / speed limits (current → next) |
| **1.12** | *(new)* | Personal heading / compass (always-on) |
| **1.13** | *(new)* | Player map coordinates (always-on) |
| **1.14** | *(new)* | Park / return mark (freeze “you parked here”) |
| **2.x** | E2-S1, CMD-04/05 | Governor Mode |
| **3.1** | CMD-06 | Consist teleport + Station Snap & Return |
| **3.2** | *(new)* | Comms Radio Overlay (helper UI) |
| **3.3–3.5** | *(new / was parking-lot path tracer)* | Dispatcher: remote switch → manual path check → auto-route |
| **4.4** | *(new)* | Look-at Track ID |
| **4.5** | *(was 4.4)* | Next station distance (fluids) |
| **4.6** | *(was Project Plan 3.2)* | Station waypoint (foot) + in-zone station coords |
| **4.7** | *(was parking-lot HUD IA)* | Center-weighted / stacked HUD IA |
| **5.1** | *(new)* | Digital Catalog |

---

## Backlog

- [x] **Epic 0 — Foundation & Safe Boot** — Toolchain works; empty UMM mod loads; fail-closed before UI or state writes.

  - [x] **0.1 Environment setup** — .NET SDK, net48 targeting, Cursor + C# Dev Kit, dnSpy.
    > As a maintainer, I want a working net48 toolchain so I can build and inspect game assemblies.

  - [x] **0.2 Project initializer** — template-umm layout, `info.json`, UMM `Main` / `Load` / `OnToggle`.
    > As a player, I want the mod to appear in UMM and toggle cleanly with no gameplay yet.

  - [x] **0.3 Build & recovery docs** — Mods drop path + 3-step recovery in DEV_GUIDE / modding.md.
    > As a new session, I want documented deploy/recover steps so I do not guess paths.

  - [~] **0.4 Graceful fail** — Missing Harmony target → log + self-disable (no game crash). *(Scaffold: try/catch on Load.)*
    > As a player, I want a broken signature to disable the mod instead of taking down the session.

  - [x] **0.5 Safe Boot smoke** — Game launches with mod enabled; Player.log clean of mod errors.
    > As a maintainer, I want a clean load smoke so Foundation is truly done.

---

- [ ] **Epic 1 — Diagnostic HUD** *(HIGH · Journey Stage 1)* — Read-only situational awareness; HUD left → right; **no game-state writes**.  
  *Tier 2:* each in-game story ships discrete `T2 …` Player.log lines (no per-frame spam).  
  *Why now:* Apprentice “don’t blow up” — ammeter / TM / fluids before deep yard tools.

  - [x] **1.1 Speed telemetry** *(was E1-S1)* — Top-bar speed in km/h (game-native).
    > As a driver, I want speed on the HUD so I can glance without hunting the cab gauge.

  - [x] **1.2 Grade & tonnage** *(was E1-S2)* — Top-bar grade % + consist mass (t).
    > As a driver, I want grade and train weight visible so I can judge effort at a glance.

  - [x] **1.3 Car integrity** *(was CMD-01a)* — Pipe / Handbrake / Couplers for the car under the player; `T2 integrity`.
    > As a yard worker, I want the car I’m standing on summarized so I know brakes and couplers without opening menus.

  - [x] **1.4 Train + local-car HUD** *(was CMD-01b)* — Top bar = usable loco-train totals; second bar = local car; `T2 consist` / `T2 local-car`. *(Red null top superseded by **4.3**.)*
    > As a driver, I want train totals and the car under me on two bars so cab and consist stay clear.

  - [x] **1.5 Coupler tight/loose** *(was CMD-01c)* — Chain tight vs loose marks; `T2 coupler`.
    > As a shunter, I want tight vs loose chains shown so I know if an end is really ready to move.

  - [x] **1.6 Look-at inspect** *(was CMD-01d)* — Look-at wins second bar; standing fallback; `Loco …` type; `T2 look-at`.
    > As a yard scout, I want to aim at a car and see its integrity so I can inspect from a roof or the ground.

  - [x] **1.7 Load monitor** *(was CMD-02a)* — Top-bar `Load %` (amps / max); yellow ≥80%, red ≥95%; `T2 power`. **Done** — Tier 1; Tier 2 **PASS\*** (live % on DE2; color bands deferred until hard pull)
    > As an engineer, I want load % on the HUD so I know how close I am to blowing the traction motor fuses.

  - [x] **1.8 Motor status** *(was CMD-02b)* — Top-bar `Motors` OK (green) / Hot (yellow) / Dead (red); `T2 power`. **Done / shipped** — Tier 1 + Tier 2 **PASS**. HUD is **current-state only** (OK = below threshold; Hot = above `overheatingTemperatureThreshold` while fuse alive; Dead = TMS trip / working &lt; total). **Cut:** early Hot / hysteresis / predictive dwell on the HUD — thermal mitigation belongs in **Epic 2**.
    > As an engineer, I want TM temperature status on the HUD so I can see if motors are currently overheating or already tripped.

  - [x] **1.9 Fluid monitor** *(was CMD-02c)* — Top-bar `Fuel %` + `Oil %`; yellow if either &lt; 20%; red if either &lt; 5% (paired); `T2 power`. **Done / shipped** — Tier 1 + Tier 2 **PASS** (v0.4.18). Steam/electric placeholders deferred.
    > As an engineer, I want Fuel/Oil % on the HUD so I know when to return for service before a stall.

  - [x] **1.10 Speed limit — current** *(was CMD-03)* — Top-bar single `Limit N` after Speed; yellow within 5 km/h of limit; red when over; `T2 limit` (Limit/loco changes only). **Authority:** posted `SignDebug` boards (digit × 10); geometry / SignPlacer ladder as fallback. Grade already in **1.2**. **Done / shipped** — Tier 1 + Tier 2 **PASS** (v0.4.20).
    > As a driver, I want the current governing speed limit on the HUD so I do not overspeed the board I’m under right now.

  - [x] **1.11 Speed limit — next / trend** — Same single `Limit` badge (no second km/h chip): `^` green / `v` yellow for next different board ahead (lookahead ≈ max(500 m, speed×6)). Builds on **1.10** board+fallback authority. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a driver, I want an up/down cue for the next speed limit so I can brake or accelerate before I miss the change.

  - [x] **1.12 Personal heading** — Always-on nav bar `Heading NE` (16-point; no degrees). Player/camera facing; Unity +Z = north. Quiet `T2 heading` on point change. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a yard worker, I want a personal compass so I can orient myself to a map even when no train HUD is shown.

  - [x] **1.13 Player coordinates** — Always-on flat-map `Pos x, z` (no height) on the nav bar. Quiet `T2 pos` (≥50 unit move). **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a yard worker, I want exact coordinates beside the compass so I can find myself on a map in large yards.

  - [x] **1.14 Park / return mark** — `Home` sets/updates session park mark at player XZ; `Shift+Home` clears. Always-on nav chip `Park NE 84m` (16-point bearing + meters back to mark) or `Park here` within 1 m; omitted when unmarked. Quiet `T2 park` on set/clear / bearing change. Distinct from live `Heading` / `Pos`. **Done** — Tier 1; Tier 2 smoke pending (v0.4.24).
    > As a yard worker, I want to mark where I left the loco so when I’m running around I always know which way and how far to get back.

  **Build order (Stage 1 open work):** **1.14** Tier 2 smoke → mark **Epic 1** complete → then **4.6** / Epic 2; re-smoke Load yellow/red when practical. *(Do not reopen **1.8** HUD thermal prediction.)*

---

- [ ] **Epic 2 — Governor Mode** *(MEDIUM)* — Gated soft writes via Three-Gate + safety gates. Prefix/Postfix only. *Active thermal management lives here — not on the Monitor HUD.*

  - [ ] **2.1 Three-Gate helper** *(was E2-S1)* — Shared Integrity → State Registry → Soft Write path; fail closed. *Core foundation / prerequisite for 2.2 / 2.3.*
    > As a maintainer, I want one write path so every governor aborts the same safe way.

  - [ ] **2.2 Thermal governor** *(was CMD-04)* — Soft-scale / cap throttle when **1.8** Motor status is Hot (current over-temp); abort if unsafe. *Prediction / protection — not a HUD rewrite.*
    > As an engineer, I want the mod to soft-cap throttle when motors overheat so I avoid TM Offline events.

  - [ ] **2.3 Auto-brake governor** *(was CMD-05)* — Engine-toggle linked brake release when safe; abort otherwise.
    > As an engineer, I want safe auto brake-release on engine toggle so startup is less fiddly without unsafe writes.

---

- [ ] **Epic 3 — Yard Master / Dispatcher** *(Journey Stages 2–3)* — Yard efficiency after HUD (+ governor abort patterns for anything that writes). Never delete cars.

  - [ ] **3.1 Manual consist management & teleport** *(was CMD-06)* — Teleport via native organizers and/or helper UI; abort on hazmat / jobs / coupler / speed / unknowns; fail closed. Includes **Station Snap & Return**.
    > As a yard master, I want verified teleport helpers and station snap/return so I can reorganize consists and handle paperwork without long walks.

  - [ ] **3.2 Comms Radio Overlay** — Auxiliary HUD panel with helper actions for consist ops / teleport (keeps tools off physical item clutter).
    > As a yard master, I want a Comms-style helper panel so teleport and yard tools stay one click away.

  - [ ] **3.3 Manual switch / turntable remote** — Flip switches and turntables from the HUD / look-at so you don’t walk back and forth. *Stage 2.*
    > As a shunter, I want to flip switches and turntables from my HUD so I don’t have to walk kilometres between throws.

  - [ ] **3.4 Path tracer: manual check** *(was parking-lot Switch Path Tracer)* — Pick a destination; show whether switches are aligned (“check my math”) without auto-throwing. *Stage 2.*
    > As a yard master, I want to click a destination and see if my switches are aligned so I can verify the path myself.

  - [ ] **3.5 Path tracer: automated dispatching** — “Align Route” after a verified path; throws switches automatically. *Stage 3 — needs stable **3.4** + write-safety patterns.*
    > As a yard master, I want an Align Route control that fixes switches automatically once the path is verified.

  **Build order (dispatcher):** **3.3** → **3.4** → **3.5** ( **3.1** / **3.2** when teleport / UI pain dominates).

---

- [ ] **Epic 4 — HUD quality (QOL)** — Small UX polish on the Diagnostic HUD. *Supports Stage 1 playability.*

  - [x] **4.1 Enhanced targeting** *(was QOL-06)* — Look-at spherecast **0.15 m**, max **250 m**. *PASS\*; slight sky-stickiness accepted.*
    > As a yard scout, I want distant cars to resolve under the crosshair so I can inspect from farther away.

  - [x] **4.2 Cargo on second bar** *(was QOL-07)* — Freight `Cargo …` / `Empty Cargo`. *Empty Cargo smoke deferred.*
    > As a yard scout, I want cargo named on the second bar so I know what a car is carrying.

  - [x] **4.3 Hide loco gadget top bar** *(was QOL-08)* — Show loco chips **only** on a usable loco train; otherwise **hide** (no red dash wall). Reuse usable-train + look-at target. Supersedes **1.4** red-null UI. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.14+)
    > As a player, I want loco gadget readouts only when a loco is relevant — like cab instruments — not a wall of dashes in the yard.

  - [ ] **4.4 Extended car inspection — Track ID** — Add current Track ID (e.g. `SM-O6I`) on look-at inspector. Job already ships on the second bar.
    > As a yard master, I want a car’s Track ID when looking at it so I can identify lost consists.

  - [ ] **4.5 Next station distance** *(linear nav)* — When Fuel/Oil are in warn/critical, optional `Next: … [N km]` using live station data (not a static mileage table); fail-closed if path/station unknown. *No GPS minimap.*
    > As an engineer, I want distance to the next station when fluids are low so I know whether I can make it without a map overlay.

  - [ ] **4.6 Station waypoint (foot) + in-zone station coords** — When the player is inside a city/station **zone**, show that station’s map coords (and distance + 16-point bearing to it) on the always-on / nav strip for job-board walks. Fail-closed outside zones / if station unknown. Distinct from **4.5** (loco fluid-gated). *No minimap.*
    > As a yard master, I want the local station’s coordinates and which way to walk when I’m in a city zone so I don’t get lost finding the job board.

  - [x] **4.7 HUD strip IA reorder** — Horizontally **center** every HUD row; stack loco → look-at → always-on nav bar (same chrome as loco/look-at). Loco chip order: `Fuel · Oil · Mass · Grade · Load · Speed · Limit · Motors · Handbrakes · Cars`. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a driver, I want Speed and Limit in the visual center of the loco bar so I glance there first.

---

- [ ] **Epic 5 — Digital Catalog** *(Journey Stage 3 workbench)* — Convenience logistics for a working operator.

  - [ ] **5.1 Digital Catalog** — Order keys / flags / tools to the player location instead of driving to the store.
    > As a professional operator, I want to order keys, flags, and tools to my location so I don’t waste time on store runs.

---

## Parking lot

Not scheduled — discuss when Journey stage friction demands it:

- [ ] Anti-Wheelslip
- [ ] Startup Assist *(needs **2.1**)*
- [ ] Auto-Service / Auto-Shop *(overlap check vs **5.1**)*
- [ ] Manual Transmission Override
- [ ] Mounting Suite / precision mounting
- [ ] Engine Temp Soft Governor *(if distinct from **2.2**)*
- [ ] Speed-limit auto-throttle governor *(soft-cap to % of Limit — same pattern as **2.2**; candidate **2.4**)*

---

## Quality gates (all epics)

- Prefix/Postfix only — no Transpilers without an explicit decision
- Graceful fail: log + self-disable on broken method signatures
- All state writes: Three-Gate + governor safety checks
- In-game validation via Comms Radio / Sandbox / Spawner
- When a story ships: check its box here, then update PROJECT_STATUS + AGENT_HANDOFF *Current state*

### Recent breakage risks *(known hazards)*

net48 + UMM + Harmony against live game DLLs — treat game updates as hostile:

- **`TrainCar` / player / look-at path** — signature or layer changes break target resolution and most of **Epic 1** (standing, look-at, usable-train walk).
- **`SimController` / `LocoSim` ports** (`TractionMotor*`, fuel/oil containers) — renames or port-id churn break **1.7–1.9** power readouts (and later **Epic 2** governors that soft-write the same surface).
- **`RailTrack` / bogie / speed boards (`SignDebug` / SignPlacer)** — break **1.10**/**1.11** Limit authority and lookahead.
- **Coupler / brake / MU APIs** — break **1.3–1.5** marks and the usable-train yard rule (top bar visibility for **4.3**).
- **After any DV patch:** re-run Tier 1, redeploy, confirm HUD `v…` chip, then smoke the active Epic 1 checklist in [TEST_PLAN.md](TEST_PLAN.md). Prefer fail-closed (log + self-disable) over crashing the session.
