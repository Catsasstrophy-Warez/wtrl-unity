# WTRL.Garage — contract

Ported from `SwiftRacer/Sources/WTRLCore/Content/Definitions.swift`
(`PartDefinition`, `InstalledComponent`, `BuildRecipeDefinition`) and
`VehicleConfigurationResolver.swift`. Depends on `WTRL.Core` (empty) and
`WTRL.Vehicle`.

**This is genuinely thin.** SwiftRacer's own garage/workshop logic at the
`WTRLCore` layer is thin — most of what makes a workshop feel real
(inspection, physical part removal/install interaction, service stains,
evidence-first diagnosis) is UI/presentation work in `SwiftRacer/Sources/
RacingGame/` that hasn't been touched by this porting pass, plus design
intent in `racinggame/05-specifications/25-GARAGE-DESIGN.md` and
`PROJECT-MAP-UNITY-MOBILE.md`'s "Workshop" migration-matrix row that
hasn't been built yet at all. Don't mistake this assembly's current
small size for the workshop system being close to done.

## Public API

`PartDefinition`, `InstalledComponent`, `BuildRecipeDefinition` (two
fields — `RequiredTransmissionId`, `RequiredCrankType` — are sourced
content the Swift original explicitly flags as "not yet read or enforced
anywhere," carried over with the same honesty; a third,
`RequiredDifferentialType`, **is now enforced** — see below).

`BuildRecipeEvaluator.SatisfiesTarget(BuildRecipeDefinition,
VehicleDefinition, EngineDefinition)` (new — resolves the "where does
BuildRecipe live" question from `WTRL.RPG/CONTRACT.md`/`Assignments/
OUTPUT-RPG-Design.md`; the satisfaction check lives here since it needs
Vehicle/Engine/Differential types, while `WTRL.RPG.SavedBuildRecipe`
holds only the reward/progression state and references a target
definition by string id). Checks weight-to-power range and, when the
recipe specifies one, a required differential type — the real 35-recipe
corpus only ever uses `"lsd"`/`"lsdRace"`, both mapped to `ClutchLsd`/
`TorqueBiasing`; an unrecognized string fails rather than silently
passing.

`ResolvedVehicleConfiguration`, `VehicleConfigurationResolver.Resolve(
VehicleDefinition, TransmissionDefinition, SuspensionDefinition?,
IReadOnlyList<InstalledComponent>)` — applies a **hardcoded** set of
three part-ID effects (`final-drive-373`, `sport-tire`, `track-damper`).
Real, but exactly as limited as it sounds; nothing has extended this
since the Swift original either.

## Deliberate deviations from the Swift source

1. **No content catalog** (same as `WTRL.Vehicle`/`WTRL.Racing`) —
   `Resolve` takes the base `VehicleDefinition`/`TransmissionDefinition`/
   `SuspensionDefinition` directly instead of looking them up by
   `vehicleID`.
2. **This is what forced `WTRL.Vehicle`'s definition types from plain
   classes to C# `record`s** (see `Vehicle/Definitions.cs`'s updated
   header comment and `Vehicle/CONTRACT.md`). Swift's resolver mutates a
   local `var` copy of a value-type struct; a C# class with `init`-only
   properties can't do that without either mutating a shared instance in
   place (corrupting whatever else references the same canonical object)
   or hand-rolling a copy constructor per type. Records give the same
   "derive a modified copy" shape via `with` for free. **This changed an
   already-shipped, tested assembly — re-verify anything built against
   `WTRL.Vehicle`'s definition types if it assumed reference/class
   semantics.**

## Verification

Same method as `WTRL.Vehicle`/`WTRL.Racing`: a throwaway `dotnet build`/
`dotnet test` project containing all three assemblies' files (subfoldered
this time — a flat copy caused a real filename collision between
`Vehicle/Definitions.cs` and `Garage/Definitions.cs` that silently
overwrote one with the other on the first attempt; worth remembering for
whoever verifies the next assembly this way). **0 errors, 0 warnings**
(after fixing 2 nullable-reference warnings). 5 new tests ported from
`WTRLCoreTests.swift`/`WTRLAdvancedTests.swift`, plus one new test this
port added (`ResolvingDoesNotMutateTheOriginalDefinitions`, guarding the
record/`with` fix above) — all pass. **18/18 combined** with
`WTRL.Vehicle`'s 5 and `WTRL.Racing`'s 8.

## Not yet ported

- `CareerTransaction.swift`/`CareerCommand` (atomic multi-command apply:
  earn/spend/acquirePart/install/completeRace/recordEvidence/
  recordHistory) — this operates on `CareerSave`, a persistence-level
  type, so it belongs in `WTRL.Career`/`WTRL.Persistence` once those
  exist, not here, even though `acquirePart`/`install` are garage
  actions. Keep the atomicity property (`apply` either commits every
  command or none) when it's ported — that's the actual value of that
  function, not the individual command handlers.
- `DynoSimulation.swift` → `WTRL.Lab` (see `PIVOT-PLAN.md`'s migration
  matrix — Workshop/Lab are listed as two separate targets for exactly
  this split).
- The real workshop interaction layer (inspection, part removal/install
  presentation, service evidence) — no `WTRLCore`-layer source exists to
  port for this; it needs fresh design against `racinggame/05-
  specifications/25-GARAGE-DESIGN.md`.
