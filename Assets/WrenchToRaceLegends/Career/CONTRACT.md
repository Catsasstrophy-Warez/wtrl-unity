# WTRL.Career — contract

`CareerTransaction`/`CareerCommand` ported from `SwiftRacer/Sources/
WTRLCore/Persistence/CareerTransaction.swift`. `CareerState` is new
design (no direct Swift equivalent — `SwiftRacer` has `CareerSave`, a
persistence DTO, and `RacingGame/GameState.swift`, a SwiftUI
`@Observable` view-model; `CareerState` is this project's runtime model,
analogous to neither exactly). `VehicleHistoryEvent` ported from
`WTRLCore/Runtime/DeveloperLab.swift` (plain data, no dependency on
anything Runtime-specific there, so ported here where its one real
consumer — `RecordHistory` — lives). Depends on `WTRL.Core` (empty),
`WTRL.Vehicle`, `WTRL.Garage`, `WTRL.Racing`, `WTRL.Events`, `WTRL.RPG`.

## Public API

**`CareerState`**: the runtime model — `Money`, `Reputation`,
`OwnedPartIds`, `InstalledComponents` (per vehicle), `CompletedRaceIds`,
`RaceRecords` (best time per race), `VehicleHistory` (per vehicle), plus
the RPG/Racing state that lives alongside it: `RivalBehavior`
(`WTRL.Racing.RivalBehaviorRuntime`), `ReputationState`, `SafetyRating`,
`DriverLicense` (all `WTRL.RPG`), `SavedRecipes`
(`List<WTRL.RPG.SavedBuildRecipe>`). `Clone()`/`CopyFrom(other)` — the
clone-mutate-commit pair `CareerTransaction.Apply` uses for atomicity.

**`CareerCommand`** (discriminated-union struct, same pattern as
`WTRL.Vehicle.ShiftExperimentMode`): `Earn`, `Spend`, `AcquirePart`,
`Install`, `CompleteRace`, `RecordHistory` — 6 static factory methods,
construct via those, not the fields directly.

**`CareerTransaction.Apply(IReadOnlyList<CareerCommand>, CareerState)
-> bool`**: applies a batch atomically. Builds a candidate via
`state.Clone()`, mutates the candidate, and only calls
`state.CopyFrom(candidate)` if every command in the batch succeeded — a
single failing `Spend` (insufficient funds) means the ENTIRE batch's
effects are discarded, `state` is left completely untouched. This
atomicity, not the individual command handlers, is the actual value of
this type — carried over verbatim from the Swift original's design.

## Deliberate deviations from the Swift source

1. **`.recordEvidence(TestRunEvidence)` is not ported.** The Swift
   original has 7 command cases; this port has 6. `TestRunEvidence`
   lives in `WTRL.Lab`, and `WTRL.Career`'s asmdef has no dependency on
   `WTRL.Lab`. This is the same "where does the type live" question
   `WTRL.RPG`'s `BuildRecipe` already resolved once (see `RPG/
   CONTRACT.md`) — flagged here as an open question, not silently
   dropped or resolved by adding a `WTRL.Lab` dependency unilaterally.
   **Options, mirroring the `BuildRecipe` resolution**: (a) add the
   `WTRL.Lab` dependency to `WTRL.Career` (breaks the current module
   boundary, but `WTRL.Career` already depends on more assemblies than
   any other module, so the cost is lower here than it was for
   `WTRL.RPG`), or (b) have `WTRL.Career` track evidence by opaque
   string id only, the way `WTRL.RPG.SavedBuildRecipe` references its
   target recipe. Not resolved in this pass — ask before choosing.
2. **`CareerState.Clone()` does NOT deep-clone `RivalBehavior`/
   `ReputationState`/`SafetyRating`/`DriverLicense`/`SavedRecipes`** —
   only the fields `CareerTransaction`'s 6 commands actually touch are
   cloned/committed. If a future command needs to mutate one of those
   atomically as part of a batch, `Clone()`/`CopyFrom()` need extending
   first — documented in `Clone()`'s own XML doc so this isn't
   discovered the hard way.
3. **`CareerState` is a mutable class**, not a value-copied struct
   (same reasoning as `WTRL.Racing.RivalBehaviorRuntime` — a long-lived
   service a caller holds and mutates in place, not Swift's
   copy-on-write value semantics).

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project alongside `WTRL.Vehicle`/`WTRL.Racing`/
`WTRL.Garage`/`WTRL.Events`/`WTRL.RPG` (all of `WTRL.Career`'s
dependencies). **0 errors, 0 warnings** on the first attempt. 9 new
tests: 1 ported term-for-term (`testCareerTransactionIsAtomicOnFailure`)
and 8 new (one per command handler, plus `Clone()`'s independence
guarantee) since the Swift test suite only exercised the atomicity
property directly. All pass.

## Not yet ported / not yet designed

- The `.recordEvidence` gap above.
- Reputation/safety-rating/license state currently lives in
  `CareerState` but nothing in this assembly actually calls into
  `WTRL.RPG`'s `ReputationState.RecordEvent`/`SafetyRatingState
  .RecordEvent` when a `CareerCommand.CompleteRace`/similar fires — the
  wiring between "a race finished" and "reputation/safety moved" is real
  design work the spec (`48-RPG-SYSTEMS-SPEC.md`) describes but this
  pass didn't implement, since it needs race-result detail (contact,
  clean overtakes, etc.) that doesn't exist as structured data anywhere
  yet.
- Nothing in `WTRL.Racing`'s `RivalBehaviorRuntime.RecordResult` is
  called from `CareerTransaction` either, for the same reason — a race's
  rival-specific win/loss detail isn't part of any `CareerCommand` yet.
