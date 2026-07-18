# Agent handoff — DerailValleyMod

## Purpose

**DerailValleyMod** — Derail Valley mod project with Cursor rules, skills, handoff protocol, and testing discipline.

**Sync:** This repo tracks **AgenticTemplate** as an **upstream remote** for shared skills/rules.
To pull shared updates: `git fetch upstream && git merge upstream/main` (resolve stack-specific conflicts manually).

Document stack-specific commands below (test runner, coverage, integration or E2E).

---

## Syncing updates from AgenticTemplate

When [AgenticTemplate](https://github.com/pbuckles22/AgenticTemplate) gets new skills or enhancements:

```bash
cd DerailValleyMod
git fetch upstream
git merge upstream/main
# Resolve conflicts — keep stack-specific overrides in:
#   - DEV_GUIDE.md, TEST_TDD.md, DESIGN_SYSTEM.md
#   - always.mdc (project context)
#   - AGENT_HANDOFF.md (run/test commands)
git push origin main
```

### What stays shared vs stack-specific

| Shared (sync from upstream)                | Stack-specific (keep yours)           |
| ------------------------------------------ | ------------------------------------- |
| Most skills (techwriter, tester, code-reviewer, code-quality-gate, tech-lead, etc.) | `DEV_GUIDE.md` (architecture, tooling) |
| Rules (handoff-checklist, testing.mdc)     | `TEST_TDD.md` (test commands)         |
| Handoff templates                          | `DESIGN_SYSTEM.md` (UI framework)     |
| Operating model skills (green-and-clean, etc.) | `always.mdc` (project context)    |
|                                            | `AGENT_HANDOFF.md` (run/test section) |

---

## Source of truth

- **Scope / sprints:** [PM_PLAN.md](PM_PLAN.md)
- **Skills:** [.cursor/skills/](.cursor/skills/) — DEV_GUIDE.md, TEST_TDD.md, DESIGN_SYSTEM.md, techwriter, tester, code-reviewer, **code-quality-gate**, **tech-lead**, tech-debt-evaluator, eval-engineer, risk-manager, release-manager, security-reviewer, incident-triager, green-and-clean, context-bootstrapper, session-summarizer, pm-governance, ui-ux, game-readiness, visual-match, **github-feature-workflow**

## Green and clean operating model (how we work)

This project assumes a strict operating model aimed at **green and clean** delivery:

- **Green**: each change is verifiable against explicit acceptance criteria and validated at the appropriate tier.
- **Clean**: context is curated; durable state lives in tracked docs; handoffs are compressed and decision-first.

Skills that enforce this:

- [.cursor/skills/green-and-clean/SKILL.md](.cursor/skills/green-and-clean/SKILL.md)
- [.cursor/skills/context-bootstrapper/SKILL.md](.cursor/skills/context-bootstrapper/SKILL.md)
- [.cursor/skills/session-summarizer/SKILL.md](.cursor/skills/session-summarizer/SKILL.md)
- [.cursor/skills/eval-engineer/SKILL.md](.cursor/skills/eval-engineer/SKILL.md)

## Context hierarchy (what belongs where)

Contributors and agents use **tracked docs** for product truth. See [CONTRIBUTING.md](CONTRIBUTING.md).

- **Level 1:** [CONTRIBUTING.md](CONTRIBUTING.md), [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md), `.cursor/rules/always.mdc`, this file
- **Level 2:** [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md)
- **Level 3:** current task plan + acceptance criteria
- **Level 4 (optional, local only):** `.cursor/handoff/NNNN-handoff-*.md` — gitignored; never sole source of truth

Token hygiene: prefer Level 1 + Level 2 + current files over transcript dumps.

## Risk discipline

Keep the top risks explicit and current:

- [RISKS.md](RISKS.md) — top 5 only (impact/likelihood/trigger/mitigation/rollback)

## Release / merge discipline

Keep "ship" criteria explicit and boring:

- [RELEASE.md](RELEASE.md) — merge-ready expectations and rollback posture

## Technical debt discipline

Track debt continuously and evaluate ROI:

- [.cursor/skills/tech-debt-evaluator/SKILL.md](.cursor/skills/tech-debt-evaluator/SKILL.md) — produces "Do first" items during handoff
- [TECH_DEBT.md](TECH_DEBT.md) — durable ranked backlog (promote persistent "Do first" items here)

## Incident / debugging discipline

When something breaks, use evidence-driven triage and keep it bounded:

- [.cursor/skills/incident-triager/SKILL.md](.cursor/skills/incident-triager/SKILL.md)
- [INCIDENTS.md](INCIDENTS.md) — what to capture (minimum) for handoff and prevention

## Pod (agents always working)

- **Techwriter:** Use when editing README, AGENT_HANDOFF, or internal docs.
- **Tester:** Black-box tests; run your **documented** test command after changes; keep the suite green. See [TEST_PLAN.md](TEST_PLAN.md).
- **Handoff (mandatory):** When the user wants a handoff, run code review (code-reviewer), tech debt (tech-debt-evaluator), and your **tests or coverage** as documented below; record in the handoff note. See [.cursor/rules/handoff-checklist.mdc](.cursor/rules/handoff-checklist.mdc).

## Contributor onboarding (norm)

1. [CONTRIBUTING.md](CONTRIBUTING.md)
2. [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md)
3. [PM_PLAN.md](PM_PLAN.md)

When shipping: update **PM_PLAN**, **doc/PROJECT_STATUS.md**, and **Current state** below in the same PR.

## Current state

- **Project:** DerailValleyMod — *Yard Master Suite* (UMM / Harmony / net48).
- **Plan:** v3.0 + CMD backlog — Phase 0 done; MVP = Diagnostic HUD (CMD-01…03).
- **Shipped on `main`:** Phase 0; E1-S1 speed; E1-S2 grade/tonnage; **CMD-01a**; **CMD-01b** dual HUD; **CMD-01d** look-at second bar (`1208de1`, v0.4.3) — shared `TryGetTargetCar` (standing wins); `T2 look-at`; HUD `v…` chip; Core inlined into mod DLL. MU yellow 2-loco smoke deferred.
- **Build / deploy:** `dotnet test YardMasterSuite.sln`; `dotnet build YardMasterSuite.sln -c Release`; `package.ps1 -NoArchive -OutputDirectory "...\Mods"`.
- **Next:** **CMD-01c** tight/loose display → CMD-02 / finish CMD-03.

## Run and test

**Game (this machine):** `C:\Program Files (x86)\Steam\steamapps\common\Derail Valley`  
**Mods drop:** `...\Mods\YardMasterSuite\` (`info.json` + dlls from `build\`)  
**Player.log:** `%USERPROFILE%\AppData\LocalLow\Altfuture\Derail Valley\Player.log`

```bash
# One-time after clone: copy Directory.Build.targets.example → Directory.Build.targets

dotnet test YardMasterSuite.sln
dotnet build YardMasterSuite.sln -c Debug
dotnet build YardMasterSuite.sln -c Release   # merge-ready + package.ps1 → dist/

# Deploy (UMM must already create Mods\):
powershell -ExecutionPolicy Bypass -File package.ps1 -NoArchive -OutputDirectory "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley\Mods"
```

Keep in sync with [TEST_PLAN.md](TEST_PLAN.md).

## Conventions

- Prefer pure functions for business logic where possible.
- **Version:** Bump `info.json` (+ `repository.json`) patch (`0.4.x`) on every deployable fix/feature so UMM and the HUD `v…` chip prove the new DLL loaded. Minor bump for story closes; keep in sync with Player.log `Version '…'. Loading.`
- **Docs:** Use the **techwriter** skill when editing README, AGENT_HANDOFF, or internal docs.
- **Tests:** Black-box; run your project test command after logic or test changes; keep the suite green (see .cursor/skills/tester/SKILL.md). Prefer writing a failing test before new production code (TDD) where applicable.

---

## Git workflow (how work lands on `main`)

Document **your** team rules here and keep them in sync with what you run locally.

1. **Integration branch:** Usually **`main`**. All shipped product state (PM_PLAN, roadmap checkboxes) should reflect what is merged here.
2. **Optional short-lived branches:** For larger slices, use `feature/<topic>` or `fix/<topic>`, then merge or rebase into `main`. Agents should follow [.cursor/skills/github-feature-workflow/SKILL.md](.cursor/skills/github-feature-workflow/SKILL.md) when branching or pushing.
3. **Before push / merge-ready:** Run your **full gate** (document it in the **Run and test** section above — e.g. tests + build + integration/E2E). Same checks should run in CI if you use GitHub Actions (or equivalent).
4. **After push — verify CI:** Agents do not get GitHub failure emails. When Actions exist, run `gh run watch --repo OWNER/REPO` (or `gh run list` + `gh run view --log-failed`) before declaring work done on `main`. See [.cursor/skills/github-feature-workflow/SKILL.md](.cursor/skills/github-feature-workflow/SKILL.md).
5. **Pull requests:** **Optional** for DerailValleyMod — set `Required` or `Optional` for your org. If optional, direct push to `main` after green CI is still valid; if required, open a PR and use the same test plan text you ran locally.
6. **After merge:** Delete the local feature branch; delete the remote feature branch if your flow created one.

---

## Handoff protocol

When ending a session:

1. Run the handoff checklist ([handoff-checklist.mdc](.cursor/rules/handoff-checklist.mdc)).
2. Update **PM_PLAN.md** when shipped scope changed.
3. Update **[doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md)** and **Current state** above (required for contributor-visible changes).
4. Optional local note: `.cursor/handoff/NNNN-handoff-*.md` ([template](.cursor/handoff/_template.md)) — gitignored; promote decisions to tracked docs.


## Epic close (automatic)

When an epic's in-scope work is done, **do not wait for the user to ask**. Run [.cursor/rules/epic-close.mdc](.cursor/rules/epic-close.mdc) / pm-governance *Epic close*: **handoff checklist first**, then mark the epic complete in plan/status docs, close note, commit/push, summarize. See [.cursor/skills/pm-governance/SKILL.md](.cursor/skills/pm-governance/SKILL.md).
Anything the team must see on GitHub belongs in **PROJECT_STATUS**, **PM_PLAN**, **README**, or the **PR** — not only gitignored handoff files.
