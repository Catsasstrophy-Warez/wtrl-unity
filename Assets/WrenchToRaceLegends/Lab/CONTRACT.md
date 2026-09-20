# WTRL.Lab — contract

Ported from `SwiftRacer/Sources/WTRLCore/Simulation/DynoSimulation.swift`,
`Content/Definitions.swift` (`DynoSample`, `DynoRun`), and
`Runtime/RunEvidence.swift` + `Runtime/Telemetry.swift`. Depends on
`WTRL.Core` (empty), `WTRL.Vehicle`, `WTRL.Racing` (declared in the
asmdef graph; not actually used by anything ported so far — see "Not yet
ported"), and `WTRL.Garage` (for `VehicleConfigurationResolver` and
`InstalledComponent`).

## Public API

**Dyno** (`Definitions.cs`, `DynoSimulation.cs`): `DynoSample`, `DynoRun`,
`DynoConfiguration`, `DynoSimulation.Run(VehicleDefinition,
TransmissionDefinition, SuspensionDefinition?, EngineDefinition,
DynoConfiguration, duration, sampleHz)` — sweeps idle→redline in a fixed
gear (real, sourced math: engine torque curve × nitrous/pressure factors
× final-drive-scale-adjusted ratio), computes wheel torque/power per
sample plus session peaks.

**Telemetry** (`Telemetry.cs`): `RuntimeTelemetrySample`,
`RuntimeTelemetryRing` — a fixed-capacity circular buffer that always
returns samples in chronological order regardless of internal write
position.

**Evidence** (`RunEvidence.cs`): `VehicleConfigurationFingerprint` (a
stable FNV-1a hash of everything that affects vehicle behavior — final
drive, installed components sorted by slot/id so order never matters,
tuning — real content-addressable identity for a configuration, not a
random ID), `EvidenceKind`, `TestRunEvidence`, `RunComparison`
(metric-delta between two evidence records across the union of both
their metric keys).

## Deliberate deviations from the Swift source

1. **No content catalog** (same pattern as every prior assembly) —
   `DynoSimulation.Run` takes the base vehicle/transmission/suspension/
   engine directly instead of resolving them from `vehicleID`.
   `VehicleConfigurationResolver.Resolve` itself already required no
   catalog (see `WTRL.Garage/CONTRACT.md`), so this only added the engine
   parameter.
2. Everything else — the dyno sweep math, the fingerprint hash, the
   telemetry ring's wraparound logic — is a direct, term-for-term port.

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project (subfoldered per assembly this time, per the
filename-collision lesson from `WTRL.Garage/CONTRACT.md`). **0 errors, 0
warnings** (after fixing 2 nullable-reference warnings on `DynoRun`). 6
new tests: 2 ported (`testDynoIsDeterministic`,
`testConfigurationFingerprintChangesWithPart`) and 4 new (order-
independence of the fingerprint hash, telemetry-ring wraparound/
pre-wraparound behavior, `RunComparison`'s metric-delta union) — no
Swift equivalent existed for these because the Swift tests exercised
`RuntimeTelemetryRing` only indirectly through `WTRLRuntime`, which isn't
ported yet. **24/24 passing combined** with `WTRL.Vehicle` (5),
`WTRL.Racing` (8), and `WTRL.Garage` (6).

## Not yet ported

- Nothing from `WTRL.Racing` is actually consumed yet, despite the
  asmdef declaring the dependency (inherited from the original module
  plan in `PIVOT-PLAN.md`, which grouped "Workshop | ... | WTRL.Garage,
  WTRL.Lab" without specifying exactly which Racing types Lab would need
  — turned out to be none so far). Leave the reference in place; a real
  "compare a dyno run against a track evidence run" feature would need
  it.
- The full `ValidationLaboratory`/`EvidenceTopology*` pipeline (evidence
  → proposal → materialize → validate → adopt) from `SwiftRacer`'s later
  waves — real and tested in Swift but never actually adopted a vehicle
  there either (see `SwiftRacer/Documentation/BUILD-READINESS.md`).
  Bigger scope than this pass; port separately if the adoption pipeline
  is actually needed rather than the dyno/telemetry/evidence primitives
  this pass covered.
- `WTRLRuntime` itself (the 120 Hz game-loop wrapper that drives
  `VehicleSimulation.Step` through a `FixedStepClock` and appends to a
  `RuntimeTelemetryRing` every frame) — that's `WTRL.Runtime`'s job, and
  nothing in `WTRL.Runtime` exists yet.

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
