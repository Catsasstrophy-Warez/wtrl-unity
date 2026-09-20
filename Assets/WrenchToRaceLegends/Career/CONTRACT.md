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
- ~~Reputation/safety-rating/license state currently lives in
  `CareerState` but nothing in this assembly actually calls into
  `WTRL.RPG`'s `ReputationState.RecordEvent`/`SafetyRatingState
  .RecordEvent`~~ — **CLOSED, 2026-09-20**: see "Closed a long-flagged
  gap: race completion now updates RPG/Racing state" and "Race-
  completion -> Career wiring closed" below. `RaceCompletionBridge`
  now calls both on every real race completion via
  `RaceFlowController.Completed`. Left in place struck-through, rather
  than deleted, so this file's own history stays legible — this
  section was NOT updated when that gap closed, which is itself a
  documentation-hygiene lesson: always update/strike the original gap
  entry in the same pass that closes it, don't just append a new
  "closed" entry further down and leave the original claim standing.
- ~~Nothing in `WTRL.Racing`'s `RivalBehaviorRuntime.RecordResult` is
  called from `CareerTransaction` either~~ — **PARTIALLY CLOSED,
  2026-09-20**: `CareerTransaction.RecordRaceOutcome` calls it whenever
  `RaceOutcomeDetail.RivalId` is set, but `RaceCompletionBridge`
  deliberately never sets `RivalId` (see that type's own doc comment:
  no win/loss detection exists to back it honestly). So the mechanism
  is wired and tested, but never actually exercised by the one real
  caller that exists today — still effectively dormant in practice,
  just not for the reason this stale entry claimed.

## Unity-Editor compile fix (2026-09-20)

The first real, licensed Unity Editor open of this project (6000.6.0f1)
surfaced two classes of compile error every prior `dotnet build`/
`dotnet test` verification pass couldn't catch, since neither depends
on `UnityEngine`:

1. Missing `System.Runtime.CompilerServices.IsExternalInit` (needed for
   every `init` accessor/`record`) — fixed once, project-wide, via
   `WTRL.Core/IsExternalInitPolyfill.cs`.
2. This assembly's use of the C# 11 `required` keyword failed with
   "Feature 'required members' is not available in C# 9.0." An attempt
   to fix this via a per-assembly `<AssemblyName>.rsp` file (Unity's
   documented mechanism for per-assembly compiler args) did not take
   effect in a real Editor compile, for reasons not fully diagnosed —
   see `Core/IsExternalInitPolyfill.cs`'s doc comment for the full
   account. Rather than ship another speculative polyfill on top of an
   unverified compiler-plumbing workaround, every `required` property in
   this assembly was converted to a constructor parameter (with any
   remaining optional properties staying `init`-only) — the same
   pattern `WTRL.Vehicle/Definitions.cs` already established. Call sites
   updated accordingly.

Verified: `dotnet test` (76/76, unchanged) confirms this refactor
didn't change behavior, and this assembly's `.dll` now also compiles
cleanly inside a real, licensed Unity Editor — the first time anything
in this project has been proven to build there.

## Closed a long-flagged gap: race completion now updates RPG/Racing state (2026-09-20)

Every `CareerTransaction`/`CareerState` note up to this point flagged
the same open item: nothing calls `ReputationState.RecordEvent`/
`SafetyRatingState.RecordEvent`/`RivalBehaviorRuntime.RecordResult`
when a race completes, because no structured race-result data existed
to drive them. `RaceOutcomeDetail.cs` is that data, and
`CareerCommand.RecordRaceOutcome` (a 7th command kind, alongside the
existing `CompleteRace` which is kept for callers that only care about
best-time tracking) is the command that applies it atomically:
`CompletedRaceIds`/`RaceRecords`, a named-rival win via
`RivalBehaviorRuntime.RecordResult` + `ReputationState
.RecordNamedRivalWin` (diminishing returns), format-based reputation
for a rival-less Touge win, and every `SafetyEvent` case based on
caller-supplied conduct flags.

**`RaceOutcomeDetail`'s conduct flags (contact, clean overtake, etc.)
must be supplied by the caller** — this assembly has no collision or
track-limit detection of its own; that needs real scene colliders,
which don't exist anywhere in this project yet. A caller with no way
to know otherwise passing all-default (clean) flags is honest; a
caller that DOES have collision data defaulting them away for
convenience would not be.

**A real atomicity bug was caught and fixed while building this**:
`CareerState.Clone()` reference-copied `RivalBehavior`/
`ReputationState`/`SafetyRating`/`DriverLicense` (safe only because no
command mutated them yet — its own doc comment already warned about
this exact class of bug). `RecordRaceOutcome` is the first command
that mutates them mid-transaction, which would have broken
`CareerTransaction.Apply`'s atomicity guarantee (a failed `Spend`
later in the same batch would still have left the RPG/Racing
side-effects applied, since candidate and original shared the same
object). Fixed by adding real `Clone()` methods to `ReputationState`,
`SafetyRatingState`, `DriverLicenseState` (RPG), and
`RivalBehaviorRuntime` (Racing), and using them in `CareerState.Clone()`.
`RecordRaceOutcomeIsAtomicOnFailureAlongsideOtherCommands` and
`CloneDeepCopiesRivalAndRpgState` (`Tests/EditMode/CareerTests.cs`)
guard against this regressing.

## RaceSession — wires RaceRules/RaceRuntimeState to a CareerCommand

New type, `Career/RaceSession.cs`: owns a `RaceDefinition` +
`RaceRuntimeState`, delegates countdown/lap/sector progression to
`WTRL.Events.RaceRules`, and once finished (`IsFinished`), builds a
`CareerCommand.RecordRaceOutcome` via `BuildOutcomeCommand`. Deliberately
thin — no scene loading, no rendering, no checkpoint/collision
detection (those aren't systems yet). This is what "wire RaceRules
into a session" and "wire race completion into Career" actually look
like as code, not the full event-preflight/results-flow experience
Rev16.1's audit recommended (still not implemented — see
`Events/CONTRACT.md`).

## First authored (non-fixture) circuit content

`World/SampleContent.FoundryRowCircuit()` (a `TrackDefinition`) and
`Racing/SampleContent.FoundryRowCircuitLine()` (a `TrackLineDefinition`
with 8 waypoint nodes) — "Foundry Row" is one of the two Blackridge
vertical-slice candidates PROJECT-MAP-UNITY-MOBILE.md names. The two
share only the string id `"foundry-row-circuit"`, duplicated rather
than referenced, since `WTRL.Racing` deliberately has no dependency on
`WTRL.World` — confirmed the hard way: an earlier version referenced
`World.SampleContent` directly from `Racing/SampleContent.cs` and
failed to compile in the real Unity Editor with CS0103, a
cross-assembly reference error the throwaway `dotnet test` method
couldn't catch because it flattens every assembly into one folder,
hiding real asmdef boundaries. **Lesson for future sample/test content
spanning two assemblies with no dependency between them: always
confirm with a real Unity Editor compile, not just `dotnet test`, when
adding a file that references another assembly's types.**
`SampleContentTests.TrackAiDriverCanFollowTheFoundryRowLineAllTheWayAround`
is a real integration test proving `WTRL.Racing.TrackAiDriver`
(previously only fixture-tested) can actually drive this circuit's
waypoints end to end.

Node coordinates and target speeds are placeholder shapes, not derived
from any real track survey — flagged the same way the rival roster's
Blender blockout dimensions were.

## EventPreflightService — scoped-down gate (2026-09-20)

`EventPreflightService.Evaluate`/`CanEnter` — real implementation of
the event-preflight gating the Rev16.1 audit recommended
(`Assignments/OUTPUT-Rev16.1-Audit.md`), deliberately scoped to only
the two axes real data already backs: reputation
(`RaceDefinition.ReputationRequired` vs `CareerState.Reputation`) and
Safety Rating/license gating for knockout entry. Rev16.1's full gate
also checked chapter, lineage, homologation, fuel plan, and loadout —
none of those are systems that exist anywhere in this project yet, and
faking checks against data that doesn't mean anything would be worse
than not building them. Extending this to those axes is real future
work, gated on those systems existing first.

## Race-completion -> Career wiring closed (2026-09-20)

Closes the gap `RaceOutcomeDetail.cs`'s own doc comment already named:
"nothing calls `ReputationState.RecordEvent`/`SafetyRatingState
.RecordEvent`/`RivalBehaviorRuntime.RecordResult` when a race
completes." `RaceOutcomeDetail` and `CareerTransaction`
(`RecordRaceOutcome`) already existed and already handled every field
correctly -- the actual missing piece was that nothing in the
race-flow lifecycle ever constructed one and called
`CareerTransaction.Apply`.

`Events.RaceFlowController` now exposes a `Completed` event (fired
from `Complete()`, using only `WTRL.Events`' own types --
`WTRL.Events` still has no dependency on `WTRL.Career`, and shouldn't
gain one just for this). New `Career.RaceCompletionBridge` subscribes
to it and applies a `RaceOutcomeDetail` built from real data.

**Honest limitation, not fixed here**: this project has no contact
detection, no rival-position comparison, and no win/loss determination
anywhere in the simulation -- `RaceRuntimeState` only tracks lap/
sector/penalty timing. `RaceCompletionBridge` can therefore only
honestly populate `RaceId`/`ClassifiedTimeSeconds`/`Format` from real
data; every contact/rival/win field is left at its safe default.
Concretely: `RivalId` is deliberately left null even for races with a
named rival in `RaceDefinition.RivalIds`, because setting it with
`PlayerWon` hardcoded false would incorrectly record a LOSS against
that rival on every single completion -- worse than not recording
anything. The one real effect every completion currently gets is
`SafetyEvent.EventCompletedZeroIncidents` (accurate: no incident of
any kind is or can be detected yet) plus best-classified-time
tracking. Reputation/rival-memory effects stay dormant until a real
contact/win-detection system exists to feed this honestly -- this is
now the concrete, narrow blocker for further Career progress, not a
vague "needs structured race-result data" note.

Verified with 3 new tests in `CareerTests.cs` that drive a real
`RaceFlowController` through its full phase sequence (Loading ->
Staging -> Countdown -> Racing -> Finishing -> Results -> Complete)
and assert the resulting `CareerState` was actually mutated by the
fired event -- not just that the bridge type compiles. Also confirms
detaching the bridge actually stops it reacting.
104/104 -> 107/107 EditMode tests pass (3 new), 4/4 PlayMode tests
still pass, `Scripts/validate_structure.sh` reports 16 assemblies, 72
C# files, no reference cycles.

## First real AnalyticsConsent call site (2026-09-20)

`RaceCompletionBridge.OnRaceCompleted` now also calls
`WTRL.Core.AnalyticsConsent.Record("race_completed", ...)` with the
real race id/format/classified time -- the first real call site for
the project's new opt-in analytics infrastructure (see
`Core/AnalyticsConsent.cs`'s own doc comment for its honest scope: no
real vendor SDK, just the consent gate + in-memory log). No-ops
entirely unless the player has opted in (default: opted out).
Verified with a test that opts in, drives a real race to completion,
and asserts the real event/parameters were recorded.
