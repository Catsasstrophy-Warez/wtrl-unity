# WTRL.Racing — contract

Ported from `SwiftRacer/Sources/WTRLCore/Simulation/TrackAI.swift`,
`RivalIntimidation.swift`, `DriverController.swift`, and
`WTRLCore/Runtime/RivalBehaviorRuntime.swift` (`RivalMemory` from
`WorldConsequences.swift`). Depends on `WTRL.Core` (empty) and
`WTRL.Vehicle` (for `VehicleInput`/`VehicleSimState`).

## Public API

**AI driving** (`AiTypes.cs`, `TrackAIDriver.cs`): `DriverModel`,
`TrackNode`, `TrackLineDefinition`, `DriverController.Input(...)`,
`DriverPerception`, `TrackAiDriver.Perceive(...)` /
`TrackAiDriver.Input(...)` (two overloads — plain line-following, and the
intimidation-aware one that takes a `RivalIntimidationState` and two
**required, explicit** deterministic samples).

**Rival intimidation** (`RivalIntimidation.cs`): `RivalIntimidationCeilings`,
`RivalIntimidationState`, `RivalIntimidation.Ceilings` (the real,
sourced per-rival values — six rivals, every ceiling traced to
`52-CONTENT-RESOLUTION-PASS-1.md`/`45-RIVAL-DEVELOPMENT.md` in the
research corpus, comments carried over verbatim), `RivalIntimidation.Update(...)`.

**Rival memory** (`RivalBehaviorRuntime.cs`): `RivalMemory`,
`RivalBehaviorSnapshot`, `RivalBehaviorRuntime` (canonical owner —
intimidation is always recomputed from memory, never persisted
separately), `RivalDeterministicSample` (FNV-1a based, for the two
required samples `TrackAiDriver.Input` needs — never call
`System.Random`/`UnityEngine.Random` for these).

## Deliberate deviations from the Swift source

1. **`RivalBehaviorRuntime` is a mutable class, not a value-copied
   struct.** Swift's version is a struct with `mutating func`s, consistent
   with that codebase's value-semantics style throughout. C# consumers
   (a long-lived career/game-state service) are expected to hold one
   instance and mutate it in place, so a reference type is the more
   natural fit here — there is no `Clone()`/aliasing concern like
   `VehicleSimState`'s, because nothing here holds arrays.
2. Everything else is a direct, term-for-term port — same discipline as
   `WTRL.Vehicle`, for the same reason: `RivalIntimidation.Ceilings`'
   values are sourced content, not something to paraphrase.

## Verification

Same method as `WTRL.Vehicle` (see its `CONTRACT.md` for the full
rationale): a throwaway `dotnet build`/`dotnet test` project containing
this assembly's files plus `WTRL.Vehicle`'s (these files have no
`UnityEngine` dependency). **0 errors, 0 warnings on the first attempt.**
8 new tests ported from `RivalIntimidationTests.swift`,
`Reconciled3DeterministicIntimidationTests.swift`, and
`Wave20CanonicalRivalBehaviorTests.swift` — all pass. Combined with
`WTRL.Vehicle`'s 5, the full suite is 13/13 passing.

## Not yet ported

- `TrackGeometryRuntime`/`RaceDisciplineRuntime`-adjacent systems —
  deliberately not ported; `SwiftRacer` itself rejected these across
  several upload waves as a stale/regressed lineage (still referencing a
  `RaceFormat.drag` case that doesn't exist in the real content model).
  If `WTRL.Events` needs race-flow state, design it fresh against
  `SwiftRacer`'s actual `RaceRuntimeState`/`RaceRules`, not this lineage.
- The content side of rivals — which `VehicleDefinition` each rival
  drives, per-rival generation data — lives wherever the content-catalog
  architecture question from `WTRL.Vehicle/CONTRACT.md` gets resolved.
