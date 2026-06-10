# Agentic Template (stack-agnostic)

Cursor **rules**, **skills**, **handoff protocol**, and **test discipline** for any codebase — browser extensions (Edge, Chrome), backends, CLIs, native apps, etc. Bring your own runtime and document commands in **AGENT_HANDOFF.md** and **TEST_PLAN.md**.

## New here?

**Start with [CONTRIBUTING.md](CONTRIBUTING.md)** — reading order, tracked vs gitignored docs, PR expectations.

**Current work:** [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md)

## Quick start

```bash
git clone https://github.com/pbuckles22/AgenticTemplate.git
cd AgenticTemplate
# Install your toolchain (npm install, cargo build, etc.), then open this folder in Cursor.
```

Document your **test** and **coverage** commands in [AGENT_HANDOFF.md](AGENT_HANDOFF.md) and [TEST_PLAN.md](TEST_PLAN.md), then follow the tester skill and handoff checklist.

## What’s included

| Area | Contents |
|------|----------|
| **.cursor/rules** | `always.mdc`, `handoff-checklist.mdc`, `testing.mdc` |
| **.cursor/skills** | DEV_GUIDE, TEST_TDD, DESIGN_SYSTEM, techwriter, tester, code-reviewer, tech-debt-evaluator, pm-governance, ui-ux, game-readiness, visual-match, github-feature-workflow |
| **.cursor/handoff** | Handoff note template and README |
| **doc/** | Requirements and optional **`doc/handoff/`** for tracked contributor notes (see `.gitignore` for gitignored session files) |
| **examples/** | Reference UI/specs for **visual-match** / **ui-ux** |
| **script/** | Optional — test or CI helper scripts; document in AGENT_HANDOFF |

No application runtime is included — bring your own stack.

## What not to put in the repo

- **No secrets** — API keys, tokens, credentials. Use environment variables or a local config that is gitignored.
- **Session handoff notes** — By default `.cursor/handoff/*-handoff-*.md` (and legacy `.cursor/handoff/handoff-*.md`) plus `doc/handoff/*-HANDOFF-*.md` (and legacy `doc/handoff/HANDOFF-*.md`) are gitignored (see `.gitignore`). Commit `_template.md`, `.cursor/handoff/README.md`, and any tracked docs you choose under `doc/handoff/`.

## Source of truth

[CONTRIBUTING.md](CONTRIBUTING.md), [doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md), [AGENT_HANDOFF.md](AGENT_HANDOFF.md), [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md), and `.cursor/skills/`.
