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

- `[YardMasterSuite] Version '0.4.0'. Loading.`
- `[YardMasterSuite] Yard Master Suite enabled (Monitor HUD).`
- `[YardMasterSuite] Active.`
- After Off: `… disabled.` then `Inactive.`
- After On again: `… enabled (Monitor HUD).` then `Active.`
- No `[YardMasterSuite]` exception / `LogException` / stack trace from this mod.

Ignore unrelated game noise (e.g. OpenVR) unless it mentions YardMasterSuite.

### CMD-01a — `T2 integrity` lines (current)

Prefix: `T2 integrity`

| When you… | Expect in Player.log |
|-----------|----------------------|
| HUD first samples (usually on foot) | `T2 integrity init (on-foot): — Pipe  \|  — Handbrake  \|  — Couplers` |
| Climb onto a car | `T2 integrity on-car: Pipe … bar  \|  Handbrake N  \|  Couplers F± R±` |
| Step off the car | `T2 integrity on-foot: — Pipe  \|  — Handbrake  \|  — Couplers` |
| Pipe / Handbrake / Couplers change while on car | `T2 integrity change: …` (new fragment only when those fields change) |

---

**CMD-01a closed:** Dual-HUD / train-wide / look-at checks are **deferred to CMD-01b / CMD-01d** (will retest when those ship). Remaining checklist is for the **current single strip** only.

**CMD-01a sign-off checklist (stand on the car; pointing is CMD-01d later):**

| # | Check (plain English) | Evidence |
|---|------------------------|----------|
| 1 | Mod loads; listed Active; no mod errors on boot | **Player.log** / UMM Logs |
| 2 | Stand on the ground — dashes for Pipe / Handbrake / Couplers | **Player.log** `T2 integrity init` / `on-foot` + HUD |
| 3 | Climb onto a car — live Pipe, Handbrake (1/0 for **this car**), Couplers (`F± R±`) for **car under feet** | **Player.log** `T2 integrity on-car: …` + HUD |
| 4 | Uncouple/couple → `F`/`R` marks flip; this car's handbrake → Handbrake 1/0; charge brakes → Pipe moves | **Player.log** `T2 integrity change: …` + HUD |
| 5 | Mod Off → HUD gone; On → HUD back; no red errors | **Player.log** `disabled` / `enabled` + HUD |
| 6 | No YardMasterSuite exceptions for the session | **Player.log** |

Recovery: [modding.md](doc/requirements/modding.md).

### Later stories (retro requirements)

| Story | Planned `T2` topic (when implemented) |
|-------|----------------------------------------|
| **CMD-01b** consist summary | `T2 consist …` — Cars / Handbrake on·off / Hose; log on mount and when those fields change |
| **CMD-01d** look-at | `T2 look-at …` — second HUD bar appear/disappear; Car # (`XX` if not on train); Job #; integrity fragment for looked-at car |
| **CMD-01c** tight/loose | `T2 coupler …` — tight vs loose when it changes |
| **CMD-02 / 03** | Same pattern: discrete lines for that monitor’s sign-off fields |

**CMD-01d (product):** Point at a car → a **second HUD bar** appears directly under the main top-left strip (same height as the main bar; width fits its text). It shows that car’s Pipe / Handbrake / Couplers, Car # (front→back from engine, or `XX` if not on the train), and Job #. Stop pointing → that second bar disappears (main HUD stays).

---

**Handoff / merge-ready:** Tier 1 (`dotnet test` + Release build). Phase stories that touch in-world UI also need Tier 2 smoke before marking Done in PM_PLAN.
