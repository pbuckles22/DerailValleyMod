# DEV_GUIDE — DerailValleyMod

## Tech stack

| Layer | Choice |
|-------|--------|
| Game | Derail Valley (Unity) |
| Language | C# |
| Target framework | `net48` (class library) |
| Mod loader | Unity Mod Manager (UMM) |
| Patching | Harmony — Prefix / Postfix only |
| Project shape | [derail-valley-modding/template-umm](https://github.com/derail-valley-modding/template-umm) |
| IDE | Cursor + C# Dev Kit |
| Inspection | [dnSpy](https://github.com/dnSpy/dnSpy/releases) |

Product: [doc/requirements/product.md](../../doc/requirements/product.md)  
Modding notes: [doc/requirements/modding.md](../../doc/requirements/modding.md)  
Plan: [PM_PLAN.md](../../PM_PLAN.md)

## Environment setup

1. **.NET SDK** 8+ with **.NET Framework 4.8 targeting pack** (VS “.NET desktop development” workload).
2. **Unity Mod Manager** installed into Derail Valley (creates `Mods\`).
3. **Cursor** + **C# Dev Kit**; **dnSpy** for inspecting `Assembly-CSharp.dll`.

After clone, copy `Directory.Build.targets.example` → `Directory.Build.targets` and set your game `Managed\` path (file is gitignored).

## Layout (template-umm)

```
YardMasterSuite.sln
YardMasterSuite/           # classlib (Main.cs + .csproj)
info.json                  # UMM metadata (repo root)
package.ps1                # zip / copy for Mods
repository.json            # UMM update check stub
Directory.Build.targets    # local only — game ReferencePath
build/                     # post-build dll copy
```

## Build / deploy

```bash
dotnet build YardMasterSuite.sln -c Debug
dotnet build YardMasterSuite.sln -c Release   # also runs package.ps1 → dist/
```

Deploy to game (after UMM install):

```powershell
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

That copies `info.json` + `build/*` into `Mods\YardMasterSuite\`. Toggle in UMM with Ctrl+F10.

**This machine:** game root `C:\Program Files (x86)\Steam\steamapps\common\Derail Valley`  
**Player.log:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`

## Conventions

- Fail closed on Harmony load failure (`Main.Load` returns false).
- Prefix/Postfix only — no Transpilers without an explicit decision.
- All state writes: Three-Gate + governor safety gates (product.md).
