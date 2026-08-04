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

- `[YardMasterSuite] Version '<info.json>'. Loading.` *(ship # also in UMM Mod Manager — not on HUD)*
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
- [x] Freight `Cargo …` / `Empty Cargo` — load PASS; Empty Cargo **PASS** @ 0.4.56 (F6 dump)

---

### 1.5 Coupler tight/loose *(was CMD-01c)* — `T2 coupler`

Marks: `-` clear · **red** `*` tow not ready · **yellow** `*` loco↔loco MU open · white `+` tow-ready (no MU required) · **blue** `+` loco↔loco with MU (Player.log: `*` / `*Y` / `+`).

**Sign-off**

- [x] Mod loads; Active
- [x] Uncoupled → `-`
- [x] Coupled + loose chain → distinct mark
- [x] Fully linked → `+`
- [x] Standing or look-at drives marks (look-at wins)
- [x] Mod Off → On; no exceptions

### Bundle E #23 — Loco↔loco coupler colors — **PASS** v0.4.80

**Red** `*` = tow not ready (any unfinished step, loco or car). **Yellow** `*` = loco↔loco tow-ready, MU open (license-agnostic). **White** `+` = tow-ready when MU not required. **Blue** `+` = loco↔loco with MU. Clear = `-`.

**Sign-off**

- [x] UMM **0.4.80** Active
- [x] Clear → `F- R-`
- [x] Mid-couple (cock open, air-only, …) → **red** `*`
- [x] Mech+air ready, MU open → **yellow** `*` (both locos)
- [x] MU plugged → **blue** `+`
- [x] Mixed ends (e.g. MU team + air-only rear) → blue `+` / red `*` as appropriate
- [x] Optional: car↔car / loco↔car white `+` confirm — **PASS** @ 0.4.80 (player pics + Player.log)
- [x] Mod Off → On; no exceptions

### Bundle E follow-up — loco↔freight coupler smoke — **PASS** v0.4.80

Confirm **red** mid-couple / **white `+`** tow-ready on car ends — **PASS** (loco↔freight + car↔car; Off→On clean).

### Bundle E #22 — Load live hard-pull — **WAIVED** 2026-07-26

F6 force yellow/red already **PASS**. Dedicated live hard-pull setup not worth it; watch colors in normal play.

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

### 1.16 Recommended Limit — **CUT** (2026-08-03)

`(Recommended)` auto-adopt ruined QoL (80↔50↔80). Soft **Brake** cue may remain. Historical Tier 2 notes below are archive only.

### 1.17 Posted Limit + Next look-ahead — `T2 limit` — **PASS @ 0.5.104**

HUD: `Limit N | Next M (Xm)` (or `X.Xkm`). Limit = sticky posted only — no `(Posted)` / `(Recommended)`. Soft Brake chip **CUT**. Geometry-ahead boards **CUT**. World board index seeds Limit from behind only (not Next).

**Sign-off — PASS @ 0.5.104 (player 2026-08-03)**

- [x] Mod Manager shows **0.5.104** — `Limit N` stable (no brief Limit thrash; no `(Posted)` label)
- [x] When a different board is on path: `Next M (distance)` appears with sensible meters/km
- [x] No `(Recommended)`, no `Brake N in …` chip
- [x] Mod Off → On; no exceptions
- [ ] **#4 on ice** — blind behind-seed / empty Limit at startup if board &gt;600 m behind may be OK; reopen if Limit stays empty with board &lt;600 m behind

### Stress RAG HUD chip — **PASS @ 0.5.105**

Train-bar chip after Motors: `Stress N %` from worse of coupler `TrainStress.stress` / `derailBuildUp` vs game derail thresholds. RAG: green &lt;80%, yellow ≥80%, red ≥95% (same bands as Load). Fail-closed `— Stress` when thresholds unusable. **Not** consist-in-zone / Limit occupancy — live game coupler/derail physics only.

**Sign-off — PASS @ 0.5.105 (player 2026-08-03)**

- [x] Mod Manager shows **0.5.105**
- [x] In loco: `Stress N %` after Motors (green at rest; rose under abuse → yellow → red → derail)
- [x] Solo DE2 confirmed (`Cars 0`) — Stress tracks loco `TrainStress`, not train length / board occupancy
- [x] No exceptions; Mod Off → On clean

### 1.16 archive (Recommended adopt — do not resume)

Look-ahead boards come from the **route ahead** (`TrackPathAhead`, switches as thrown) with **arc** route distance (Bezier span, not chord). On-path facing uses the **route tangent at the board** (not loco heading — 0.5.52 late `fDot=-0.39`). On-path boards no longer need the right-hand gate (0.5.51). Braking has a soft budget (yellow + adopt) and a hard budget (red), both reduced by downhill gravity; grade beating hard braking shows `RUNAWAY`. Sticky Limit is released only by **passing** a board. Labels `(Posted)` / `(Recommended)` / `(Geometry)`; 5 s loosen-hold. Adopted Recommended uses release-lead hysteresis (0.5.54) so a far restriction does not 30↔60 chatter on grade wobble. **0.5.55:** speed-floor release + scan-drop + standstill freeze. **0.5.56:** adopt-time grade kept for sticky release; sticky never clobbered by looser Resolve. **0.5.57:** rich drive/brake debug. **0.5.58:** honest hard budgets + calmer color + no duplicate target. **0.5.59:** dynamic ≤6 km route scan; tightest severe board gets Brake at mass/grade/type slowdown time ×1.5, so an intermediate 60 cannot hide a farther 30. **0.5.60:** Brake target qualifies against **speed** (the Limit chip adopting it no longer silences it); window is the worse of comfortable slowdown and guaranteed heavy-application room **+50%**; planning grade is safety-biased and the on-screen target cannot blink out. **0.5.61 (root cause):** `T2 limit`'s new `scan=/path=/reach=` diagnostics proved the 0.5.60 warning-window math was correct but starved of data — `path=7seg reach=6971m` shows the route walk already knew the board was there, while `ahead=2 min=70@465m` shows the scan could not *see* it because its `SignDebug` prop had not streamed into the scene yet (Derail Valley streams sign decor near the train; the `RailTrack` graph/curve is not streamed). Fix bypasses sign streaming entirely for early detection: every walked route segment's own curve radius is scanned via the existing SignPlacer-ladder geometry math (`SpeedLimitGeometryZones.TryGoverningZone`), synthesizing an `AheadBoard` from geometry alone wherever a real sign has not loaded yet (`T2 limit` gains `geo=N`). Also fixes issue #2 from the same session: `MinTargetDeltaKmh` (Brake's own "close enough, don't nag" gate) now equals `SpeedLimitDisplay.NearAboveKmh` (5, was 3) so Brake cannot go red while the Limit chip is still only yellow.

**Archive sign-off (1.16 Recommended)**

- [ ] Mod Manager shows `0.5.68` — Tier 2 **held** (dial **0.80**, margin 10%, Brake cap 4.5 km; Align Google Maps first)
- [ ] Mod Manager shows `0.5.67` — superseded (Drive chip; corpus capture)
- [ ] Mod Manager shows `0.5.66` — superseded before smoke (Drive chip added for long-haul pacing)
- [x] Mod Manager shows `0.5.65` — Tier 2 **partial** (Brake 30 without Limit 30 ×312; still sticky posted; stress thr?)
- [x] Mod Manager shows `0.5.64` — Tier 2 **FAIL** stickiness (posted 60→stayed 30; geo along≈2665 lead≈3556; `thr=0/1` stress %)
- [x] Mod Manager shows `0.5.63` — Tier 2 **FAIL** for stickiness (dial 0.1 still too conservative; `src=geo along≈2600 lead≈3500` under posted 60)
- [ ] Mod Manager shows `0.5.62` — superseded before smoke
- [ ] Mod Manager shows `0.5.61` — Tier 2 **partial** (geometry-ahead caught far 30s / `geo≥1`, but Limit stayed Recommended 30 too long under posted 70/80 — dial at 0.5.62)
- [x] Mod Manager shows `0.5.60` — Tier 2 **FAIL** (70→30 still unwarned — root cause was sign streaming, not window math; see 0.5.61)
- [x] Mod Manager shows `0.5.59` — Tier 2 **FAIL** (60→50→60 chip chatter; no `Brake 30` under `Limit 30 (Recommended)`; 70→30 still unwarned)
- [ ] Mod Manager shows `0.5.58` — Tier 2 pending (honest hard lead + yellow band + no duplicate Brake target)
- [ ] Mod Manager shows `0.5.57` — Tier 2 pending (drive/brake debug for lead tuning)
- [ ] Mod Manager shows `0.5.56` — Tier 2 pending (sticky grade / no-clobber)
- [x] Mod Manager shows `0.5.55` — Tier 2 **FAIL** (standstill OK; residual 30↔60/70 Recommended thrash)
- [x] Mod Manager shows `0.5.54` — Tier 2 **PASS** (grade-edge 30↔60 ship)
- [ ] On-path drop on a **curve** (e.g. `'4'=40`): `take` in Player.log with `fDot ≲ −0.5` while still tens of meters out — **not** skip at ~12 m then Recommended at &lt;1 m
- [ ] On-path drop (e.g. 60→30): Brake / `(Recommended)` appears with soft lead — **not** only in the last ~30 m / ~10 s
- [ ] **Sticky:** after passing a `4` board, Limit stays 40 — an older `6`/`8` board behind never raises it (this caused the 0.5.50 derail)
- [ ] **Downgrade:** on a negative grade the Brake chip / `(Recommended)` drop arrives materially earlier than on the flat; `T2 limit` detail shows `grade=-x.x%`
- [ ] Steep descent that brakes cannot hold shows `RUNAWAY — Brake N NOW` (red)
- [ ] **Path:** boards on other branches no longer appear (`track=n` in traces); no 40 from a board ~700 m off to the side
- [ ] Boards just past the next switch **do** appear, with sane route distance
- [ ] Thrown diverge on `7 4`: warns before the frog; through-set stays on the through number
- [ ] At a posted 90 you read `Limit 90 (Posted)`; adopt happens nearer than 0.5.50 (lead ×1.15, not ×3.5)
- [ ] Same km/h: `(Recommended)` → `(Posted)` once; no bounce; no 50↔60 flash
- [x] **No 30↔60 Recommended chatter** on a mild grade wobble once a far restriction has been adopted (release lead holds until clearly outside) — Tier 2 **PASS** @ **0.5.54** (Player.log: zero adjacent 30↔60 flips)
- [x] Standstill does **not** thrash 40↔80 from facing jitter — Tier 2 **PASS** @ **0.5.55** (zero Speed-0 Limit flips)
- [ ] **No 30↔60 / 50↔60 Recommended thrash** while grade eases or boards briefly leave the ahead scan — Tier 2 @ **0.5.56**
- [ ] Limit 50 is yellow from **40–55** inclusive and red at **56+**; `Limit 60 (Recommended)` does not also show `Brake 60 in …` — Tier 2 @ **0.5.58**
- [ ] Light DE2 downgrade: red/`RUNAWAY` arrives materially earlier than 0.5.57; following the first cue avoids the 69→49-at-30-board failure — Tier 2 @ **0.5.58**
- [ ] At ~70 with a near 60 and farther 30, `Brake 30` appears around the DE2/mass/grade estimated slowdown time **plus 25%** (was 50% through 0.5.64), before Limit adopts 30; `T2 limit` includes `type=LocoDE2` — Tier 2 @ **0.5.65**
- [ ] `Limit 30 (Recommended)` **and** `Brake 30 in … s` are shown together — adopting the number never removes the countdown — Tier 2 @ **0.5.60**
- [ ] Cruising 50–70 toward a 30: the chip is up **kilometres** out (≈250 s+ on a descent), and following it needs no hard application — Tier 2 @ **0.5.60**
- [ ] The Brake chip does **not** blink on/off between frames while grade wobbles; `T2 limit` `planGrade=` moves smoothly even when `grade=` jumps — Tier 2 @ **0.5.60**
- [ ] If a warning still arrives late, `T2 limit` `reach=`/`min=` shows whether the route walk had even found the board — Tier 2 @ **0.5.60**
- [ ] **The 70→30 case, for real this time:** repeat the exact route from the 0.5.60 failure. `Brake 30` (or `Limit 30 (Recommended)`) must appear kilometres out, well before the sign is close enough to render on screen. `T2 limit` should show `geo=1` (or more) on the frame the warning first appears, and `min=30@…` should show a large distance the very first time it is ever non-`—`, not just once the sign has streamed in — Tier 2 @ **0.5.61+**
- [ ] **Dial 0.40 + align:** when `adv=… 30` appears, Limit must show `30 (Recommended)` (never Posted-only). Under mild 60/70/80, Limit should not sit on 30 for km after Brake clears. If sticky, use `suggest=`. — Tier 2 @ **0.5.67**
- [ ] At ~70 with a farther 30, `Brake 30` still appears with useful lead under the **25%** planning buffer — Tier 2 @ **0.5.67**
- [ ] **Drive goals (use HUD `Drive` chip):** light-engine session **≥12 km** total; prefer **2+ routes** (e.g. mainline stretch **and** Harbor Town / other steep grade). Per route, aim **≥5 km** or until you’ve seen a posted step-down + sticky release. Normal pacing; optional overspeed OK. Agent tunes from logs. Chip resets when leaving the world. — Tier 2 @ **0.5.67**
- [ ] `Limit 40` (or `40 (Recommended)`) while cruising 41–45 (yellow, not red) shows **no** `Brake 40` chip; going to 46+ (red) does show it — Tier 2 @ **0.5.61+**
- [ ] No new false-positive Brake/Recommended chatter from ordinary mainline curves that were never signed (geometry-ahead scan must not nag on curves loose enough that the game itself never placed a board there)
- [ ] Straight mainline does **not** flicker to 40 from micro-kinks
- [ ] Clean light-engine run, **then** a loaded freight downgrade run

### 1.10 Speed limit — current *(was CMD-03)* — `T2 limit` — **PASS** v0.4.20

Top bar single `Limit N` after Speed. Yellow within 5 km/h of limit; red when over. **Authority:** posted `SignDebug` boards only (digit × 10; sticky; facing our travel direction; dual junction via branch). No geometry fallback; ignore opposite-direction boards.

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

### 1.12 Personal heading — `T2 heading` — **PASS** v0.4.23 · in-world only **v0.4.34** · no HUD version **v0.4.35**

Always-on nav bar: `Heading NE` (16-point). Independent of loco top bar. **Not** drawn on launcher/menus. **No** mod `v…` chip on HUD.

**Sign-off**

- [x] Mod loads; Active; chip `v0.4.23`
- [x] On foot / no train HUD — Heading on always-on bar
- [x] Turn in place — label steps through N / NNE / NE / ENE / … (not `°`)
- [x] Face roughly +Z world — `Heading N` *(Unity north)*
- [x] `T2 heading` on point change
- [x] No YardMaster exceptions in Player.log
- [x] Launcher / main menu — **no** Monitor HUD / no `— Heading` (`v0.4.34`)
- [x] In world — Heading still on always-on bar
- [x] In world — always-on has **no** `v…` chip; Mod Manager shows **0.4.35**

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

### 4.4 Track ID on second bar — `T2 local-car` / `T2 look-at` — Bundle **B.3** v0.4.33 omits empty

Second bar includes `Track SM-O6I` on yard tracks. **Omit** the Track segment on mainline / unknown (no `— Track`).

**Sign-off**

- [x] Chip `v0.4.33`
- [x] Look-at / stand on yard car — `Track …` matches a nearby Track ID sign
- [x] Mainline / no yard ID — **no** Track segment (not `— Track`)
- [ ] `T2 local-car` / `T2 look-at` fragments include Track only when present

### 4.5 Next station (fluids) — **CUT** v0.4.55 · cut smoke **PASS**

Former `Next: … [N km]` on loco bar when fluids low — **removed**. Smoke A1–6 **PASS** @ 0.4.54 then product cut. Cut verify @ **0.4.55** (player 2026-07-25): F8/F9 low fluids, mainline — **no** `Next:` chip.

**Fluid HUD debug inject (kept):** in-world, usable loco (debug gate on — **Shift+F1** toggles; bottom legend HUD when on):
- **F8** — cycle **Fluids**: real → low oil / full fuel → low fuel / full oil → both low → both full → real
- Player.log: `T2 fluid-debug: …`

**Cargo / Load / Coupler / license / turntable / loco-license debug:** one key per concern (cycle). Look-at or stand on a car in the consist for F7:
- **F5** — loco licenses: **real → DH4 only → DH4+DE6 → real** *(replaces lighter; game id DH4 not DE4)*
- **F6** — loco **Load %** HUD: off → **85%** yellow → **97%** red → off *(F10 remapped — Windows often eats F10)*
  *(Load % is traction amps, not car Mass — sit in DE2/DE6.)*
- **F7** — **all freight** in the coupled trainset: **unload (tare) ↔ full load** (game UnloadCargo/LoadCargo events)
- **F9** — coupler HUD for #23: off → front `F*` yellow → rear `R*` yellow → both → off
- **F11** — grant **all** obtainable general + job licenses ↔ restore pre-press snapshot (**real**)
- **PgUp / PgDn** — within turntable **SearchRadius + 15 m** (or look-at); hold = bar/lever rate; tap = assist if ≤2 m from lock
- Player.log: `T2 cargo-debug …` / `T2 load-debug: …` / `T2 coupler-debug: …` / `T2 turntable …` / `T2 loco-license-debug …` / `T2 license-debug …`

**F5 DH4/DE6 license cycle — smoke @ v0.6.5 — Tier 2 PASS (2026-08-03)**

- [x] UMM **0.6.5** Active; debug legend `F5 DH4/DE6`
- [x] Career Manager: F5 → DH4 OWNED only → both OWNED → both priced again (real)

**Cut sign-off — PASS @ 0.4.55**

- [x] UMM **0.4.55** Active
- [x] F8 Oil 5% — no `Next:`
- [x] F9 Fuel 5% — no `Next:`
- [x] Mainline low fluid — no `Next:`

### 4.6 Station waypoint (foot) — `T2 station` — Bundle **C** **PASS** v0.4.48

Always-on: in job-generation zone show `Station {YardID} {bearing} {m}m` (or `here`). **No** map coords. Omit outside zones. **`here`** = same office gate as house AR hide (AABB / 20 m fallback). Apron flip to `here` (~12–14 m) is **accepted**.

**Sign-off**

- [ ] Outside station zone — Station chip absent
- [x] Enter station/city zone — Station chip with yard id + bearing/distance (**no** `· x, z`) — `v0.4.32`
- [x] Walk to office — house icon hides **and** chip flips to `Station … here` at the same moment (v0.4.48)
- [ ] `T2 station` on enter/leave / bearing change

### 4.8 Active Job HUD + preview-prep edge — `T2 job` — Bundle **D** v0.4.52 · pending B re-smoke

**Build:** UMM **Yard Master Suite 0.4.52**.  
**Primary story (B):** warn while shunting with job paperwork **in inventory** (any overview/booklet — multi-job OK).  
**Colors:** Preview warn &lt; **200 m** (yellow); critical / OUT &lt; **50 m** (red). Cancelled flash ~**8 s**.  
**API:** `currentJobs` empty + inventory has ≥1 `JobOverview`/`JobBooklet` → player vs that job’s origin Regular destroy (**not** generation zone; **not** board-only). HUD meters include **−30 m** safety buffer. Game wipe distance **unchanged**.  
**B3 root cause (0.4.50):** Preview used gen-zone → vanished ~400 m early. Fixed 0.4.51+.  
**0.4.52:** Preview only while holding job item(s); empty hands/board alone → no Preview.

#### Setup

1. Load world → Mod Manager **0.4.52 Active**.
2. Enter station; **pick up** one or more job overviews into inventory/hands (**do not** validate yet).
3. Confirm Preview shows; drop all job items → Preview **gone**.

#### A — Quiet / fail-closed — **PASS** @ 0.4.49

| Step | Action | Expect |
|------|--------|--------|
| A1 | Leave station zone, no taken job | No job bar / no Preview |
| A2 | In zone, empty board, no taken | No job bar |

#### B — Pre-validate prep (PRIMARY)

Goal: assemble job-numbered cars (A, then B onto same loco) **before** validating, to save bonus clock. Crossing Regular edge wipes `availableJobs` / staged cars.

| Step | Action | Expect |
|------|--------|--------|
| B1 | Hold ≥1 job overview (no validated job) | `Preview Nm` (~800 m at SM OK). Empty inventory → **no** Preview |
| B2 | Hold several overviews; shunt deep in yard | Preview stays; meters comfortable |
| B3 | Drive out past where Station chip vanishes | Preview **stays**; meters drop; &lt;200 m yellow; &lt;50 m red |
| B4 | Cross wipe line (HUD has −30 m buffer) | `Preview OUT` (red); game wipe distance unchanged |
| B5 | Step back with overview still in inventory | `Preview Nm` returns (not Cancelled) |
| B6 | Validate / take | Preview → `Job … \| Bonus …` only (**no** Zone) |

#### B0 — Taken bar regression (table stakes)

| Step | Action | Expect |
|------|--------|--------|
| B0.1 | With taken job, drive far | Still Job + Bonus; **no** Zone; distance ≠ Cancelled |

#### C — Cancelled — **PASS** @ 0.4.49

| Step | Action | Expect |
|------|--------|--------|
| C1 | Trash / abandon taken booklet | Red `Job … \| Cancelled` ~8 s |

#### Sign-off — Bundle D **PASS** @ v0.4.52 (player 2026-07-24)

- [x] A — fail-closed (0.4.49)
- [x] UMM **0.4.52** Active
- [x] B1 / inventory — Preview with overview in hand (~639–768 m at SM)
- [x] B1b — drop/stow all job items → Preview gone (player PASS)
- [x] B3 — Preview stays past ~400 m; yellow/red/OUT through Regular edge
- [x] B4b — wipe / EXPIRED after too far from origin (player + screenshot)
- [x] B5 — reverse from red/OUT, return, validate OK (player PASS)
- [x] B6 — after validate: `Job … | Bonus …` only (screenshot SM-FH-83)
- [x] B0 — taken job drive: Job+Bonus, no Preview/Zone (screenshot)
- [x] C — Cancelled (0.4.49)

**Defer:** ~~delivery clear (#18)~~ **PASS** (player 2026-07-24). **License warn:** v0.4.53 Tier 2 **PASS**.

### 4.8b License warn on held overview — `T2 job` — v0.4.53 · **PASS**

**Build:** UMM **Yard Master Suite 0.4.53**.  
**Behavior:** While holding JobOverview/JobBooklet and player lacks required job licenses → red **`No license: FH`** (comma-separated if several). Still shows **Preview** when edge applies (`No license: SH  |  Preview OUT`). Taken / Cancelled unchanged. Preview wipe distance unchanged.

#### Smoke

1. Mod Manager **0.4.53 Active**.
2. Career (or save) **without** Freight Haul (or pick a job type you lack).
3. Pick up that overview (do not validate).
4. Expect Active Job bar: **`No license: FH`** (red). With Preview in range → both chips.
5. Drop paperwork → warn gone. Validate a licensed job → taken Job+Bonus only (no license chip).

**Sign-off — PASS @ 0.4.53 (player 2026-07-24)**

- [x] UMM **0.4.53** Active
- [x] Missing license → `No license: …` while holding overview (SH and other types exercised)
- [x] Licensed overview / put-down → no license chip; Preview alone OK
- [x] Preview still works alongside warn when near Regular edge


### 4.9 AR wayfinding markers — `T2 ar` — **PASS** (closed 2026-07-26)

Screen markers: loco / office / pin **PNG icons** (shape primary). Edge clamp when behind. Office = paperwork area (not yard center). Bundles **A** (sticky/on-object/edge + loco hide) + **C** (`here` / house hide same gate) + baseline icons. **Product:** apron ~12–14 m from Station Office door → house icon hides **and** Station chip shows `here` together — accepted “at office,” not a false-hide bug.

**Sign-off**

- [x] Baseline icons + distance (loco / office / pin) — shipped; Bundles A–C smoke **PASS** through **v0.4.48**
- [x] After using a loco — train icon + meters (loco hide in-cab **PASS** @ v0.4.47)
- [x] Enter station zone — house icon at **office/validator**, not mid-yard cargo
- [x] `Home` — pin icon; `Shift+Home` clears
- [x] Turn away — marker clamps / sticky-row edge (Bundle A **PASS**)
- [x] Office proximity — house hide + `Station … here` same gate (**PASS** Bundle C v0.4.48; accepted 2026-07-26)
- [x] `T2 ar` shows loco / office / pin set changes

### 4.10 Loco radar — **PASS** v0.4.75

Nearest **other** locos as amber AR markers on the sticky locator bar. Place: `SM-T12P` / `FF-A2P` or `FF #Y-…` (city + spur). Rigid AABB pack. Unblocks Bundle E **#23**.

**Sign-off**

- [x] UMM **0.4.75** Active
- [x] Radar on sticky bar — full plate visible
- [x] Turntable / edge pair — two squares side-by-side, no Venn
- [x] `SM-…` / `FF-A2P` track only; spur → **`FF #Y-…`** (city + track)
- [x] No YardMaster exceptions in Player.log

### 4.7 HUD strip IA — **PASS** v0.4.23

All rows centered. Stack: loco → look-at → always-on bar (same chrome).

**Sign-off**

- [x] Usable train — loco bar centered; Speed/Limit mid-string
- [x] Look-at bar centered under loco
- [x] Always-on bar centered under the lowest other bar (or alone when others hidden)
- [x] Always-on readable (dark bar background)
- [x] Chip `v0.4.23`

Recovery: [modding.md](doc/requirements/modding.md).

---

## Epic 2 — Governor Mode

### 2.1 Three-Gate helper — **PASS** v0.4.81 (Tier 1)

`ThreeGate.TryApply`: Integrity → State Registry → Safety → Soft Write. Fail closed (no write on gate fail; soft write `false`/throw → SoftWrite abort). Safety for governors; other callers pass `true`.

**Sign-off**

- [x] Tier 1 — abort each gate without calling write; apply when all pass; throw → SoftWrite abort
- [x] Tier 2 — **N/A** until **2.2** soft-writes in-world

### 2.2 Thermal governor — **PASS** v0.5.15

**Hot trigger:** cab MU TM TEMP Warning/Critical (`MUChainTemperatureState`).

**Soft-cap:** roll throttle down **5%/s** toward:
- **Warning** → **75%**
- **Critical** → **55%**

Cool/Dead → release. Player.log: `T2 thermal: soft-cap → … (Warning|Critical)` / `cap release`.

**Sign-off**

- [x] UMM **0.5.15** Active
- [x] Cab TM TEMP yellow → HUD `Motors Hot` (same moment)
- [x] Motors OK / cool — throttle free
- [x] Warning + high throttle — soft-rolls toward ≤75%; `soft-cap → 0.75 (Warning)` (Player.log)
- [x] Critical — rolls toward ≤55% (log hit once during smoke)
- [x] Cool again — `cap release`; throttle free
- [x] Mod Off → On; no exceptions / no stuck throttle (iterative smoke)

### 2.2b Thermal debug inject + GOV flash — **PASS** v0.5.16

**Shift+F1** debug on, sit in loco, **F10** cycles: off → **Motors Hot 50%** (Warning) → Critical → off. Governor soft-rolls as usual. While capping, Motors chip flashes **▼GOV**.

**Sign-off**

- [x] UMM **0.5.16** Active
- [x] F10 → `Motors Hot 50%`; soft-cap `0.75 (Warning)` (Player.log)
- [x] F10 → Critical → soft-cap `0.55` (Player.log)
- [x] Real heat (debug off) → `soft-cap → 0.75 (Warning)` (Player.log)
- [x] Cap release on cool / off; iterative smoke **PASS** 2026-07-28

### 2.3 Auto-brake governor — **PASS** v0.5.25

**Trigger:** engine **on → off** (falling edge) on lead usable loco — **stopped or moving**.

**Action:** soft-roll **train + independent** toward **full** and **throttle toward idle** at **20%/s**. Handbrakes untouched. **Never** auto-releases on engine start. Want to coast → leave engine **on**.

**Player.log:** `T2 autobrake: applying` / `apply done` / `abort …`

**Sign-off**

- [x] UMM **0.5.25** Active
- [x] Stationary / rolling, brakes open + throttle up, engine **Off** → `applying` → levers soft-close + throttle idle → `apply done`
- [x] Engine **On** → no auto-release from this governor
- [x] Already secured + Off → quiet
- [x] Mod Off → On; no exceptions (iterative smoke **PASS** 2026-07-28)

---

## Epic 3 — Yard Master / Dispatcher

### 3.5 Align Route (CTC) — smoke @ **v0.5.101** — Tier 2 **PASS** (2026-08-03)

**Keys / UI:** `Insert` = desk. Pathfinding on **Set dest** / **Recheck** / **Align Route** (or `End` pin). City/track dropdowns. **Recheck** = recompute from current track to saved dest. Stale when leaving corridor **or** a planned switch is flipped. Chip: `Path OK | ETA … | rem … | trip …%` (`lag` mode).

**License:** **Dispatcher** (`Dispatcher1`). Dijkstra = **travel seconds**; Align re-evals frozen corridor (no full graph Invalidate after throw).

**Player.log:** `T2 path: detail` / `costcheck …` / `eta-refresh`; keep `T2 limit` Drive fields for 1.16 corpus.

**Slice smoke**

- [x] **#1 Mapping:** cold Insert → mapping banner; world interactive; cities fill — Tier 2 **PASS** @ **0.5.80**
- [x] **#3 ETA:** accel/brake gradual; `lag`; rem/trip sane — Tier 2 **PASS** @ **0.5.101**
- [x] **#4 Transit:** HB Path OK on clear Through (not pocket) — Tier 2 **PASS** @ **0.5.101**. **Waived (player):** staged “no free I→O ⇒ skip whole city” / park-a-car Recheck — re-open if seen in normal play; Tier 1 still locks occupied Through + NoPath.

**Full sign-off**

- [x] UMM **0.5.101** Active
- [x] Set dest → Path / Facing / Exit / **ETA**; Align → threw N → **Path OK**
- [x] Drive Path OK; leave → Path stale → Recheck/Align (incl. W-0416 dual-branch fix; planned switch flip → stale)
- [x] Dispatcher → Align throws; Align again → already clear / threw 0; Mod Off → On; no exceptions
- [x] Arrival → `ETA 0s` / rem 0 / trip 100% (earlier haul)

### 3.6 Digital Switch List — smoke @ **v0.6.2** — Tier 2 **PASS** (2026-08-03)

**Keys / UI:** `Insert` → desk → **Switch List** tab. Job dropdown = taken (`currentJobs`) + held inventory tickets. **Load Switch List** → Prep / Transit / Delivery. **Align step** = set dest + Align Route for current leg. **Next** advances manually.

**Player.log:** `T2 switch-list: loaded …` / `align step …` / `next` / `complete`; Align via `T2 align: threw N`

**Sign-off**

- [x] UMM **0.6.2** Active (Mod Manager Version — not HUD)
- [x] Desk → Switch List → Refresh shows a taken or held job
- [x] Load Switch List → ≥3 steps (Prep → Transit → Delivery) with real track IDs
- [x] Align step → Path OK / Facing; switches throw (`threw 2` on Prep @ FF-C3O)
- [x] Next advances steps (Transit / Delivery)
- [x] Occupancy fix: Prep Align to track with job cars (no false “no free through / occupancy”)
- [ ] Clear / Hide; Mod Off → On; no exceptions *(covered in Align smoke; optional recheck)*
- [x] Fail closed: unreadable tracks → status error *(earlier 0.6.1 before `startingTrack` fix)*

### 3.1 Job-cars teleport + Station Snap — smoke @ **v0.6.4** — Tier 2 **PASS** (2026-08-03)

**Keys / UI:** `Insert` → **Switch List**. Select held/taken job → **Move … here** (place mode). **Look at** destination track → chip `PLACE OK · N cars · TRACK`. **Flip** · **Confirm place** → `TeleportTrainset`. **Snap office** / **Return**.

**Player.log:** `T2 teleport: place · …` / `started · N cars → TRACK · aim=(x,z)` / `complete`; `T2 snap: …`

**Sign-off**

- [x] UMM **0.6.4** Active
- [x] Move → PLACE chip; look-at target; Confirm → multi-car teleport complete (×3 in log)
- [x] Snap office / Return logged; under-mesh spawn = known polish follow-on
- [x] No place ghost yet (MVP chip-only — follow-on)
- [x] Never deletes cars; no exceptions on confirm path

### 3.1b License-gated loco spawn — smoke @ **v0.6.12**

**Keys:** **`/`** enter/exit · mouse wheel or **[ ]** scroll · **R** flip · **Enter** confirm · Shift+/ cancel.

**Expect:** blue/red re-rail ghost on rails, drawn by our own `CarDestinationHighlighter` built from the game's highlighter prefab + place materials; scroll HandCar + licensed (no Spug); chip matches.

**Sign-off**

- [x] Wheel scrolls car type (PASS @ 0.6.11)
- [ ] UMM **0.6.12** Active
- [ ] `/` → visible blue/red ghost on track
- [ ] Enter spawns; cancel clears ghost
- [ ] Player.log: `ghost built from CommsRadioCrewVehicle` (or `RerailController`), **no** `no highlighter template`

### 3.4 Path tracer: manual check — **demoted** (engine only; see 3.5)

---

## Epic 4 — HUD quality (reopened)

### 4.11 Backup proximity — PASS v0.5.12

Check-point clearance (tenths). **Green** ≤**0.5 m** + couple-scan. **Yellow** through **30.0 m**; plain beyond. Cab look ignored; no “Couple ready”.

**Sign-off**

- [x] UMM **0.5.12** Active
- [x] Green ≤0.5 m / yellow through 30 m / tenths (iterative smoke **PASS**)
- [x] 3.8 m jump — **non-repro** (player: likely user error; waived)
- [x] No “Couple ready”; fully coupled → chip omitted
- [x] Mod Off → On; no exceptions

### 4.12 Direction-gated proximity — **PASS** v0.5.17

Reverser gate: **Reverse** → `Rear …` / `Rear —`; **Forward** → `Front …` / `Front —`; **Neutral** → omit. After couple, tip jumps to new free end (not stuck on locked joint).

**Sign-off**

- [x] UMM **0.5.17** Active
- [x] Reverse → `Rear` / `Rear —`; Forward → `Front` / `Front —`
- [x] Neutral → no proximity chip (AR loco radar may remain)
- [x] After couple → reading moves to new free tip (not stuck green on locked joint)
- [x] Mod Off → On; no exceptions
- [x] Iterative smoke **PASS** 2026-07-28

### 1.15 Consist free-motion — **PASS** v0.5.20

Other locos vs cab: **quiet** when synced; **yellow** `MU idle` if either unit off/Neutral (brakes match); **red** `MU desync` on brake/ind-brake fight or both on+in-gear mismatch. MU cable syncs reverser/throttle — unplug MU to desync those.

**Sign-off**

- [x] UMM **0.5.20** Active
- [x] Single loco / synced → no chip
- [x] Off/Neutral (brakes match) → yellow both cabs
- [x] Both in gear + wrong reverser/throttle → red
- [x] Train/independent brake mismatch → red
- [x] Match again → chip clears
- [x] Mod Off → On; no exceptions
- [x] Iterative smoke **PASS** 2026-07-28
