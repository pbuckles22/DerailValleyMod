# Speed-limit / Brake tuning store (**1.16**)

Append-only **JSONL** (one JSON object per line). Use this for long-haul trend analysis across smokes — not Player.log as the archive.

## Why JSONL (not SQLite yet)

| | JSONL here | SQLite |
|--|--|--|
| Agent / git | Diffable, greppable, no binary | Needs tooling / harder to review in PRs |
| Append after smoke | One line | `INSERT` |
| Analysis | PowerShell / Python / Excel | SQL |
| Scale | Fine for hundreds–thousands of sessions | Better if we outgrow ~10k+ frame rows |

**Rule:** keep **knobs** + **session summaries** in git. Put bulky per-frame extracts under `raw/` (gitignored). Graduate to SQLite only if queries become painful.

## Files

| File | Purpose |
|------|---------|
| [`knobs.jsonl`](knobs.jsonl) | Every dial / constant change we ship (what moved, why) |
| [`sessions.jsonl`](sessions.jsonl) | One row per smoke / log pass (outcomes + key metrics) |
| [`schema.md`](schema.md) | Field definitions |
| `raw/*.jsonl` | Optional full `T2 limit` frame dumps (local only) |

## After each smoke

1. Confirm UMM Version.
2. Agent (or you) appends one `sessions.jsonl` line from Player.log.
3. If knobs change in the next ship, append one `knobs.jsonl` line.
4. Optional: `powershell -File doc/tuning/extract-t2-frames.ps1` → `raw/vX.Y.Z_frames.jsonl`.

## Open product tension (tracked in sessions)

**Brake without Limit 30** — fixed in **0.5.66** (`BrakeLimitAlign`); session `2026-07-31_067` shows **0** such frames.

**False / sticky Recommended 30 (and 40)** — open. Corpus through Harbor leg **`2026-07-31_067h`**: 789 frames; Limit 30 Rec **483**; Limit 40 Rec **165**; real `take '3'=30` only **10**. Player: Brake/Rec to 30 (or 40) without ever seeing that board posted. Also **34** frames where `adv=` target ≠ Limit number (e.g. Limit 30 Rec + Advisory 40) — all in `adv=` / `Limit` / `min=` logs. Retune after downhill capture; Align/AR issues parked in `TECH_DEBT.md`.
