---
name: github-feature-workflow
description: >-
  Short-lived feature branches, CI before commit, push, optional PR, and branch
  cleanup. Use when implementing a feature or non-trivial fix, when the user
  asks for a branch/PR workflow, or when substantial edits should be committed
  and pushed—not left only as local uncommitted work.
---

# Git / GitHub feature branch workflow

**Policy in this template:** [AGENT_HANDOFF.md](../../AGENT_HANDOFF.md) — document whether **`main`** is the integration branch, whether **PRs are optional or required**, and the **full CI / merge-ready command**. This skill adds a **disciplined ritual**: branch → green gate → commit → push → cleanup.

## When to apply

- User asks for a **feature branch**, **PR**, **commit**, or **push**.
- A **coherent slice** of work is done and should be **recorded in git** before the session ends.
- **Do not** create branches for one-line typo fixes unless the user wants it.

## Branch naming

- Prefer: `feature/<short-kebab-topic>` or `fix/<issue-or-topic>`.
- Optional: epic/ticket id for **PM_PLAN** / roadmap traceability.

## Standard sequence

1. **Start from agreed base:** `git fetch origin` when remote exists; `git status`.
2. **Create branch:** `git checkout -b <name>`.
3. **Implement** with tests per [tester](../tester/SKILL.md) and [TEST_TDD.md](../TEST_TDD.md) when applicable.
4. **Gate before commit:** run the **documented** command in AGENT_HANDOFF (placeholder: tests + build + E2E — **replace** with your project’s real gate, e.g. `npm run ci`).
5. **Commit:** clear imperative subject; body only when it helps.
6. **Push:** `git push -u origin <branch>` (first time); later `git push`.
7. **Optional PR:** use team policy; template assumes PRs may be **optional** — see AGENT_HANDOFF **Git workflow** section.
8. **After merge:** checkout integration branch, `git pull`, **`git branch -d <branch>`**; delete remote branch if used only for PR.
9. **Shipped scope:** update [PM_PLAN.md](../../PM_PLAN.md) and your **product plan** under `doc/plan/` (if used).

## What this skill does not do

- Replace **handoff** — see [AGENT_HANDOFF.md](../../AGENT_HANDOFF.md) and [.cursor/rules/handoff-checklist.mdc](../../rules/handoff-checklist.mdc).
- Override **your** PR requirement: document reality in AGENT_HANDOFF.
