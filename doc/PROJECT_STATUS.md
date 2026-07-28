# Project status

Human-readable **snapshot**. Keep in sync with [AGENT_HANDOFF.md](../AGENT_HANDOFF.md) → *Current state* and checkbox truth in [PM_PLAN.md](../PM_PLAN.md).

**Last updated:** 2026-07-27

---

## Summary

**Yard Master Suite** — Fleet Operator utilities for *Derail Valley*  
Stack: Unity / C# `net48` / UMM / Harmony  

| | |
|--|--|
| **Journey** | Stage 2 — Governors |
| **MVP** | Epic 1 System Monitor HUD |
| **Version** | **0.5.15** on `main` *(**2.2** thermal governor)* |
| **WIP branch** | — |

---

## Active work

| Branch | Role |
|--------|------|
| **`main`** | Integration — **0.5.15**; next **4.12** direction-gated proximity |

---

## Progress (official IDs)

### Epics

- [x] **Epic 0** — Foundation & Safe Boot
- [x] **Epic 1** — Diagnostic HUD *(complete 2026-07-23; **1.15** queued)*
- [ ] **Epic 2** — Governor Mode *(**2.1** + **2.2** done; **2.3** next after HUD follow-ups)*
- [ ] **Epic 3** — Yard Master / Dispatcher
- [x] **Epic 4** — HUD quality *(complete 2026-07-27 incl. **4.11**; **4.12** queued)*
- [ ] **Epic 5** — Digital Catalog

### Next

- [ ] **4.12** Direction-gated proximity — hide `Rear` when forward; show `Front` when forward
- [ ] **1.15** Engine / MU alive — lead running + trailing trip awareness
- [ ] **2.3** Auto-brake governor
- Parking: DM3 reverser/MU notes; **3.1b**; session reset

### Gemini triage (2026-07-27)

- **4.11** backup / couple-ready → **shipped** @ **0.5.12**
- Loco radar → already **4.10**
- Harbor Hill / Hot → **2.2** **shipped** @ **0.5.15**
- DM3 → parking lot
