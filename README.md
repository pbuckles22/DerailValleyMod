# Agentic Template (stack-agnostic)

Cursor **rules**, **skills**, **handoff protocol**, and **test discipline** for any project — **not** tied to Flutter, Node, or a specific stack. Use this for browser extensions (Edge, Chrome), backends, CLIs, or other codebases where you do not want a mobile scaffold.

**Related:** For a **Flutter iOS-only** app template with the same agentic layer plus `lib/`, `ios/`, and Dart tests, use [FlutterAgenticTemplate](https://github.com/pbuckles22/FlutterAgenticTemplate).

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
| **.cursor/skills** | DEV_GUIDE, TEST_TDD, DESIGN_SYSTEM, techwriter, tester, code-reviewer, tech-debt-evaluator, pm-governance, ui-ux, game-readiness, visual-match |
| **.cursor/handoff** | Handoff note template and README |
| **doc/** | Placeholder for requirements |
| **examples/** | Placeholder for reference UI/specs |
| **script/** | README — add your own test runner scripts |

No application runtime is included — bring your own stack.

## What not to put in the repo

- **No secrets** — API keys, tokens, credentials. Use environment variables or a local config that is gitignored.
- **Handoff notes** — `.cursor/handoff/handoff-*.md` are gitignored so local handoff notes are not pushed. The template `_template.md` and `README.md` are committed.

## Source of truth

[AGENT_HANDOFF.md](AGENT_HANDOFF.md), [PM_PLAN.md](PM_PLAN.md), [TEST_PLAN.md](TEST_PLAN.md), and `.cursor/skills/`.
