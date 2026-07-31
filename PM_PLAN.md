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
| **1 — Apprentice** | Throttle, brakes, sander, don’t blow the fuse | Situational Awareness HUD | **Epic 1** *(done)* + **4.3** + **4.6** + **4.7**; Epic 2 done |
| **2 — Junior Yardman** | Walk/throw switches (grind); earn **Shunting (SH)** | — | *(no remote throw — **3.3 cut**)* |
| **3 — Yard Master** | City → track deliveries; don’t think about levers | CTC Align Route + reverse cue | **3.5** *(Dispatcher-gated)*, **5.1**, **3.1** |

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
| **3.3** | *(was remote switch)* | **Cut** — walk/throw is the grind to SH / Align Route |
| **3.4** | *(was check-my-math UX)* | **Internal** path engine only (no separate player chore) |
| **3.5** | *(was parking-lot path tracer)* | City→track Align Route (Dispatcher-gated); reverse cue; through-lane bias |
| **4.4** | *(new)* | Look-at Track ID |
| **4.5** | *(was 4.4)* | Next station distance (fluids) |
| **4.6** | *(was Project Plan 3.2)* | Station waypoint (foot) + in-zone station coords |
| **4.7** | *(was parking-lot HUD IA)* | Center-weighted / stacked HUD IA |
| **4.8** | *(new)* | Active Job HUD + preview-prep edge |
| **4.9** | *(new)* | AR wayfinding markers (loco / station office / pin) |
| **4.10** | *(new)* | Loco radar (find spawned locos for MU / yard walk) |
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

