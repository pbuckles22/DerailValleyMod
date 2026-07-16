# Test plan (TEST_PLAN.md)

Two-tier strategy for *Yard Master Suite*. Keep in sync with [AGENT_HANDOFF.md](AGENT_HANDOFF.md).

---

## Tier 1: Fast feedback (build)

```bash
dotnet build YardMasterSuite.sln -c Release
```

**Pass:** 0 errors; `build/YardMasterSuite.dll` present; Release also produces `dist/YardMasterSuite_v*.zip` via `package.ps1`.

Requires local `Directory.Build.targets` (copy from `Directory.Build.targets.example`).

---

## Tier 2: In-game smoke (UMM load)

Requires UMM installed (`Mods\` exists under the game root).

```powershell
dotnet build YardMasterSuite.sln -c Debug
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

1. Launch Derail Valley; UMM Ctrl+F10 — **Yard Master Suite** listed.
2. Toggle off/on; no red flag.
3. Player.log: `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log` — no mod errors.

Recovery: [modding.md](doc/requirements/modding.md).

---

**Handoff:** Phase 0 scaffolding merge-ready = Tier 1. Phase 0 **gate** = Tier 1 + Tier 2.
