# Test plan

Two-tier strategy for *Yard Master Suite*. Story IDs match [PM_PLAN.md](PM_PLAN.md). Keep in sync with [AGENT_HANDOFF.md](AGENT_HANDOFF.md).

| Tier | When | Gate |
|------|------|------|
| **1** | Every logic change | `dotnet test` + Release build |
| **2** | In-world HUD / UMM behavior | Deploy + Player.log `T2 …` + on-screen HUD |

**Merge-ready:** Tier 1 always. Stories that touch in-world UI also need Tier 2 before checking Done in PM_PLAN.

---

## Tier 1 — Fast feedback

```bash
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Release
```

**Pass:** All unit tests green; 0 build errors; `build/YardMasterSuite.dll` present; Release produces `dist/YardMasterSuite_v*.zip`.

Requires local `Directory.Build.targets` (from `Directory.Build.targets.example`).

Pure helpers live in `YardMasterSuite.Core` (no Unity/game refs).

---

## Tier 2 — In-game smoke

Requires UMM (`Mods\` under the game root).

```powershell
dotnet build YardMasterSuite.sln -c Debug
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

### Evidence

| Source | Where | Proves |
|--------|--------|--------|
| **Player.log** | `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log` | Load, toggle, discrete `T2 …`, exceptions |
| **UMM Logs** | Mod Manager → Logs | Same lines (subset) |
| **HUD** | Top-left in game | Matches latest `T2` line |

**Logging:** lifecycle + discrete `T2` on meaningful change. No per-frame spam. Speed/grade/tonnage are not logged every tick.

**Retro:** each new Monitor story ships `T2 <topic> …` lines and a checklist below.

### Lifecycle (every session)

- `[YardMasterSuite] Version '<info.json>'. Loading.` *(also HUD `v…` chip)*
- `… enabled (Monitor HUD).` → `Active.`
- Off → `disabled.` → `Inactive.` · On again → `enabled` → `Active.`
- No YardMasterSuite exceptions / stack traces

---

## Story checklists

### 1.4 Train + local-car HUD *(was CMD-01b)* — `T2 consist` / `T2 local-car`

Top bar = loco-train totals; second bar = car under feet. Look-at = **1.6**.

**Expected `T2 consist`**

| When | Log |
|------|-----|
| First sample, no loco | `T2 consist init (no-loco): — Cars  \|  — Handbrakes` |
| Gain usable loco train | `T2 consist loco: Cars N  \|  Handbrakes M` |
| Lose usable path | `T2 consist no-loco: — Cars  \|  — Handbrakes` |
| Totals change while loco present | `T2 consist change: Cars N  \|  Handbrakes M` |

**Expected `T2 local-car`**

| When | Log |
|------|-----|
| On foot (hidden) | `T2 local-car init (hidden)` |
| Climb onto a car | `T2 local-car appear: Pipe …  \|  Handbrake N  \|  Couplers …  \|  Car #  \|  Job …` |
| Step off | `T2 local-car hide` |
| Fields change on car | `T2 local-car change: …` |

**Sign-off**

- [x] Mod loads; Active; no mod errors
- [ ] No loco — top bar **hidden** (**4.3**; was red dash wall)
- [x] On loco train — live Speed/Grade/Mass/Cars/Handbrakes
- [x] On foot — no second bar
- [x] Stand on car — second bar Pipe / Handbrake / Couplers / Car # / Job #
- [x] Couplers `+` only when fully linked; `-` if incomplete
- [x] Mod Off → On; no exceptions

---

### 1.6 Look-at inspect *(was CMD-01d)* — `T2 look-at`

Look-at **wins** over standing. Locos append `Loco DE6`-style type. **4.1:** spherecast 0.15 m / 250 m *(PASS\*)*.

**Expected `T2 look-at`**

| When | Log |
|------|-----|
| Not pointing at a car | `T2 look-at init (hidden)` / `hide` |
| Point at a car | `T2 look-at appear: …` (+ `Loco …` if loco) |
| Fields change | `T2 look-at change: …` |
| Stand on A, look away | `T2 look-at hide` then `T2 local-car appear: …` |

**Sign-off**

- [x] Mod loads; Active
- [x] On foot, not looking — no second bar
- [x] Point at car — second bar for that car
- [x] Car not on usable train — `Car XX`; top bar per **4.3** (hide when no loco)
- [x] Car on usable train — Car # from loco; top bar can show totals
- [x] Stand on A, look at B — bar shows B; look at sky — A
- [x] Point at loco — `Loco …`; freight omits
- [x] Distant car ~80–250 m resolves *(PASS\*)*
- [~] Freight `Cargo …` / `Empty Cargo` — load PASS; Empty Cargo smoke deferred (**4.2**)

---

### 1.5 Coupler tight/loose *(was CMD-01c)* — `T2 coupler`

Marks: `+` usable · plain `*` loose chain · `-` open · yellow `*` MU warning.

**Sign-off**

- [x] Mod loads; Active
- [x] Uncoupled → `-`
- [x] Coupled + loose chain → distinct mark
- [x] Fully linked → `+`
- [x] Standing or look-at drives marks (look-at wins)
- [x] Mod Off → On; no exceptions

---

### 1.7 Load monitor *(was CMD-02a)* — `T2 power` — **PASS\*** v0.4.15

Top bar `Load N %` after Handbrakes. Yellow ≥80%, red ≥95%.

**Sign-off**

- [x] Mod loads; Active; chip matches deploy
- [x] No usable loco — top bar **hidden** (**4.3**); no Load
- [x] Diesel/electric — live `Load` with throttle (**PASS\*** — saw 1–2% on DE2)
- [ ] Yellow ≥80%, red ≥95% — **deferred** (hard pull not available)
- [ ] Steam / no amps — `— Load` (fail-closed) when top bar visible
- [x] Mod Off → On; no exceptions

---

### 4.3 Hide loco gadget top bar *(was QOL-08)* — **PASS** v0.4.14+

**Sign-off**

- [x] Sky / on foot / freight-only — **no** top bar, **no** red dash wall; `v…` chip still visible
- [x] In loco or look-at usable loco train — top bar live (Speed…Load)
- [x] Look-at freight with no loco path — top bar hidden; second bar can still show
- [x] Mod Off → On; no exceptions

### 1.8 Motor status *(was CMD-02b)* — `T2 power` — **PASS** v0.4.16

Top bar `Motors OK` / `Hot` / `Dead` after Load (green / yellow / red). Dead = fuse off or dead TM; Hot = temp over threshold.

**Sign-off**

- [x] Mod loads; Active; chip matches deploy (`v0.4.16`)
- [x] No usable loco — top bar **hidden**; no Motors
- [x] Diesel/electric cool — green `Motors OK`
- [x] Over-temp — yellow `Motors Hot` (**PASS** — current-state; brief dwell accepted)
- [x] Fuse off or dead TM — red `Motors Dead` (cab TM OFFLINE)
- [ ] Steam / no TM — `— Motors` (fail-closed) when top bar visible
- [ ] `T2 power` includes Motors fragment *(not explicitly confirmed)*
- [ ] Mod Off → On; no exceptions *(not explicitly confirmed)*

**Shipped decision (2026-07-20):** no HUD Hot-entry / hysteresis follow-up. Thermal protection → **Epic 2.2** governor.

### 1.9 Fluid monitor *(was CMD-02c)* — `T2 power` — **PASS** v0.4.18

Top bar `Fuel N %` / `Oil N %` after Motors. Yellow (paired) if either &lt; 20%; red (paired) if either &lt; 5%. Reads `ResourceContainer` FUEL/OIL normalized ports.

**Sign-off**

- [x] Mod loads; Active; chip matches deploy (`v0.4.18`)
- [x] No usable loco — top bar **hidden**; no Fuel/Oil
- [x] Diesel with tanks — live Fuel and Oil % (match cab/service)
- [x] Either &lt; 20% — both Fuel and Oil yellow (forced 19% / 5% smoke)
- [x] Both ≥ 20% — plain (forced 20% + live high %)
- [x] Either &lt; 5% — both Fuel and Oil red (forced 4% smoke)
- [ ] Steam / electric / no tank — `— Fuel` / `— Oil` fail-closed when top bar visible *(deferred)*
- [x] `T2 power` fragment includes Fuel and Oil
- [x] Mod Off → On; no exceptions
- [x] Load-time `GUI.skin` ArgumentException fixed (styles built only in `OnGUI`)

### 1.10 Speed limit — current *(was CMD-03)* — `T2 limit` — **PASS** v0.4.20

Top bar single `Limit N` after Speed. Yellow within 5 km/h of limit; red when over. **Authority:** posted `SignDebug` boards first (digit × 10); geometry fallback.

**Sign-off**

- [x] Mod loads; Active; chip matches deploy (`v0.4.20`)
- [x] No usable loco — top bar **hidden**; no Limit
- [x] Pass board `8` → `Limit 80` (then `6` → `60`, etc.)
- [x] Near limit — yellow `Limit`
- [x] Over limit — red `Limit`
- [x] `T2 limit` changes on Limit/loco only (not every km/h)
- [x] No YardMaster exceptions in Player.log *(Off→On accepted with session toggles)*

### 1.11 Speed limit — next / ↑↓ — `T2 limit` — **PASS** v0.4.23

Same `Limit N` badge; green `^` if next board higher; yellow `v` if next lower; lookahead ≈ max(500 m, speed×6). No second km/h chip.

**Sign-off**

- [x] Chip `v0.4.23`; Active
- [x] Approach a lower board — `Limit … v` before you pass it
- [x] Approach a higher board — `Limit … ^` before you pass it
- [x] After passing — arrow clears; new current Limit
- [x] GYR vs **current** still works (yellow near / red over)
- [x] `T2 limit` includes `^`/`v` when trending; no Speed spam

### 1.12 Personal heading — `T2 heading` — **PASS** v0.4.23

Always-on nav bar: `Heading NE` (16-point). Independent of loco top bar.

**Sign-off**

- [x] Mod loads; Active; chip `v0.4.23`
- [x] On foot / no train HUD — Heading on always-on bar
- [x] Turn in place — label steps through N / NNE / NE / ENE / … (not `°`)
- [x] Face roughly +Z world — `Heading N` *(Unity north)*
- [x] `T2 heading` on point change
- [x] No YardMaster exceptions in Player.log

### 1.13 Player coordinates — `T2 pos` — **PASS** v0.4.23 · HUD chip removed Bundle **B.1** v0.4.31

Originally: always-on nav included flat `Pos x, z` (no height). **B.1:** Pos chip removed from always-on; quiet `T2 pos` debug may remain. Bars hug content width (no fixed min that left empty right pad).

**Sign-off (original chip)**

- [x] `Pos` shows two numbers only
- [x] Walk ~50+ units — `T2 pos change` (not every meter)
- [x] No YardMaster exceptions

**B.1 smoke**

- [x] Always-on has **no** `Pos` chip
- [x] Heading (and Marked/Station when present) still show
- [x] Bar width hugs text (no empty right pad) — `v0.4.31`

### 1.14 Park / return mark — `T2 mark` — **PASS** v0.4.25

Always-on nav: `Home` sets mark; `Shift+Home` clears. Chip `Marked NE Nm` / `Marked here`; omit when unmarked.

**Sign-off**

- [x] Mod loads; Active; chip `v0.4.24` *(label tweak → `v0.4.25`)*
- [x] No mark — Marked chip absent from always-on bar
- [x] Press `Home` — `Marked here` (was Park); walk away — bearing + meters
- [x] `Shift+Home` — Marked chip gone
- [x] No YardMaster exceptions in Player.log *(assumed with clean smoke)*

### 4.4 Track ID on second bar — `T2 local-car` / `T2 look-at` — pending smoke v0.4.26

Second bar includes `Track SM-O6I` (or `— Track` on mainline / unknown).

**Sign-off**

- [ ] Chip `v0.4.26`
- [ ] Look-at / stand on yard car — `Track …` matches a nearby Track ID sign
- [ ] Mainline / no yard ID — `— Track`
- [ ] `T2 local-car` / `T2 look-at` fragments include Track

### 4.5 Next station (fluids) — `T2 next-station` — pending smoke v0.4.26

When Fuel or Oil is yellow/red on a usable loco: optional `Next: … [N.N km]` on loco bar. Omit when fluids OK or start/dest station unknown.

**Sign-off**

- [ ] Fluids OK — no Next chip
- [ ] Fluids low while in a station zone (or on that yard’s track) — Next shows another station + km
- [ ] On mainline with no resolvable start station — Next omitted (fail-closed)
- [ ] `T2 next-station` on appear/change

### 4.6 Station waypoint (foot) — `T2 station` — pending smoke v0.4.26

Always-on: in job-generation zone show `Station {YardID} {bearing} {m}m · x, z` (or `here`). Omit outside zones.

**Sign-off**

- [ ] Outside station zone — Station chip absent
- [ ] Enter station/city zone — Station chip with yard id, bearing/distance, coords
- [ ] Walk toward station center — meters drop; near center → `here`
- [ ] `T2 station` on enter/leave / bearing change

### 4.8 Active Job HUD + preview-prep edge — `T2 job` — retarget after Bundle D

Taken: `Job … · Bonus …` only. Preview prep: `Preview …m` to Regular destroy edge when available jobs and none taken. Cancelled on Abandoned/Expired.

**Sign-off**

- [ ] No taken / no preview risk — job bar absent (or Preview omitted)
- [ ] Take a job — bar shows Job id + Bonus; **no** Zone/Keep meters
- [ ] Abandon / expire taken job — bar shows red **Cancelled**
- [ ] Hold/stage unvalidated jobs (no taken) — Preview meters track tight Regular edge; near edge warn; past → `OUT`
- [ ] `T2 job` on appear/hide / bonus minute / preview edge change

### 4.9 AR wayfinding markers — `T2 ar` — pending smoke v0.4.29

Screen markers: loco / office / pin **PNG icons** (shape primary). Edge clamp when behind. Office = paperwork area (not yard center).

**Sign-off**

- [ ] Chip `v0.4.29`
- [ ] After using a loco — train icon + meters toward that loco
- [ ] Enter station zone — house icon at **office/validator**, not mid-yard cargo
- [ ] `Home` — pin icon; `Shift+Home` clears
- [ ] Turn away — marker clamps to screen edge
- [ ] `T2 ar` shows loco / office / pin set changes

### 4.7 HUD strip IA — **PASS** v0.4.23

All rows centered. Stack: loco → look-at → always-on bar (same chrome).

**Sign-off**

- [x] Usable train — loco bar centered; Speed/Limit mid-string
- [x] Look-at bar centered under loco
- [x] Always-on bar centered under the lowest other bar (or alone when others hidden)
- [x] Always-on readable (dark bar background)
- [x] Chip `v0.4.23`

Recovery: [modding.md](doc/requirements/modding.md).