- [x] **Epic 1 — Diagnostic HUD** *(HIGH · Journey Stage 1)* — Read-only situational awareness; HUD left → right; **no game-state writes**. **Status: complete — 2026-07-23**
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

  - [x] **1.5 Coupler tight/loose** *(was CMD-01c)* — Chain tight vs loose marks; `T2 coupler`. Bundle E **#23** colors: red tow-not-ready / yellow loco↔loco MU open / white tow-ready / blue MU team (**v0.4.80**, Tier 2 **PASS**).
    > As a shunter, I want tight vs loose chains shown so I know if an end is really ready to move.

  - [x] **1.6 Look-at inspect** *(was CMD-01d)* — Look-at wins second bar; standing fallback; `Loco …` type; `T2 look-at`.
    > As a yard scout, I want to aim at a car and see its integrity so I can inspect from a roof or the ground.

  - [x] **1.7 Load monitor** *(was CMD-02a)* — Top-bar `Load %` (amps / max); yellow ≥80%, red ≥95%; `T2 power`. **Done** — Tier 1; Tier 2 **PASS** (live % on DE2; F6 color bands **PASS**; live hard-pull **#22** waived 2026-07-26 — confirm in normal play)
    > As an engineer, I want load % on the HUD so I know how close I am to blowing the traction motor fuses.

  - [x] **1.8 Motor status** *(was CMD-02b)* — Top-bar `Motors` OK (green) / Hot (yellow) / Dead (red); `T2 power`. **Done / shipped** — Tier 1 + Tier 2 **PASS**. HUD is **current-state only** (OK = below threshold; Hot = above `overheatingTemperatureThreshold` while fuse alive; Dead = TMS trip / working &lt; total). **Cut:** early Hot / hysteresis / predictive dwell on the HUD — thermal mitigation belongs in **Epic 2**.
    > As an engineer, I want TM temperature status on the HUD so I can see if motors are currently overheating or already tripped.

  - [x] **1.9 Fluid monitor** *(was CMD-02c)* — Top-bar `Fuel %` + `Oil %`; yellow if either &lt; 20%; red if either &lt; 5% (paired); `T2 power`. **Done / shipped** — Tier 1 + Tier 2 **PASS** (v0.4.18). Steam/electric placeholders deferred.
    > As an engineer, I want Fuel/Oil % on the HUD so I know when to return for service before a stall.

  - [x] **1.10 Speed limit — current** *(was CMD-03)* — Top-bar single `Limit N` after Speed; yellow within 5 km/h of limit; red when over; `T2 limit` (Limit/loco changes only). **Authority:** posted `SignDebug` boards only (digit × 10; sticky until next board; dual `6/4` via switch branch). No geometry fallback. Grade already in **1.2**. **Done / shipped** — Tier 1 + Tier 2 **PASS** (v0.4.20); boards-only UX **0.5.34**.
    > As a driver, I want the current governing speed limit on the HUD so I do not overspeed the board I’m under right now.

  - [x] **1.11 Speed limit — next / trend** — Same single `Limit` badge (no second km/h chip): `^` green / `v` yellow for next different board ahead (lookahead ≈ max(500 m, speed×6)). Builds on **1.10** board+fallback authority. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a driver, I want an up/down cue for the next speed limit so I can brake or accelerate before I miss the change.

  - [x] **1.12 Personal heading** — Always-on nav bar `Heading NE` (16-point; no degrees). Player/camera facing; Unity +Z = north. Quiet `T2 heading` on point change. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a yard worker, I want a personal compass so I can orient myself to a map even when no train HUD is shown.

  - [x] **1.13 Player coordinates** — Always-on flat-map `Pos x, z` (no height) on the nav bar. Quiet `T2 pos` (≥50 unit move). **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a yard worker, I want exact coordinates beside the compass so I can find myself on a map in large yards.

  - [x] **1.14 Park / return mark** — `Home` sets/updates session mark at player XZ; `Shift+Home` clears. Always-on nav chip `Marked NE 84m` (16-point bearing + meters back) or `Marked here` within 1 m; omitted when unmarked. Quiet `T2 mark` on set/clear / bearing change. Distinct from live `Heading` / `Pos`. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.25).
    > As a yard worker, I want to mark where I left the loco so when I’m running around I always know which way and how far to get back.

  - [x] **1.15 Consist free-motion** *(was “Engine / MU alive”)* — Consist HUD chip vs cab controls: **quiet** when synced; **yellow** `MU idle` if either unit off/Neutral (brakes match); **red** `MU desync` on brake/ind-brake fight or both on+in-gear mismatch (reverser/throttle). Reads engine-on, reverser, throttle, train + independent brake. Distinct from **1.8** Motors Hot and **2.3** auto-brake. **Done** — Tier 1 + Tier 2 smoke **PASS** (v**0.5.20**).
    > As an engineer, I want a clear warning when another loco on my train is fighting free forward or reverse motion, a softer cue when units are off or in neutral, and silence when the consist is correctly synced with me.

  - [ ] **1.16 Recommended Limit + soft brake lead** — `Limit` is a **look-ahead recommendation**: adopts the next slower posted board once inside the soft-brake lead (not when tires pass). HUD labels **`(Posted)`** / **`(Recommended)`** / `(Geometry)`; **5 s loosen-hold** and no Posted↔Recommended bounce on the same km/h.
    - **Path-aware look-ahead (0.5.51 / 0.5.53)** — boards are gathered along the **route ahead** (`TrackPathAhead` walks the live graph through each junction as thrown). Distance is **arc span** along the Bezier (not chord); facing uses the **route tangent at the board**, not loco heading (0.5.52: `'4'=40` skipped at ~12 m with `fDot=-0.39`). Kills off-branch steals and finds boards past the next switch.
    - **Grade-aware braking (0.5.51)** — two budgets: soft (light `0.18` / heavy `0.08` m/s², 12 s reaction) drives the yellow window and adopt; hard (light `0.55` / heavy `0.30`, 3 s) drives red. Downhill gravity (`9.81 × −grade/100`) is subtracted from both, so a descent warns far earlier. Gravity beating the hard budget = explicit **`RUNAWAY — Brake N NOW`** chip rather than a braking distance the train cannot achieve. Adopt lead factor cut `3.5 → 1.15` (the chip warns early; the number changes when you must act).
    - **Sticky = tire-pass only (0.5.51)** — `PostedStickyLimit` + `BoardTakeDetector`: a restriction is released only by passing a board. Fixes the 0.5.50 stress derail where a `'6'=60` board 273 m *behind* raised Limit 40→60 mid-descent.
    - Posted boards remain the authored authority; **sustained geometry fills only when no board is known**. **Follow-up:** hold a limit *increase* until the rearmost car clears the slower zone; look-ahead geometry along the path (not whole-track min).
    > As an engineer, I want Limit to show a safe speed for what is coming — with enough lead to ease from 80 to 60 — instead of flipping under my wheels and leaving me to panic-brake.

  **Build order:** **Epic 4** complete → **2.1** Three-Gate → **2.2**. *(Do not reopen **1.8** HUD thermal prediction.)*

