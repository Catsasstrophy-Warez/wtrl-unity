# WTRL.Runtime — contract

Ported from `SwiftRacer/Sources/WTRLCore/Runtime/WTRLRuntime.swift`,
`PresentationState.swift`, and `SamplingPolicy.swift`. This is the
composition root: it owns a `FixedStepClock`, drives
`VehicleSimulation.Step` at a fixed physics rate regardless of the
wall-clock frame delta it's handed, derives presentation/audio state
every call, and records rate-gated telemetry into a
`RuntimeTelemetryRing`. Depends on `WTRL.Core` (empty), `WTRL.Vehicle`,
`WTRL.Lab` (for `RuntimeTelemetryRing`/`RuntimeTelemetrySample`).

This is the last assembly in the "keep going with Career, then
Persistence, then Runtime" sequence, and the last of the ten assemblies
ported term-for-term from SwiftRacer this pass (Vehicle, Racing,
Garage, Lab, RPG, World, Events, Career, Persistence, Runtime).

## Public API

`SamplingPolicy` (physicsHz/telemetryHz/numericUiHz/diagnosticsHz,
`.Default` = 120/30/15/5 — ported verbatim from `SamplingPolicy.swift`).
`WTRLSnapshot` (the read-only external view: `Vehicle`, `ElapsedSeconds`,
`VehicleId`, `SurfaceId`, `Diagnostics`, `Presentation`, `Audio`).
`WTRLRuntime` — `Snapshot` (thread-safe read), `TelemetrySamples`,
`Reset(vehicleId?, x, z, heading)`, `SetSurface(id)`,
`SetShiftMode(mode)`, `Advance(frameDelta, input, vehicle, engine,
transmission, tire, suspension, surface?)`. `VehiclePresentationState`,
`AudioLayerState`, `VehicleAudioState` — pure derived-state structs, no
runtime dependency of their own beyond `WTRL.Vehicle` types.

## Deliberate deviations from the Swift source

1. **No content catalog (the same deviation every prior assembly
   made).** Swift's `advance(frameDelta:input:)` resolved the active
   vehicle's engine/transmission/tire/suspension/surface internally via
   `VehicleConfigurationResolver.resolve(vehicleID:...)` and
   `CanonicalContent` lookups every single frame. No catalog exists
   anywhere in this project. `Advance` therefore takes all of those
   already-resolved definitions as required parameters on every call,
   matching `VehicleSimulation.Step`'s own signature exactly. Whichever
   layer eventually owns per-frame content resolution — still an open
   architecture question across the whole project, same as it was for
   `WTRL.Garage`/`WTRL.RPG`'s `BuildRecipe` question — is responsible
   for calling `VehicleConfigurationResolver.Resolve` (caching it across
   frames where nothing installed changed) and passing the result here.
2. **`suspension` is non-nullable and required, not optional with a
   fallback.** This was caught and corrected during this port itself,
   before it ever compiled: an early draft made `suspension` nullable
   with a synthesized placeholder default when null, which directly
   contradicts the deliberate discipline `WTRL.Vehicle.VehicleSimulation
   .Step` established specifically so this project never silently
   fabricates a fallback the way Swift's old catalog-lookup-with-default
   could. Caught through self-review before compiling, not by the
   compiler or a test — worth recording since it's the first instance
   this pass where the discipline itself, not tooling, caught a
   regression risk. `surface` remains nullable/optional, matching
   `VehicleSimulation.Step`'s own signature (a flat/unknown-surface
   default is a legitimate physics case, unlike suspension).

## A real bug this port's verification caught (not self-review this time)

`WTRLRuntime` originally initialized its `FixedStepClock` field with
`new FixedStepClock()` (both in the field initializer and in `Reset`).
This compiled with 0 warnings — but `dotnet test` hung indefinitely,
burning CPU in an actual infinite loop (confirmed by watching the
`testhost` process's CPU time climb into the hundreds of seconds with
zero test-runner output).

Root cause: for a **struct**, a zero-argument `new T()` call in C#
always binds to the implicit compiler-provided zero-initializer — it
does **not** call a user-defined constructor whose parameters are all
optional, even though `new T()` would ordinarily be valid overload-
resolution syntax for calling `FixedStepClock(double hz = 120)`. This
is a genuine, easy-to-miss C# gotcha specific to structs (classes don't
have this behavior — `new SomeClass()` against a `SomeClass(double hz =
120)` constructor does call it normally). The result was `Step == 0`,
which turns `FixedStepClock.Consume`'s `while (_accumulator >= Step)`
into an unconditional infinite loop the very first time `Advance` is
called with any positive `frameDelta`.

