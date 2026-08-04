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
| [`G31b_Loco_Spawn_Ghost.md`](./G31b_Loco_Spawn_Ghost.md) | **3.1b** — ghost invisible diagnosis |
| [`G31b Loco Spawn Ghost Fix Guide.md`](./G31b%20Loco%20Spawn%20Ghost%20Fix%20Guide.md) | Gemini fix: use native `CarDestinationHighlighter` (shipping @ **0.6.11**) |

**Backlog only (not Active):** `player_headlamp_script.cs` → parked in `PM_PLAN` Parking lot (*Player headlamp*).
