# Tuning JSONL schema

Each line is one JSON object. Unknown fields allowed (forward-compatible).

## `knobs.jsonl`

| Field | Type | Meaning |
|-------|------|---------|
| `ts` | string ISO-8601 | When the change was decided / shipped |
| `version` | string | Mod version that first contains this knob set |
| `agg` | number | `SpeedLimitAggressiveness.Value` (0=safe sticky … 1=late) |
| `warningMargin` | number | `BrakeAdvisory.WarningTimeMarginFactor` (1.25 = +25%) |
| `adoptLead` | number | Effective adopt lead factor at `agg` |
| `releaseLead` | number | Effective release lead factor at `agg` |
| `geoScale` | number | Effective `GeometryLimitLeadScale` at `agg` |
| `notes` | string | Why we moved |
| `fromSession` | string? | Session id that justified the change |

## `sessions.jsonl`

| Field | Type | Meaning |
|-------|------|---------|
| `id` | string | Stable id, e.g. `2026-07-31_065` |
| `ts` | string ISO-8601 | Smoke / analysis time |
| `version` | string | UMM Version |
| `agg` | number | Dial in that build |
| `verdict` | `pass` \| `fail` \| `partial` \| `waived` | Tier 2 call |
| `t2ChangeCount` | number | `T2 limit change` lines in Player.log |
| `brakeWithoutLimit30` | number | Frames with `adv=… 30` but Limit ≠ 30 |
| `rec30UnderHighPosted` | number | `Limit 30 (Recommended)` while `posted` ∈ {60,70,80} |
| `overspeedVsRec` | number | Speed > Recommended + 5 |
| `suggestMax` | number? | Max numeric `suggest=` seen (dial tip) |
| `stressThrOk` | bool | False if logs show `thr?` / unusable DerailStressThreshold |
| `highlights` | string[] | Short bullets |
| `playerNotes` | string? | Verbatim feedback |
| `nextAction` | string? | What to ship / try next |

## `raw/*_frames.jsonl` (optional, gitignored)

One object per `T2 limit change` line: `spd`, `limit`, `posted`, `adv`, `src`, `along`, `lead`, `headroom`, `suggest`, `stress`, `build`, `curveNow`, `curveAhead`, `grade`, `agg`, `min`, `geoCount`, …
