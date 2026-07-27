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
| [`A411_Backup_Proximity.md`](./A411_Backup_Proximity.md) | **4.11 @ 0.5.12** — unified: green `Rear Nm` honesty + ranging context (**1 md + 4 code**) |
| [`Recent Findings & Feature Concepts.md`](./Recent%20Findings%20%26%20Feature%20Concepts.md) | Triaged **2026-07-27**: §4 backup → **4.11**; §2 radar = shipped **4.10**; §3 Harbor Hill → **2.2** backlog; §1 DM3 → parking lot |
| [`Loco Radar Implementation Context.md`](./Loco%20Radar%20Implementation%20Context.md) | **Superseded** by shipped **4.10** |
| [`A2_Thermal_Governor_TriggerMismatch.md`](./A2_Thermal_Governor_TriggerMismatch.md) | **2.2** Hot trigger mismatch — parked after **4.11** |
| [`Loco_Radar_4_10_Sticky_Pack.md`](./Loco_Radar_4_10_Sticky_Pack.md) | Historical **4.10** sticky pack |
| [`Key Buffer Implementation.md`](./Key%20Buffer%20Implementation.md) | **OFF-TOPIC** — Windows hotkey buffer |
| [`Test Plan.md`](./Test%20Plan.md) | Stale Bundle D letters — prefer tracked `TEST_PLAN.md` |
