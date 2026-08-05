# GeminiDocs

Local scratch for Gemini conversation exports. **Not** the source of truth.

Promote decisions into tracked docs (`PM_PLAN.md`, `doc/requirements/*`, `TEST_PLAN.md`, etc.), then **delete** the scratch files from this folder (keep this README).

**Git:** this folder is gitignored **except this README**.

**Layout:** **flat only** — no subfolders. See [`.cursor/rules/gemini-handoff.mdc`](../../.cursor/rules/gemini-handoff.mdc).

## Comprehensive handoff (“Gemini this” / “G this”)

- **Extremely detailed** narrative `.md` in this folder
- Involved source as flat copies (**≤9** code files; combine excerpts if more)
- `{topic}__{repo/path→__}.cs` naming; **Attached code** section
- **Delete all unrelated packs** before finishing (keep only `README.md` + the new pack)
- **Active packs** = current pack only

## Active packs *(local only)*

| Pack | Topic |
|------|--------|
| [`Multi-step Maps Specification.md`](./Multi-step%20Maps%20Specification.md) | **3.7** — Gemini decisions + acceptance (promoted into `PM_PLAN` 2026-08-04) |
| [`GMaps_MultiStep.md`](./GMaps_MultiStep.md) | Bridge pack + screenshots / code excerpts (input to that spec) |