---

- [x] **Epic 2 — Governor Mode** *(MEDIUM)* — Gated soft writes via Three-Gate + safety gates. Prefix/Postfix only. *Active thermal management lives here — not on the Monitor HUD.* **Status: complete 2026-07-28** *(**2.1**–**2.3**; close @ **0.5.25**)*

  - [x] **2.1 Three-Gate helper** *(was E2-S1)* — Shared Integrity → State Registry → Safety → Soft Write path; fail closed. *Core foundation / prerequisite for 2.2 / 2.3.* **Done** — Tier 1 (**v0.4.81**); Tier 2 N/A until a governor soft-writes (**2.2**).
    > As a maintainer, I want one write path so every governor aborts the same safe way.

  - [x] **2.2 Thermal governor** *(was CMD-04)* — Soft-roll throttle when **1.8** Motor status is Hot (cab MU Warning/Critical); abort if unsafe. Warning ceiling **75%**, Critical **55%**, rollback **5%/s** via `Throttle.Set` + ThreeGate. **Done** — Tier 1 + Tier 2 smoke **PASS** (v**0.5.15**).
    > As an engineer, I want the mod to soft-cap throttle when motors overheat so I avoid TM Offline events.

  - [x] **2.3 Auto-brake governor** *(was CMD-05)* — Engine **on→off** soft-rolls **train + independent** toward full and **throttle toward idle** (any speed; coast = leave engine on); ThreeGate fail-closed; never auto-releases on start; handbrakes untouched. Distinct from parking-lot Startup Assist. **Done** — Tier 1 + Tier 2 smoke **PASS** (v**0.5.25**).
    > As an engineer, I want air brakes applied and throttle brought down whenever I shut the engine down so an unpowered loco is not free to roll under power residue — if I want to coast, I leave the engine on.

---

- [ ] **Epic 3 — Yard Master / Dispatcher** *(Journey Stages 2–3)* — CTC-style **Align Route**: pick **city → track**, pathfind (through-lane bias), show reverse cue, throw switches. Never delete cars. Scope: **yard and inter-city**. DV has **no Dispatcher license** — gate Align on **Dispatcher** (Dispatcher1) as the career unlock.

  - [ ] **3.1 Manual consist management & teleport** *(was CMD-06)* — Teleport via native organizers and/or helper UI; abort on hazmat / jobs / coupler / speed / unknowns; fail closed. Includes **Station Snap & Return**.
    > As a yard master, I want verified teleport helpers and station snap/return so I can reorganize consists and handle paperwork without long walks.

  - [ ] **3.2 Comms Radio Overlay** — Auxiliary HUD panel with helper actions for consist ops / teleport / Align Route (keeps tools off physical item clutter).
    > As a yard master, I want a Comms-style helper panel so teleport and yard tools stay one click away.

  - [x] **3.3 Manual switch / turntable remote** — **Cut** (2026-07-28). Walking/throwing switches is the career grind. Licensed **3.5** Align Route is the CTC remote. *(PgUp/PgDn turntable = Epic 4 QOL only.)*
    > ~~As a shunter, I want to flip switches and turntables from my HUD…~~

  - [ ] **3.1b License-gated re-rail / spawn** *(debug → product candidate)* — Hotkey: scroll liveries the player holds licenses for; place with same re-rail blue/red ghost box as native re-rail. Replace/extend any ad-hoc spawn debt.
    > As a tester/yard master, I want to re-rail a licensed loco under the crosshair with clear place/no-place feedback.

  - [x] **3.4 Path tracer: manual check (player chore)** — **Demoted** (2026-07-28). Not a separate "check my math" grind. Path + alignment math is the **internal engine** for **3.5** (preview before throw). Look-at **End** pin may remain as a quick spur shortcut.
    > ~~As an engineer / yardman, I want to click a destination and only check switches…~~

  - [~] **3.5 Align Route (CTC)** — Destination **city → yard track**; pathfind with **passthrough / through-lane cost bias**; HUD **Facing OK / Reverse into dest / N reverses** (informational — no auto-shove yet); **Align Route** throws junctions via ThreeGate. **Requires Dispatcher** (GeneralLicenseType.Dispatcher1). Fail closed on no path / unknown. *WIP.*
    > As a licensed shunter/dispatcher, I want to pick a city and track, see if I'll need to reverse, and Align Route so switches are set for the delivery — modern desk, not hiking levers.

  **Build order:** **3.5** (path engine → picker → reverse cue → Align); **3.1** / **3.1b** / **3.2** when teleport / re-rail / UI pain dominates.

