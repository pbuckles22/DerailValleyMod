# Release / merge discipline

Keep releases boring and reversible.

---

## Versioning *(required)*

SemVer on `info.json` + matching `repository.json`:

| Bump | When |
|------|------|
| **PATCH** `0.4.x` | Every deployable fix or feature (UMM + HUD `v…` chip prove the new DLL) |
| **MINOR** | Story / slice close when you want a clearer milestone label |

Player.log `Version '…'. Loading.` reads `info.json`. After deploy: confirm HUD chip matches before Tier 2 — a stale chip means the old DLL is still loaded (toggle mod or restart).

Also summarized in [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Conventions*.

---

## Merge-ready (minimum)

Documented gate in `AGENT_HANDOFF.md` + `TEST_PLAN.md` — treat as mandatory:

- [ ] Tier 1 green (`dotnet test` + Release build)
- [ ] Tier 2 run when the story needs in-game sign-off
- [ ] Version bumped when the DLL changes
- [ ] Tracked docs updated (`PM_PLAN` checkbox + PROJECT_STATUS + Current state)
- [ ] Rollback path clear (usually one revert commit)

---

## Rollback

- Prefer a single revert commit per change
- If it hits a “stable” line: revert immediately and re-run the required tier(s)
