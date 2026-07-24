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
| [`Key Buffer Implementation.md`](./Key%20Buffer%20Implementation.md) | **OFF-TOPIC for Yard Master / Bundle D** — Windows hotkey swallow/inject for \` double-tap screenshot (`GlobalHotkeyManager`, `SendInput`, WPF `Dispatcher`). Not Preview edge. |
| [`Test Plan.md`](./Test%20Plan.md) | Bundle D smoke letters (may be stale vs v0.4.52 inventory gate) — prefer tracked [`TEST_PLAN.md`](../../TEST_PLAN.md) §4.8. |

**Still aligned with Bundle D (our ship):** Preview = inventory job paperwork; meters = Regular destroy − player − **30 m** HUD buffer; game wipe distance **unchanged**; no gen-zone gate. That came from earlier Job Cancellation / safety-buffer guidance — **not** from `Key Buffer Implementation.md`.
