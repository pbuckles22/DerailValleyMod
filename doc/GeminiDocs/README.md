# GeminiDocs

Local scratch for Gemini conversation exports. **Not** the source of truth.

Promote decisions into tracked docs (`PM_PLAN.md`, `doc/requirements/*`, `TEST_PLAN.md`, etc.), then **delete** the scratch files from this folder (keep this README).

**Git:** this folder is gitignored **except this README**.

**Layout:** **flat only** — no subfolders. See [`.cursor/rules/gemini-handoff.mdc`](../../.cursor/rules/gemini-handoff.mdc).

## Comprehensive handoff

- Narrative `.md` in this folder
- Involved source as flat copies (≤10): `{topic}__{repo/path→__}.cs`
- **Attached code** section + update **Active packs** below

## Active packs *(local only)*

| Pack | Topic |
|------|--------|
| [`A4_House_Proximity_Hide_Lobby_Box.md`](./A4_House_Proximity_Hide_Lobby_Box.md) | A.4: house gone at SM door (~14 m to anchor). Gemini Lobby Box: tighten MaxBuilding 25 / fallback 7–8 / shrink −1.5..−2. (+ 6 code snapshots + Gemini raw). |

*(Earlier UX packs promoted 2026-07-23 into [`UX_SMOKE_FEEDBACK_2026-07-23.md`](../requirements/UX_SMOKE_FEEDBACK_2026-07-23.md) + `PM_PLAN` **4.8**.)*
