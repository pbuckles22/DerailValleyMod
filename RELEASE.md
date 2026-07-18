## Release / merge discipline (lightweight)

Keep releases/merges boring and reversible.

### Versioning (required — do not skip)

SemVer on `info.json` + matching `repository.json`:

| Bump | When |
|------|------|
| **PATCH** (`0.4.x`) | Every deployable fix or feature (so UMM + HUD `v…` chip prove the new DLL) |
| **MINOR** | Story / slice close when you want a clearer milestone label |

Also keep Player.log `Version '…'. Loading.` in sync (reads `info.json`). After deploy: confirm the HUD chip matches before Tier 2 sign-off — a stale chip means the old DLL is still loaded (toggle mod or restart).

Same rule is summarized in [AGENT_HANDOFF.md](AGENT_HANDOFF.md) → *Conventions*.

### Merge-ready (minimum)

Document your real gate in `AGENT_HANDOFF.md` and `TEST_PLAN.md`, then treat it as mandatory:

- Tier 1 is green (fast feedback)
- Tier 2 is run when behavior demands integration/E2E validation
- Version bumped per the table above when the DLL changes
- Tracked docs updated when workflow/expectations change
- Rollback path is clear (a revert commit is usually sufficient)

### Rollback

- Prefer a single revert commit per change
- If a change affects your “stable” line, revert immediately and re-run the required validation tier(s)
