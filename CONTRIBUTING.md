# Contributing

Thank you for contributing. This template uses **tracked documentation** as the source of truth — not chat history and not local handoff files alone.

## Start here (reading order)

1. [README.md](README.md) — what this template provides
2. **[doc/PROJECT_STATUS.md](doc/PROJECT_STATUS.md)** — current state, active branch, what's next
3. [PM_PLAN.md](PM_PLAN.md) — phased checklist (customize for your product)
4. [AGENT_HANDOFF.md](AGENT_HANDOFF.md) — run/test commands, merge-ready gate, conventions
5. [TEST_PLAN.md](TEST_PLAN.md) — test tiers
6. [doc/requirements/](doc/requirements/) — product specs (add as needed)

## Local session notes vs GitHub

| Location | On GitHub? | Purpose |
|----------|------------|---------|
| `doc/PROJECT_STATUS.md` | **Yes** | Human-readable current state — **update when milestones ship** |
| `AGENT_HANDOFF.md` → Current state | **Yes** | Maintainer + agent snapshot — keep in sync |
| `PM_PLAN.md` | **Yes** | Phase checklists |
| `.cursor/handoff/*-handoff-*.md` | **No** (gitignored) | Optional local session diary |
| `doc/handoff/*-HANDOFF-*.md` | **No** (gitignored by default) | Same — promote decisions to tracked docs |

**Norm:** If a decision affects what contributors build next, update `doc/PROJECT_STATUS.md` and `AGENT_HANDOFF.md` in the same PR — not only a gitignored handoff note.

## Development setup

```bash
git clone https://github.com/pbuckles22/AgenticTemplate.git
cd AgenticTemplate
# Install your stack toolchain, then open in Cursor
```

Document your **merge-ready command** in [AGENT_HANDOFF.md](AGENT_HANDOFF.md) and [TEST_PLAN.md](TEST_PLAN.md).

## Variant templates

Stack-specific variants (e.g. [FlutterAgenticTemplate](https://github.com/pbuckles22/FlutterAgenticTemplate)) track this repo as **upstream**. See [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Creating variant templates*.

## Pull request expectations

- [ ] Scope matches PM_PLAN / PROJECT_STATUS
- [ ] Merge-ready gate green (your documented command)
- [ ] No secrets or credentials
- [ ] If a phase milestone shipped: update **PM_PLAN** and **doc/PROJECT_STATUS.md**

## Questions

Open a GitHub issue (contribution template) or see [TECH_DEBT.md](TECH_DEBT.md), [RISKS.md](RISKS.md).
