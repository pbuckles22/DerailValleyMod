# Modding notes — Derail Valley / UMM

Companion to [product.md](product.md). Day-to-day toolchain: [DEV_GUIDE.md](../../.cursor/skills/DEV_GUIDE.md).

**Repo:** https://github.com/pbuckles22/DerailValleyMod

## Stack (runtime)

| Piece | Choice |
|-------|--------|
| Engine | Unity |
| Language | C# → class library `.dll` |
| Target | `net48` |
| Mod loader | Unity Mod Manager (UMM) |
| Injection | Harmony (Prefix / Postfix only) |
| Workflow | Decompile `Assembly-CSharp.dll` → Harmony patches → `dotnet build` → drop `.dll` into Mods folder |

## Development principles

- **Prefix/Postfix only** — Strictly avoid Transpilers for long-term stability.
- **Graceful fail** — If a game update breaks a method signature, log the failure and **disable the mod** rather than crashing.
- **Governor logic** — Active modifiers (thermal / wheelslip / brake release) must pass safety gates (e.g. stationary) before writing to game state.
- **Three-Gate** — All state writes: Integrity → State Registry → Soft Write (see product.md).

## BepInEx → UMM (“Rosetta Stone”)

| Tutorial term | UMM equivalent | Notes |
|---------------|----------------|-------|
| `Plugin.cs` | `Main.cs` | Entry point with `Load()` |
| `Awake()` / `Start()` | `OnToggle()` | Load/unload Harmony patches |
| GUI manager patch | Harmony patching | Manual `[HarmonyPatch]` on game types |

## Launch recovery (3 steps)

If the game crashes on boot or fails to load:

1. **UMM check** — Ctrl+F10; find the mod with the red flag.
2. **Bad apple** — Move `Mods` → `Mods_Backup`, confirm clean launch, restore mods one-by-one.
3. **Player.log** — `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`

## Reference library

| Need | Source | Strategy |
|------|--------|----------|
| Modding bible | [Insprill’s DV Modding Guide](https://github.com/Insprill/dv-modding-guide) | Read first |
| Harmony API | [Harmony documentation](https://harmony.pardeike.net/) | Patch patterns |
| Track / switch math | [WallyCZ/DVRouteManager](https://github.com/WallyCZ/DVRouteManager) | e.g. `RouteTracker.cs` |
| Mod loading | [Unity Mod Manager](https://github.com/newman55/UnityModManager) | Project layout |
| This project | [pbuckles22/DerailValleyMod](https://github.com/pbuckles22/DerailValleyMod) | Source of truth |

## Troubleshooting

- **“No SDKs found”** — Install .NET Framework 4.8 targeting pack / desktop workload; restart terminal.
- **Namespace errors** — Confirm `Assembly-CSharp.dll` reference in `.csproj`.
- **Mod missing in UMM** — Validate `Info.json` (missing commas are the usual failure).

## Source

Developer Roadmap v3.0 (2026-07).
