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

## Clone() added to RivalBehaviorRuntime; first authored track content (2026-09-20)

Added `RivalBehaviorRuntime.Clone()` (deep copy of the memory
dictionary) for the same `CareerTransaction` atomicity reason as
`WTRL.RPG`'s three `Clone()` additions — see `Career/CONTRACT.md`.

Also added `Racing/SampleContent.FoundryRowCircuitLine()` — the
project's first authored (non-fixture) `TrackLineDefinition`, paired
with `WTRL.World.SampleContent.FoundryRowCircuit()`. The two share only
a duplicated string id (`"foundry-row-circuit"`), not a reference —
`WTRL.Racing` deliberately has no dependency on `WTRL.World`, confirmed
the hard way when an earlier version referencing it directly failed to
compile in a real Unity Editor (a cross-assembly error the throwaway
`dotnet test` method missed entirely, since it flattens every assembly
into one folder). See `Career/CONTRACT.md` for the full account and the
process lesson.

## AiVehicleSession — real physics under AI control (2026-09-20)

`Racing/AiVehicleSession.cs` — the AI-driving analog of
`WTRL.Runtime.WTRLRuntime`/`WTRL.UI.VehicleRuntimeController`: drives
real `VehicleSimulation.Step` physics every call, sourcing input from
`TrackAiDriver` instead of a keyboard. Closes the gap between two
previously separate proofs: `VehicleRuntimeControllerTests` (real
physics, player input, no AI) and `SampleContentTests`'s AI-line test
(real AI perception, simplified kinematic movement instead of real
physics). This is the first test coverage in the project exercising
real physics under AI control end to end.

Same no-catalog discipline as everything else: every definition is a
required constructor parameter.

## Every documented track's AI racing line, plus a reusable oval generator (2026-09-20)

Added racing lines for all 7 new `WTRL.World` tracks. The 3 oval
tracks (Cutback Tri-Oval, Longbow Speedway, Highbank Superspeedway)
share a new `BuildOvalLine` helper that procedurally generates a
stadium-shape loop (2 straights + 2 semicircular ends) from just
straight length + turn radius + speeds, rather than hand-placing dozens
of node coordinates that would only look precise. The 3 road-course
archetypes (Whisperwood/Cliffside/Ironclad) are hand-authored node
loops, same discipline as Foundry Row. The drag strip (Redline Raceway)
deliberately has no racing line -- `WTRL.Events.DragRaceRules` already
drives progression by straight-line distance, not waypoint-following.

`AiVehicleSessionTests`-style integration coverage was extended
(`SampleContentTests.AiVehicleSessionCanFollowEveryNewTrackLineWithoutError`)
to confirm `TrackAiDriver.Perceive` never fails and every node is
reachable in sequence for all 6 new lines, including shapes the AI
driving code had never seen before (the procedurally-generated ovals).

## Intimidation wiring closed: RivalIntimidation/TrackAI were already ported, now actually driven (2026-09-20)

A "what's left" roadmap item asked for `RivalIntimidation.swift`/
`TrackAI.swift` to be ported into `WTRL.Racing`. Found both already
existed as real, tested code (`RivalIntimidation.cs`,
`TrackAIDriver.cs`, `Tests/EditMode/RivalIntimidationTests.cs`) --
`Vehicle/CONTRACT.md`'s "Not yet ported" note was simply never updated
when that port landed (fixed there too). The actual remaining gap was
narrower: `TrackAiDriver`'s intimidation-aware `Input` overload existed
and was tested in isolation, but nothing in the real AI-driving loop
(`AiVehicleSession.Step`) ever called it -- every AI-driven vehicle
always used the plain, non-intimidation overload regardless of any
`RivalIntimidationState` a caller might have.

Closed by giving `AiVehicleSession` optional `Intimidation`/`Proximity`
properties: when unset, behavior is byte-for-byte unchanged (every
existing caller/test); when set, `Step` samples
`RivalDeterministicSample` keyed by the session's own advancing tick
(not wall-clock or a shared static RNG -- the same fixed-step-safe
discipline the Swift original's own 2026-09-19 fix required) and calls
the intimidation-aware overload. `WTRL.Racing` still has no dependency
on `WTRL.Career` -- computing a real `RivalIntimidationState` from a
career-level loss count remains the caller's job, same "resolved by the
caller" discipline as every other definition in this assembly.

Also added `ContactDetector`/`TrackProgress`/`OvertakeTracker`
(`ContactDetector.cs`) -- real contact/overtake DETECTION primitives
closing "the concrete, narrow blocker for further Career progress"
named in `Career/CONTRACT.md`. Deliberately detection-only: wiring
these into `RaceSessionController`/`RaceCompletionBridge` needs a
caller with both vehicles' live simulation state each frame, which
doesn't exist yet (player state lives in `VehicleRuntimeController`,
rival state in `AiVehicleSession`, with no shared per-frame comparison
point today) -- real, separate follow-on work.

4 new PlayMode tests exercise the intimidation wiring through real
physics (finite/stable over a 3600-step run, deterministic replay
across two independent sessions given identical inputs). 8 new EditMode
tests cover `ContactDetector`/`TrackProgress`/`OvertakeTracker`.
137/137 EditMode + 22/22 PlayMode tests pass.
