# Test plan (TEST_PLAN.md)

Two-tier strategy for *Yard Master Suite*. Keep in sync with [AGENT_HANDOFF.md](AGENT_HANDOFF.md).

---

## Tier 1: Fast feedback (unit tests + build)

```bash
dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Release
```

**Pass:** All unit tests green; 0 build errors; `build/YardMasterSuite.dll` + `build/YardMasterSuite.Core.dll` present; Release also produces `dist/YardMasterSuite_v*.zip` via `package.ps1`.

Requires local `Directory.Build.targets` (copy from `Directory.Build.targets.example`) for the mod project.

**Unit tests:** Pure helpers live in `YardMasterSuite.Core` (no Unity/game refs) — e.g. `SpeedDisplay` km/h formatting.

---

## Tier 2: In-game smoke (UMM + Monitor HUD)

Requires UMM installed (`Mods\` exists under the game root).

```powershell
dotnet build YardMasterSuite.sln -c Debug
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

### Where to look for evidence

| Source | Path / place | What it proves |
|--------|----------------|----------------|
| **Player.log** | `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log` | Load, toggle, **discrete Tier 2 debug lines** (`T2 …`), exceptions. Agent can read this file. |
| **UMM Logs tab** | Mod Manager → Logs | Same lines as Player.log (subset). |
| **On-screen HUD** | Top-left in game | Visual confirmation; should match the latest `T2` line. |

**Logging policy:** Lifecycle (`enabled` / `disabled`) plus **discrete** Tier 2 lines when something meaningful for sign-off changes (mount/dismount, integrity field change). No per-frame spam of unchanged values. Speed/grade/tonnage are **not** logged every tick.

**Retro rule (all later Monitor stories):** Each in-game story ships matching `T2 <topic> …` Player.log lines for its acceptance checks (same pattern as CMD-01a). Document expected strings in this file when the story lands.

### Lifecycle lines (every session)

- `[YardMasterSuite] Version '<info.json>'. Loading.` (also shown as `v…` chip next to the top HUD bar)
- `[YardMasterSuite] Yard Master Suite enabled (Monitor HUD).`
- `[YardMasterSuite] Active.`
- After Off: `… disabled.` then `Inactive.`
- After On again: `… enabled (Monitor HUD).` then `Active.`
- No `[YardMasterSuite]` exception / `LogException` / stack trace from this mod.

Ignore unrelated game noise (e.g. OpenVR) unless it mentions YardMasterSuite.

### CMD-01b — dual HUD (`T2 consist` / `T2 local-car`)

**Layout:** top bar = loco-anchored train totals; second bar = car under feet only. Look-at is **CMD-01d** (not this smoke).

**Log file:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`  
Filter: `[YardMasterSuite]`

#### Expected `T2 consist` lines (top bar)

| When you… | Expect in Player.log |
|-----------|----------------------|
| First sample, no loco | `T2 consist init (no-loco): — Cars  \|  — Handbrakes` |
| Gain a usable loco train (stand on car fully linked to a loco) | `T2 consist loco: Cars N  \|  Handbrakes M` |
| Lose usable path (step off / break hose-tight-cocks path to loco) | `T2 consist no-loco: — Cars  \|  — Handbrakes` |
| Cars or Handbrakes total changes while loco present | `T2 consist change: Cars N  \|  Handbrakes M` |

#### Expected `T2 local-car` lines (second bar)

| When you… | Expect in Player.log |
|-----------|----------------------|
| First sample on foot (bar hidden) | `T2 local-car init (hidden)` |
| Climb onto a car | `T2 local-car appear: Pipe …  \|  Handbrake N  \|  Couplers F± R±  \|  Car #  \|  Job …` |
| Step off the car | `T2 local-car hide` |
| Pipe / Handbrake / Couplers / Car # / Job change while on car | `T2 local-car change: …` |

#### Sign-off checklist

| # | Check (plain English) | Evidence |
|---|------------------------|----------|
| 1 | Mod loads; Active; no mod errors on boot | Lifecycle lines + no exceptions |
| 2 | No loco — top bar **red border** + `— Speed \| — Grade \| — Mass \| — Cars \| — Handbrakes` | HUD + `T2 consist … no-loco` |
| 3 | On a loco train — top bar live Speed/Grade/Mass/Cars/Handbrakes | HUD + `T2 consist loco:` / `change:` |
| 4 | On foot — **no second bar** | HUD + `T2 local-car hide` or `init (hidden)` |
| 5 | Stand on a car — second bar under top: Pipe / Handbrake 0–1 / Couplers / Car # / Job # | HUD + `T2 local-car appear:` |
| 6 | Couplers `+` only when fully linked (mech + air hose + blue MU wires if present); `-` if any missing | HUD couple/hose/MU smoke + `T2 local-car change:` |
| 7 | Mod Off → HUD gone; On → back; no YardMasterSuite exceptions | Lifecycle `disabled` / `enabled` |

Recovery: [modding.md](doc/requirements/modding.md).

### CMD-01d — look-at second bar (`T2 look-at`) — **priority flip v0.4.6**

**Layout:** same second bar as standing-on-car. **Look-at wins** over standing (inspect yard cars from a car roof). Standing is the fallback when the crosshair is not on a car. Shared target car also drives usable-train top bar. Locos append `Loco DE6`-style type; freight omits that segment.

**Log file:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`  
Filter: `[YardMasterSuite]` · chip `v0.4.7`  
**QOL-06:** look-at uses spherecast (**0.15 m** radius) out to **250 m** (was thin raycast ~80 m). Accept: good enough for yard scouting; slight sky-stickiness OK.

#### Expected `T2 look-at` lines

| When you… | Expect in Player.log |
|-----------|----------------------|
| First sample on foot, not pointing at a car | `T2 look-at init (hidden)` |
| Point at a car (on foot or standing on another) | `T2 look-at appear: Pipe …  \|  Handbrake N  \|  Couplers F± R±  \|  Car #  \|  Job …` (+ `\|  Loco …` if loco) |
| Stop pointing / look away (on foot) | `T2 look-at hide` |
| Look-at car fields change | `T2 look-at change: …` |
| Standing on a car, look away from cars | `T2 look-at hide` then `T2 local-car appear: …` (standing fallback) |

#### Sign-off checklist

| # | Check (plain English) | Evidence |
|---|------------------------|----------|
| 1 | Mod loads at **v0.4.7**; Active; no mod errors | Lifecycle + HUD chip |
| 2 | On foot, not looking at a car — **no second bar** | HUD — **PASS** (prior) |
| 3 | Point at a car — second bar shows that car’s Pipe / Handbrake / Couplers / Car # / Job # | HUD — **PASS** (Job e.g. `SM-SU-46`) |
| 4 | Point at a car **not** on a usable loco train — `Car XX`; top bar red/null | HUD after uncouple — **PASS** |
| 5 | Point at a car **on** a usable loco train — Car # from loco; top bar can show that train’s totals | HUD — **PASS** |
| 6 | Stand on car A, look at car B — second bar shows **B**; look at sky — second bar shows **A** | HUD — **PASS** at v0.4.6 |
| 7 | Point at a loco — second bar includes `Loco DE6` (or type); freight omits Loco segment | HUD — re-smoke |
| 8 | Distant car (only top visible, ~80–250 m) resolves as look-at target | HUD — **PASS*** at v0.4.10 (0.15 m; slight sky-stickiness OK) |
| 9 | Freight second bar shows `Cargo Steel Rails` (etc.); empty = `Empty Cargo`; loco omits Cargo | HUD — cargo load **PASS** at v0.4.11; `Empty Cargo` wording at v0.4.12 (deferred smoke) |

### CMD-01c — coupler tight/loose (`T2 coupler`)

**Layout:** same Couplers segment on the second bar. Marks: `+` usable, plain `*` coupled + chain loose, `-` open/incomplete, yellow `*` MU warning.

**Log file:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`  
Filter: `[YardMasterSuite]` · chip `v0.4.5`

#### Expected `T2 coupler` lines

| When you… | Expect in Player.log |
|-----------|----------------------|
| First sample, no target car | `T2 coupler init (hidden)` |
| Target appears (stand or look-at) | `T2 coupler appear: Couplers F±/* R±/*` (or `init:` on first visible) |
| Lose target | `T2 coupler hide` |
| Tighten / loosen / couple / uncouple changes marks | `T2 coupler change: Couplers …` |

#### Sign-off checklist

| # | Check (plain English) | Evidence |
|---|------------------------|----------|
| 1 | Mod loads; Active; no mod errors | Lifecycle + HUD chip — **PASS** at **v0.4.4**; **v0.4.5** = glyph-only (`~`→plain `*`), re-smoke waived |
| 2 | Uncoupled end shows `-` | HUD — **PASS** |
| 3 | Mechanically coupled, chain **loose** shows distinct mark | HUD — **PASS** (`~` at 0.4.4; plain `*` at 0.4.5) |
| 4 | Fully linked (tight + hose + cocks) shows `+` | HUD — **PASS** |
| 5 | Standing or look-at target drives the marks (look-at wins) | HUD — **PASS** (priority flip v0.4.6) |
| 6 | Mod Off → On; no YardMasterSuite exceptions | Lifecycle — **PASS** |

### Later stories (retro requirements)

| Story | Planned `T2` topic (when implemented) |
|-------|----------------------------------------|
| **CMD-02 / 03** | Same pattern: discrete lines for that monitor’s sign-off fields |

---

**Handoff / merge-ready:** Tier 1 (`dotnet test` + Release build). Phase stories that touch in-world UI also need Tier 2 smoke before marking Done in PM_PLAN.