---

- [x] **Epic 4 — HUD quality (QOL)** — Small UX polish on the Diagnostic HUD. *Supports Stage 1 playability.* **Status: complete 2026-07-28** *(incl. **4.12** @ **0.5.17**)*

  - [x] **4.1 Enhanced targeting** *(was QOL-06)* — Look-at spherecast **0.15 m**, max **250 m**. *PASS\*; slight sky-stickiness accepted.*
    > As a yard scout, I want distant cars to resolve under the crosshair so I can inspect from farther away.

  - [x] **4.2 Cargo on second bar** *(was QOL-07)* — Freight `Cargo …` / `Empty Cargo`. **Done** — Tier 1 + Tier 2 **PASS** (Empty Cargo @ v0.4.56). Look-at **Mass** chip @ v0.4.57.
    > As a yard scout, I want cargo named on the second bar so I know what a car is carrying.

  - [x] **4.3 Hide loco gadget top bar** *(was QOL-08)* — Show loco chips **only** on a usable loco train; otherwise **hide** (no red dash wall). Reuse usable-train + look-at target. Supersedes **1.4** red-null UI. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.14+)
    > As a player, I want loco gadget readouts only whimage.pngimage.pngimage.pngen a loco is relevant — like cab instruments — not a wall of dashes in the yard.

  - [x] **4.4 Extended car inspection — Track ID** — Add current Track ID (e.g. `SM-O6I`) on look-at inspector. Job already ships on the second bar. **Done** — Tier 1 + Tier 2 **PASS** (Bundle B / reconfirmed v0.4.53).
    > As a yard master, I want a car’s Track ID when looking at it so I can identify lost consists.

  - [x] **4.5 Next station distance** *(linear nav)* — **Cut** (2026-07-25). Nearest-other-yard chip (`Next: Farm [2.0 km]`) was clutter and not mainland range guidance. Revisit only with a real range/route story (Epic 3 path / destination).
    > ~~As an engineer, I want distance to the next station when fluids are low…~~

  - [x] **4.6 Station waypoint (foot) + in-zone station chip** — In job-generation zone: `Station {YardID} {bearing} {m}m` or **`here`**. Map coords **cut** (Bundle B.2). **`here`** shares office gate with house AR hide (Bundle C). Fail-closed outside zones. *No minimap.* **Done** — Tier 1 + Tier 2 **PASS** (v0.4.48).
    > As a yard master, I want the local station’s id and which way to walk when I’m in a city zone so I don’t get lost finding the job board.

  - [x] **4.8 Active Job HUD + preview-prep edge** — Inventory-gated `Preview Nm` to Regular destroy (−30 m HUD buffer); taken = Job+Bonus only; Abandoned/Expired → red **Cancelled**. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.52). License warn on held overview (`No license: FH`) — **Done** — Tier 1 + Tier 2 **PASS** (v0.4.53).
    > As a yard master, I want a clear preview-edge warning while I shunt job cars before validating so I don’t lose the ticket saving bonus time, and my taken job bar shows bonus without fake zone death.

  - [x] **4.9 AR wayfinding markers** — Screen-space icons (distinct shapes; color secondary) for (1) last/active loco, (2) in-zone **station office** (not yard center), (3) custom pin (`Home` / `Shift+Home`). `WorldToScreenPoint` + edge clamp when behind; icon + distance only. Replaces foot-nav dependence on Heading/Pos text. **Done** — baseline + Bundles A/C (**v0.4.48**); sticky/on-object/edge through **v0.4.43**; loco radar **4.10**. Office proximity: same gate hides house AR and flips Station chip to **`here`** (~12–14 m apron = accepted “at office”, not a false-hide bug — product decision 2026-07-26).
    > As a yard worker, I want floating markers for my loco, the station office, and my pin so I can run toward them without reading compass math.

  - [x] **4.10 Loco radar** — Nearest other locos as amber sticky AR markers (type · m · place). Place: `SM-…` / `FF-A2P` track alone, or `FF #Y-…` spur with city. Rigid AABB pack (no Venn). Unblocks Bundle E **#23**. *Done* — Tier 1 + Tier 2 **PASS** (v0.4.75).
    > As a yard master, I want to see where other locos are so I can walk to one and MU without searching the whole yard.

  - [x] **4.7 HUD strip IA reorder** — Horizontally **center** every HUD row; stack loco → look-at → always-on nav bar (same chrome as loco/look-at). Loco chip order: `Fuel · Oil · Mass · Grade · Load · Speed · Limit · Motors · Handbrakes · Cars`. **Done** — Tier 1 + Tier 2 **PASS** (v0.4.23).
    > As a driver, I want Speed and Limit in the visual center of the loco bar so I glance there first.

  - [x] **4.11 Backup proximity** — Rear-camera clearance on loco rear tip (`Rear N.Nm` / `Rear —`); **green** ≤0.5 m + couple-scan; **yellow** through 30 m; check-point inset (−0.25 m). No “Couple ready”. Cab look ignored. **Done** — Tier 1 + Tier 2 smoke **PASS** (v**0.5.12**; 3.8 m jump non-repro / user error).
    > As a driver reversing to pick up a train, I want distance before impact and a clear cue when I am close enough to brake and couple.

  - [x] **4.12 Direction-gated proximity** — Reverser gate: **Reverse** → `Rear …`; **Forward** → `Front …` (same ranging/colors); **Neutral** → omit. Tip jumps to new free end after couple. **Done** — Tier 1 + Tier 2 smoke **PASS** (v**0.5.17**).
    > As an engineer, I only want rear proximity when I’m backing; when I’m going forward, show front clearance instead.

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
- [ ] Manual Transmission Override *(DM3 reverser must leave neutral to unlock throttle; DM3 has no MU — knowledge note from Gemini 2026-07-27, not a ship yet)*
- [ ] Mounting Suite / precision mounting
- [ ] Engine Temp Soft Governor *(if distinct from **2.2**)*
- [ ] Speed-limit auto-throttle governor *(soft-cap to % of Limit — same pattern as **2.2**; candidate **2.4**)*
- [ ] **Session reset hotkey** — e.g. Shift+F6: set time ~07:00, clear/reset weather board, invalidate active jobs, refresh available job board (Tier 2 / sandbox).
- [ ] **License-gated re-rail scroll** — see **3.1b** (promoted from debug ask).
- [ ] **AR markers — in-view only (no false edge-stick)** — Station house / loco / pin / radar: do **not** edge-clamp a marker onto nearby geometry when the world target is off-screen or occluded. Only pull to screen edge (or show) when the target is actually in view; otherwise omit or keep off-screen so the icon doesn’t look glued to a car beside you. *(Smoke note 2026-07-28: house @ 120 m appeared on freight car flank.)* Follow-on to **4.9** / **4.10**.
- [x] **2.2 Thermal governor — Hot trigger** — cab yellow = MU Warning; HUD/governor use `MUChainTemperatureState`. Soft-roll Warning **75%** / Critical **55%**. **PASS** @ **0.5.15**. Gemini: `doc/GeminiDocs/A2_Thermal_Governor_TriggerMismatch.md`.

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
- **After any DV patch:** re-run Tier 1, redeploy, confirm Mod Manager Version, then smoke the active Epic 1 checklist in [TEST_PLAN.md](TEST_PLAN.md). Prefer fail-closed (log + self-disable) over crashing the session.
