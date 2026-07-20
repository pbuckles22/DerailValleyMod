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

### Later *(open)*

| Story | Planned `T2` |
|-------|----------------|
| **1.8 / 1.9** | Extend `T2 power` (Motors / Fuel / Oil) |
| **1.10** | Speed-limit topic (grade already shipped) |

Recovery: [modding.md](doc/requirements/modding.md).
