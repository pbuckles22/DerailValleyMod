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

1. Launch Derail Valley; UMM Ctrl+F10 — **Yard Master Suite** listed Active.
2. Top-left HUD shows `— km/h  |  — %  |  — t` on foot; enter a loco and confirm speed / grade / tonnage update while moving (grade sign +/−; tonnes for consist).
3. Toggle mod off — HUD disappears; toggle on — HUD returns; no red flag.
4. Player.log: `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log` — no mod errors.

Recovery: [modding.md](doc/requirements/modding.md).

---

**Handoff / merge-ready:** Tier 1 (`dotnet test` + Release build). Phase stories that touch in-world UI also need Tier 2 smoke before marking Done in PM_PLAN.
