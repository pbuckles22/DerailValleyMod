# DEV_GUIDE — DerailValleyMod

## Tech stack

| Layer | Choice |
|-------|--------|
| Game | Derail Valley (Unity) |
| Language | C# |
| Target framework | `net48` (class library) |
| Mod loader | Unity Mod Manager (UMM) |
| Patching | Harmony — Prefix / Postfix only |
| IDE | Cursor + C# Dev Kit |
| Inspection | [dnSpy](https://github.com/dnSpy/dnSpy/releases) |

Product: [doc/requirements/product.md](../../doc/requirements/product.md)  
Modding notes: [doc/requirements/modding.md](../../doc/requirements/modding.md)  
Plan: [PM_PLAN.md](../../PM_PLAN.md)

## Environment setup (Phase 0)

Install order:

1. **.NET SDK** 8+ — [download](https://dotnet.microsoft.com/download)
2. **Visual Studio Installer** — “.NET desktop development” (for .NET Framework 4.8 targeting)
3. **Cursor** + **C# Dev Kit**
4. **dnSpy**

Scaffold:

```bash
dotnet new classlib -f net48
```

## Architecture (intended)

```
(repo root)
  src/                 # C# class library (UMM mod) — not created yet
  doc/requirements/    # product + modding truth
```

Never commit game `Managed/` DLLs or `DerailValley_Data/` (see `.gitignore`).

## Build / deploy workflow

1. Browse `Assembly-CSharp.dll` in dnSpy (local game install).
2. Write Harmony patches (Prefix / Postfix).
3. `dotnet build`
4. Copy output `.dll` + `Info.json` into the game’s `Mods/<ModName>/` folder.
5. Launch; toggle mod in UMM (Ctrl+F10).

Document the exact merge-ready command in `AGENT_HANDOFF.md` / `TEST_PLAN.md` once the classlib exists.

## Conventions

- Fail closed: log + self-disable on missing Harmony targets.
- All state writes: Three-Gate + governor safety gates (product.md).
- Prefer pure helpers for non-Unity logic where possible.
- See `AGENT_HANDOFF.md` for run/test commands.