Fixed by always constructing with the rate spelled out explicitly —
`new FixedStepClock(SamplingPolicy.Default.PhysicsHz)` in the field
initializer (a static default, since instance property initializers
run in declaration order and `SamplingPolicy` the property is declared
*after* the `_clock` field, so referencing the instance property here
would read its own not-yet-initialized backing value) and `new
FixedStepClock(SamplingPolicy.PhysicsHz)` in `Reset` (the instance
property, since by then it may have been customized by the caller).

This is the first bug in this entire ten-assembly porting pass that
neither a compiler warning nor self-review caught — only running the
actual test suite did. It is a strong argument for never skipping the
`dotnet test` step in favor of `dotnet build` alone, even when a build
comes back clean.
3. **`System.Threading.Lock`-equivalent via a plain `object` + `lock`**
   instead of Swift's `NSLock`. Functionally identical mutual exclusion;
   no behavioral difference.

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project (`/tmp/runtime_check/`) containing all ten
now-shipped assemblies' source (Vehicle, Racing, Garage, Lab, RPG,
World, Events, Career, Persistence, Runtime), each in its own
subfolder to avoid the same-filename collision this pass already hit
once during the Garage port.

`dotnet build`: **0 errors, 0 warnings** on the first attempt.

New tests (`WTRLRuntimeTests.cs`, no direct 1:1 Swift-test file to port
since `WTRLCoreTests.swift`'s runtime-level tests exercised a real
hero-1965-catalog this port has no catalog for; these instead assert
the same behavioral guarantees the Swift source's own tests and
comments describe):
- `FixedStepDeterminism_60HzAndTwiceAt120HzConverge` — the correct form
  of the determinism check `WTRL.Vehicle`'s own port had to work around
  by driving `FixedStepClock` directly (see `Vehicle/CONTRACT.md`).
  Now that `WTRLRuntime.Advance` itself owns the fixed-step clock, this
  drives it through the real public API at two different wall-clock
  frame deltas and asserts convergence.
- `TelemetryHasFourCornerEvidence` — ports the spirit of
  `testExpandedTelemetryHasFourCornerEvidence`.
- `TelemetrySamplingIsRateGatedNotEveryPhysicsStep` — asserts the
  30Hz-vs-120Hz rate gate actually reduces sample count, not just that
  telemetry exists.
- `ResetReturnsToStartingPositionButKeepsSurface`,
  `SetShiftModeIsReflectedInSnapshot`, `DiagnosticsAreRecomputedEveryAdvance`
  — new coverage with no isolated Swift-test equivalent (these were only
  ever exercised as side effects of other tests in the Swift suite).

`dotnet test`: **76/76 passing project-wide** (70 prior across the nine
earlier assemblies + 6 new Runtime tests), after fixing the
`FixedStepClock` construction bug described above.

## Not yet ported

- No scene-loading, results-flow, or `RaceState`-style 8-state wrapper
  around `WTRL.Events.RaceRuntimeState`'s 4-phase state machine (flagged
  as a Rev16.1-reuse-worthy pattern for later, not resumed this pass).
- No Unity Editor has ever opened this project in any environment
  across this entire session — every verification, including this one,
  has been throwaway plain-.NET compilation, never an actual
  Unity/IL2CPP build. `WTRLRuntime` has no `MonoBehaviour`/`Update()`
  wrapper yet; that integration layer (calling `Advance` from Unity's
  frame loop with `Time.deltaTime`) does not exist yet and is the
  natural next step once a Unity Editor is available to verify it in.

## Device quality profiles (2026-09-20)

Closes "no device quality profiles" from the Milestone M8 gap audit.
New `DeviceQualityProfile.Apply(tier)` sets real
`QualitySettings`/`Application.targetFrameRate` values for three fixed
tiers (Low/Medium/High). HONEST LIMITATION: no automatic device
benchmarking or tier detection exists -- that requires running on real
hardware, which this environment cannot do (see PIVOT-PLAN.md's
standing note that no device testing of any kind has occurred here).
A caller (a settings menu, or a build-time default) picks the tier
explicitly; this doesn't invent device detection it can't verify.
Verified via 3 PlayMode tests asserting real `QualitySettings` state
after each tier is applied.
