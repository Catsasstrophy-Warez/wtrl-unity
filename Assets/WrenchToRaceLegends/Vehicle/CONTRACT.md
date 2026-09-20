# WTRL.Vehicle — contract

Ported from `SwiftRacer/Sources/WTRLCore/Simulation/` (`VehicleSimulation
.swift`, `DynamicsSubsystems.swift`, `PowertrainSolver.swift`) and
`SwiftRacer/Sources/WTRLCore/Content/Definitions.swift` +
`AdvancedDefinitions.swift`. Depends only on `WTRL.Core` (currently
empty — see its own `CONTRACT.md`; nothing here actually needed anything
from it).

## Public API

**Content definitions** (`Definitions.cs`) — plain classes with
`init`-only properties, looked up by `Id` by whatever owns a catalog
(nothing yet — see "Deliberate deviations" below):
`VehicleDefinition`, `EngineDefinition`, `TransmissionDefinition`,
`SuspensionDefinition`, `TireDefinition`, `SurfaceDefinition`,
`TorquePoint`. Enums: `DriveLayout`, `DifferentialKind`,
`BrakeArchitecture`, `TransmissionKind`, `SurfaceKind`.

**Simulation state** (`SimulationState.cs`) — mutable structs, mutated
per-frame via `ref`: `VehicleInput`, `TireState`, `BrakeState`,
`DamageState`, `SuspensionState`, `WheelState`, `DrivetrainState`,
`CornerSuspensionState`, `VehicleSimState`, `VehicleTuning`,
`DiagnosticFinding`. **Read `VehicleSimState`'s XML doc before copying
one** — it contains reference-type arrays (`Tires`, `Wheels`,
`SuspensionCorners`) that do NOT deep-copy on struct assignment the way
they did in Swift. Call `.Clone()` for an independent copy.

**Solvers** (`DynamicsSubsystems.cs`, `PowertrainSolver.cs`) — static
classes, pure functions (except where they take `ref` state to mutate):
`EngineSolver.TorqueNm`, `TireSolver.GripCoefficient`/`CombinedForces`,
`DifferentialSolver.Split`, `SuspensionSolver.Update`/`ApplyAntiRoll`,
`AeroSolver.DragForceN`/`RollingResistanceN`, `WheelSolver.Update`,
`ChassisSolver.Integrate`, `ShiftController.Update`,
`PowertrainSolver.UpdateEngineSpeed`.

**Entry point** (`VehicleSimulation.cs`):
```csharp
VehicleSimulation.Step(
    ref VehicleSimState state, VehicleInput input, VehicleDefinition vehicle,
    EngineDefinition engine, TransmissionDefinition transmission, VehicleTuning tuning,
    TireDefinition tire, SuspensionDefinition suspension,
    SurfaceDefinition? surface = null, double bankingDegrees = 0,
    ShiftExperimentMode shiftExperimentMode = default, double dt = 0);

List<DiagnosticFinding> VehicleSimulation.Diagnose(in VehicleSimState state);
```
Also `FixedStepClock` — accumulates variable frame time and invokes a
callback a fixed number of times at a constant dt (default 120 Hz),
clamping any single frame's contribution to 0.25s. **Any caller that
wants frame-rate-independent determinism must drive `Step` through a
`FixedStepClock`, not by calling `Step` directly with whatever `dt` the
engine's `Update()` reports.** This is not a suggestion — it's the actual
mechanism `WTRLCoreTests.testFixedStepDeterminism` (and this port's own
`FixedStepDeterminism_...` test) verifies.

## Deliberate deviations from the Swift source

1. **No content catalog.** Swift's `VehicleSimulation.step` reached into
   a global `CanonicalContent.tire(id) ?? CanonicalContent.tires[1]`
   (and the equivalent for suspension) from inside the physics function
   itself. `WTRL.Vehicle` has no such catalog and won't get one — `tire`
   and `suspension` are now required parameters. **Whoever builds
   `WTRL.World`/`WTRL.Runtime`'s content system is responsible for
   resolving a vehicle's `tireDefinitionId`/`suspensionId` to an actual
   object before calling `Step`, every frame.** This is a real behavior
   change, not just a refactor: the Swift version tolerated a missing id
   with a silent fallback; this version doesn't, by design.
