# DerailValleyMod

Derail Valley mod project. Cursor **rules**, **skills**, **handoff protocol**, and **test discipline** are included; document stack-specific commands in **AGENT_HANDOFF.md** and **TEST_PLAN.md**.

## New here?

**Start with [CONTRIBUTING.md](CONTRIBUTING.md)** — reading order, tracked vs gitignored docs, PR expectations.

**Current work:** [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md)

## Quick start

```bash
git clone https://github.com/pbuckles22/DerailValleyMod.git
cd DerailValleyMod
copy Directory.Build.targets.example Directory.Build.targets
# Edit Directory.Build.targets if your Steam library path differs, then:
dotnet build YardMasterSuite.sln -c Release
```

Stack and layout follow [derail-valley-modding/template-umm](https://github.com/derail-valley-modding/template-umm) (UMM / Harmony / `net48`). Deploy: [AGENT_HANDOFF.md](AGENT_HANDOFF.md), [TEST_PLAN.md](TEST_PLAN.md).

**In-game:** Install Unity Mod Manager into Derail Valley, then:

```powershell
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

## What’s included

| Area | Contents |
|------|----------|
| **.cursor/rules** | `always.mdc`, `handoff-checklist.mdc`, `testing.mdc` |
| **.cursor/skills** | DEV_GUIDE, TEST_TDD, DESIGN_SYSTEM, techwriter, tester, code-reviewer, tech-debt-evaluator, pm-governance, ui-ux, game-readiness, visual-match, github-feature-workflow |
| **.cursor/handoff** | Handoff note template and README |
| **doc/** | Requirements and optional **`doc/handoff/`** for tracked contributor notes (see `.gitignore` for gitignored session files) |
| **examples/** | Reference UI/specs for **visual-match** / **ui-ux** |
| **YardMasterSuite/** | UMM mod project (template-umm layout) |
| **package.ps1** / **info.json** | Standard UMM package metadata |

Phase 0 on `feature/e0-safe-boot` — see [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md).

## What not to put in the repo

- **No secrets** — API keys, tokens, credentials. Use environment variables or a local config that is gitignored.
- **Session handoff notes** — By default `.cursor/handoff/*-handoff-*.md` (and legacy `.cursor/handoff/handoff-*.md`) plus `doc/handoff/*-HANDOFF-*.md` (and legacy `doc/handoff/HANDOFF-*.md`) are gitignored (see `.gitignore`). Commit `_template.md`, `.cursor/handoff/README.md`, and any tracked docs you choose under `doc/handoff/`.

## Source of truth

[CONTRIBUTING.md](CONTRIBUTING.md), [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md), [AGENT_HANDOFF.md](AGENT_HANDOFF.md), [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md), and `.cursor/skills/`.
