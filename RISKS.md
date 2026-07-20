# Risk register (top 5)

Keep only the highest-leverage **current** risks. Short on purpose.

---

### 1 — Merge-ready drift

| | |
|--|--|
| **Risk** | Agents treat work as “green” without running the real gate |
| **Impact** | High |
| **Likelihood** | Med |
| **Trigger** | Merge without documented Tier 1; flaky CI ignored |
| **Mitigation** | Keep `AGENT_HANDOFF` + `TEST_PLAN` aligned; enforce handoff checklist |
| **Rollback** | Revert the merge; restore last known-good commit |

---

### 2 — Context bloat / conflicting truth

| | |
|--|--|
| **Risk** | Agent drift from bloated chat or competing “sources of truth” |
| **Impact** | Med |
| **Likelihood** | High |
| **Trigger** | Handoff notes ≫ 500 words; long logs in chat; docs disagree |
| **Mitigation** | session-summarizer; durable truth in tracked docs; minimal bootstrap |
| **Rollback** | Compressed handoff; re-establish PM_PLAN / PROJECT_STATUS; re-scope |