2. **`ShiftController.ExperimentMode`** (Swift enum with an associated
   value, `case fixedGear(Int)`) is ported as a small
   `ShiftExperimentMode` struct (`.Normal` / `.FixedGear(n)`), since C#
   enums can't carry payloads. Functionally identical.
3. **`VehicleSimState` array aliasing** — see the API section above and
   the type's own XML doc. This is the single most likely place a future
   bug will come from if someone ports code that assumes Swift's value
   semantics without reading this warning.
4. **Added after `WTRL.Garage` shipped**: `EngineDefinition`,
   `TransmissionDefinition`, `SuspensionDefinition`, `TireDefinition`,
   `SurfaceDefinition`, `VehicleDefinition` are C# `record`s, not plain
   classes (they were plain classes when this assembly first shipped).
   `WTRL.Garage`'s `VehicleConfigurationResolver` needed to derive a
   modified copy of a base definition — Swift does this by mutating a
   local `var` copy of a value-type struct, which a plain C# class with
   `init`-only properties can't replicate without either mutating a
   shared instance in place or hand-rolling per-type copy constructors.
   Records give `with`-expression copying for free while keeping
   immutability. Re-verify anything built against these types before this
   change if it assumed reference/class identity semantics — `record`
   equality is structural (value-based), not reference-based, which is a
   real behavior difference from the plain-class version if anything
   relied on `ReferenceEquals` or dictionary-keying by object identity.

## Verification

**No Unity Editor was available anywhere in this port's authoring
environment.** Every other claim in this project (`SwiftRacer/`,
`racinggame/`) has had to be verified by reading alone. This port is
different: `dotnet` (the .NET SDK) IS available, and since none of these
files reference any `UnityEngine` API, they compile and run as plain
.NET. Verification actually performed:

- `dotnet build` against a throwaway class-library project containing
  exactly these 5 source files: **0 errors, 0 warnings** (after fixing 6
  nullable-reference-type warnings the first build surfaced).
- `dotnet test` (NUnit) against 5 tests ported from
  `WTRLCoreTests.swift`/`WTRLAdvancedTests.swift` — determinism via
  `FixedStepClock`, oval banking reducing lateral load-transfer spread,
  zero-banking leaving physics unchanged, 20,000 fuzzed-input steps
  staying finite, and `VehicleSimState.Clone()` producing independent
  arrays: **all 5 pass.**
- One of those 5 tests **failed on first port** and caught a real
  mistranslation: the determinism test was initially ported as "one
  `Step` call at dt=1/60 equals two `Step` calls at dt=1/120," which is
  not actually true for this (or any nonlinear) integrator and is not
  what the Swift test checks. The Swift test's real mechanism is
  `FixedStepClock` decoupling wall-clock frame time from a fixed physics
  dt. Rewritten to actually drive both branches through
  `FixedStepClock.Consume`, which is what made it pass. This is
  documented here because the same mistake is easy to make again if
  `WTRL.Runtime`'s eventual game loop calls `Step` directly instead of
  through a clock.

**Not yet verified**: this is still outside an actual Unity project
build (the throwaway check project is plain .NET, not Unity's IL2CPP/
Mono toolchain, and doesn't exercise Unity-specific serialization,
`.asmdef` boundaries, or `UnityEngine.Object` interop). The first real
Unity Editor open of `WTRL-Unity/` should re-run these tests inside
Unity Test Framework (they're plain NUnit `[Test]` methods, so no
rewrite should be needed) as the next verification step.

## Not yet ported

- `RivalIntimidation.swift`, `TrackAI.swift` → `WTRL.Racing` (depends on
  this assembly's types but isn't part of it).
- `EnrichedRev36VehicleCatalog.swift`, `CanonicalContent.swift`'s actual
  vehicle/engine/transmission/suspension/tire data → wherever the content
  catalog ends up living (see deviation #1 above — this is now an open
  architecture question, not just a data-entry task).
- `ValidationLaboratory*.swift`, `EvidenceTopology*.swift` → `WTRL.Lab`.
