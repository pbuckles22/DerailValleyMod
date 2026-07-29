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

| [`A111_SpeedLimit_OwnershipAndCorridor.md`](./A111_SpeedLimit_OwnershipAndCorridor.md) | **1.10 @ 0.5.42** — passed a `6` board, `Limit` stayed 30 (ownership never transfers); constant 20 m lateral cap replaced with a distance-scaled corridor; new per-board `take`/`skip` trace |

