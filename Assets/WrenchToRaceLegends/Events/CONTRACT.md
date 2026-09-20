# WTRL.Events — contract

Ported from `SwiftRacer/Sources/WTRLCore/Runtime/RaceRuntime.swift` and
the `RaceDefinition`/`RaceFormat` types in `Content/Definitions.swift`.
**This is the real, working race-flow implementation** —
`PIVOT-PLAN.md` explicitly pointed here as the correct reference,
distinct from the stale `RaceDisciplineRuntime`/`TrackGeometryRuntime`
lineage that several external uploads this project absorbed tried to
reintroduce and that was rejected every time (still references a
`RaceFormat.drag` case that doesn't exist in the real 5-format enum).
Depends on `WTRL.Core` (empty), `WTRL.Vehicle`, `WTRL.World` per the
asmdef graph — not yet actually used by anything in this assembly (same
situation `WTRL.Lab/CONTRACT.md` already flagged for its own unused
`WTRL.Racing` reference).

## Public API

**Content**: `RaceFormat` (`Circuit, Touge, Knockout, PursuitChase,
PursuitEscape` — exactly 5, no drag), `RaceDefinition`.

**Circuit race flow**: `RacePhase {Staged, Countdown, Running,
Finished}`, `RacePenaltyKind`, `RacePenalty`, `RaceRuntimeState`,
`RaceRules` (`BeginCountdown`, `Start`, `Advance`, `RegisterFalseStart`,
`CompleteSector`, `CompleteLap` — every mutator guards on the current
phase, so e.g. `CompleteLap` on a still-`Staged` race is a real no-op,
not an error).

**Drag race flow** (a separate, simpler state machine — no laps/sectors,
just distance splits): `DragSplit`, `DragRaceRuntimeState`,
`DragRaceRules` (`Stage`, `Green`, `Advance` — captures 60ft/330ft/⅛-
mile/1000ft/¼-mile splits as the car crosses each distance, in order,
never re-triggering an already-captured split).

## Rev16.1 audit recommendation, not yet acted on

`Assignments/OUTPUT-Rev16.1-Audit.md` recommends layering the archived
Rev16.1 project's 8-state `RaceState` shape (`Inactive, Loading, Staging,
Countdown, Racing, Finishing, Results, Complete`) around this 4-phase
core once scene-loading and a results-display flow actually exist in
`WTRL.Runtime` — this assembly's 4 phases (`Staged/Countdown/Running/
Finished`) are the real, already-tested inner state machine; `Loading`
and `Results` are presentation-layer wrapping states this assembly
deliberately doesn't need to know about. Not implemented yet because
`WTRL.Runtime` doesn't exist — premature to add wrapping states with
nothing to wrap.

## Event preflight gating — not yet implemented

The other Rev16.1 audit recommendation — designing an entry gate against
`EventPreflightService.Evaluate`'s real check list (reputation, chapter,
lineage, homologation, fuel, loadout/spares) — is real, valuable, and
NOT done in this pass. It needs `WTRL.Career`'s reputation/chapter state
and `WTRL.Garage`'s vehicle/loadout state as inputs, so it's more
naturally `WTRL.Career`'s concern once that assembly exists (matching
`WTRL.RPG`'s own established pattern of staying dependency-light and
pushing cross-cutting orchestration to `WTRL.Career`). Tracked here so
it isn't lost.

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project (alongside `WTRL.World`, since both were designed
together this pass). **0 errors, 0 warnings** on the first attempt. 7
new tests, 6 ported term-for-term from `WTRLCoreTests.swift`'s
`testRaceRuntimeFinishesAtLapCount` and `WTRLAdvancedTests.swift`'s
`testRaceCountdownStartsRace`/`testRaceFalseStartAddsPenalty`/
`testRaceLapTimeRecorded`/`testDragSplitCapture`/
`testDragQuarterMileFinishes` (against a locally-built `RaceDefinition`,
same reasoning as every other assembly's ported tests — no content
catalog exists), plus 1 new one (`CompleteLap` is a no-op before a race
starts). All pass. **54/54 passing project-wide** across all seven real
assemblies now shipped: `WTRL.Vehicle` (5) + `WTRL.Racing` (8) +
`WTRL.Garage` (9) + `WTRL.Lab` (6) + `WTRL.RPG` (15) + `WTRL.World`/
`WTRL.Events` combined (11 — both live in the same
`Tests/EditMode/WorldAndEventsTests.cs`: 7 for Events, 4 for World).
