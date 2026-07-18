## Technical debt (tracked backlog)

This is the durable home for technical debt across sessions. Handoff notes can mention debt, but anything that persists should be recorded here.

### Cadence

- **Every handoff**: run the tech-debt-evaluator skill and record “Do first” items in the handoff note.
- **Promote persistent debt**: if a “Do first” item persists across 2+ handoffs (or blocks work), add it here and rank it.

---

## Fix now

(Blocking, unsafe, or no-rollback debt.)

- (none)

## Fix soon

(High ROI; frequent pain; not blocking.)

- **Tier 2: loco↔loco MU yellow `F*`/`R*`** — implemented; in-game smoke deferred until a second loco is available.
- **Coupler plain `*` vs yellow `*`** — loose and MU share the glyph; HUD color distinguishes, but plain `T2 coupler` / Format strings cannot (Loose→MuWarning may not emit `change:`). Revisit when MU smoke lands — distinct debug labels or marks.

## Accept for now

(Isolated + workaround + revisit trigger.)

- **Core sources compiled into `YardMasterSuite.dll`** — Unity Mono failed to `LoadFile` sibling `YardMasterSuite.Core.dll`; csproj links Core `*.cs` into the mod assembly. Revisit if UMM/Unity can reliably load a sibling Core DLL.
- **`CurrentIntegrityDebugSnapshot` / `Tier2IntegrityDebug`** — superseded by `T2 consist` / `T2 local-car` / `T2 look-at` / `T2 coupler` in the HUD driver; keep Core helpers until a cleanup pass.
- **Look-at raycast per telemetry refresh** — multiple `TryGetTargetCar` callers may raycast more than once per 100ms tick; cache if profiling shows cost.

---

## ROI rubric (quick)

Score each: Impact (0–2) + Frequency (0–2) + RiskReduction (0–2) + Effort (0–2, reverse scale). Sort descending.
