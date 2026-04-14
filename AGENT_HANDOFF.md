# Agent handoff — Project

## Purpose

This repo is an **agentic template**: Cursor rules, skills, handoff protocol, and testing discipline. **Replace** stack-specific placeholders below with your project’s commands (test runner, coverage, integration or E2E).

## Source of truth

- **Scope / sprints:** [PM_PLAN.md](PM_PLAN.md)
- **Skills:** [.cursor/skills/](.cursor/skills/) — DEV_GUIDE.md, TEST_TDD.md, DESIGN_SYSTEM.md, techwriter, tester, code-reviewer, tech-debt-evaluator, pm-governance, ui-ux, game-readiness, visual-match

## Pod (agents always working)

- **Techwriter:** Use when editing README, AGENT_HANDOFF, or internal docs.
- **Tester:** Black-box tests; run your **documented** test command after changes; keep the suite green. See [TEST_PLAN.md](TEST_PLAN.md).
- **Handoff (mandatory):** When the user wants a handoff, run code review (code-reviewer), tech debt (tech-debt-evaluator), and your **tests or coverage** as documented below; record in the handoff note. See [.cursor/rules/handoff-checklist.mdc](.cursor/rules/handoff-checklist.mdc).

## Current state

- **Template:** Agentic rules and skills in place. Add your codebase and document run/test commands here and in TEST_PLAN.md.

## Run and test

**Document your commands** (examples — replace with yours):

```bash
# e.g. npm test && npm run build
# e.g. cargo test
# e.g. pytest
```

Replace the block above with your real commands and keep them in sync with TEST_PLAN.md.

## Conventions

- Prefer pure functions for business logic where possible.
- **Docs:** Use the **techwriter** skill when editing README, AGENT_HANDOFF, or internal docs.
- **Tests:** Black-box; run your project test command after logic or test changes; keep the suite green (see .cursor/skills/tester/SKILL.md). Prefer writing a failing test before new production code (TDD) where applicable.

---

## Handoff protocol

When ending a session: (1) Run the handoff checklist (code review, tech debt, tests/coverage). (2) Update "Current state" above if needed. (3) Write a handoff note in `.cursor/handoff/` (filename: `handoff-YYYY-MM-DD_HHmm.md`). (4) Include Code review, Tech debt, Code coverage / tests, Done this session, Next up. Use `.cursor/handoff/_template.md` as a starting point.
