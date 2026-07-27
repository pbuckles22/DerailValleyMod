# Release / merge discipline

Keep releases boring and reversible.

---

## Versioning *(required)*

**Scope:** SemVer on **this mod** (`info.json` + matching `repository.json`) — not a mono-repo “project” version. Other mods (if any) version independently.

Form: `MAJOR.MINOR.PATCH` (UMM shows the full string).

| Bump | Digit | When |
|------|-------|------|
| **PATCH** | `0.5.**x**` | **Every deployable ship** — features **and** fixes. This is the Mod Manager “prove this DLL” stamp. New features are patches **within** the current minor line. |
| **MINOR** | `0.**N**.0` | **Milestone line change** — agent (or user) judges the cut is big enough to talk about as a new era. Reset patch to `0`. |
| **MAJOR** | `**N**.0.0` | Rare — public compatibility promise / breaking player contract (typical first: **1.0.0** when Stage 1+2 feel shippable as a product). |

### What “0.5” means

The **minor** digit is a **product era**, not an Epic ID and not “number of features.”

Examples of a good **MINOR** bump (agent judgment — pick one clear reason and record it in the commit/docs):

- Journey stage / pillar change (e.g. Monitor-only → first Governor soft-writes)
- Epic close that ends a long line of work and opens a different kind of work
- Player-visible capability class change (read-only HUD → active control)

Do **not** bump MINOR for every story. Stories stay on **PATCH** (`0.5.1`, `0.5.2`, …).

### Current line

- **0.4.x** — Stage 1 Monitor / Epic 1 + Epic 4 HUD era (closed)
- **0.5.x** — Epic 2 Governor era (starts **0.5.0**, 2026-07-26)

Agents choose the next MINOR when a later milestone feels as sharp as that cut; until then, patch only.

Player.log `Version '…'. Loading.` reads `info.json`. After deploy: confirm **Mod Manager** Version matches before Tier 2 — a stale Mods folder means the old DLL is still loaded (toggle mod or restart). There is **no** in-HUD `v…` chip.

**Agents:** Release build → `dist/*.zip` only. Before asking for smoke, run Mods deploy and verify version ([.cursor/rules/deploy-before-smoke.mdc](.cursor/rules/deploy-before-smoke.mdc)).

Also summarized in [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Conventions*.

---

## Merge-ready (minimum)

Documented gate in `AGENT_HANDOFF.md` + `TEST_PLAN.md` — treat as mandatory:

- [ ] Tier 1 green (`dotnet test` + Release build)
- [ ] **Mods deploy** (`package.ps1 -NoArchive` into game Mods) + `info.json` version verified — before requesting Tier 2 smoke
- [ ] Tier 2 run when the story needs in-game sign-off
- [ ] Version bumped when the DLL changes (**PATCH** unless this ship is a declared **MINOR** milestone)
- [ ] Tracked docs updated (`PM_PLAN` checkbox + PROJECT_STATUS + Current state)
- [ ] Rollback path clear (usually one revert commit)

---

## Rollback

- Prefer a single revert commit per change
- If it hits a “stable” line: revert immediately and re-run the required tier(s)
