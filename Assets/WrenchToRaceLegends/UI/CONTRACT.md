# WTRL.UI — contract

Was an empty stub assembly (asmdef only) until this pass. Now contains
the project's first `MonoBehaviour` — the smallest possible end-to-end
vertical slice connecting a `WTRL.Content` asset to `WTRL.Runtime` to
an actual Unity `Transform`.

## Public API

`VehicleRuntimeController` (`MonoBehaviour`) — takes a
`VehicleDefinitionAsset` (see `WTRL.Content/CONTRACT.md`, now a public
field, not `[SerializeField] private`, so tests can assign it without
reflection or an Editor-only dependency), constructs a `WTRLRuntime` in
`Awake`, reads WASD/arrow keys via the new Input System in
`FixedUpdate`, and delegates the actual simulate-and-apply step to
`internal void Tick(VehicleInput)` (exposed to
`WTRL.Tests.PlayMode` via `[InternalsVisibleTo]` — see "Verified to run"
below for why `Tick` exists as a separate method). Exposes
`CurrentSnapshot` for a future HUD (not wired to anything yet).

Added `Unity.InputSystem` and `WTRL.Content` to this assembly's asmdef
references to support it.

## Explicitly out of scope for this pass

No visuals (no mesh, no wheels, no camera follow), no HUD, no scene
file, no ScriptableObject asset instances. This is deliberately the
narrowest possible slice — a box-with-a-script proving the full chain
compiles and runs, not a playable vehicle. Adding those is the natural
next step once this compiles cleanly.

## VERIFIED to compile AND run (2026-09-20)

The user activated a Unity Personal license, unblocking a real
batchmode open (Unity 6000.6.0f1). `WTRL.UI.dll` compiles cleanly.

**The end-to-end chain now has real, automated proof it runs**:
`WTRL.Tests.PlayMode.VehicleRuntimeControllerTests` (4 tests, all
passing via `Unity.exe -runTests -testPlatform PlayMode`) exercises
`VehicleRuntimeController` inside an actual Play session — confirming
`Awake`'s validation, `FixedUpdate`'s per-frame simulate-and-apply
step, and the missing-content fail-loud path all behave correctly at
runtime, not just at compile time.

**A real environment limitation was found, not a code bug**:
simulating a held keyboard key via `InputSystem.QueueStateEvent` +
`InputSystem.Update()` proved unreliable under this project's
`-nographics -batchmode` PlayMode runs — a queued key-down event read
back as pressed immediately, but had reverted to released by the very
next `FixedUpdate`, even re-queued every frame. This looks like a
constraint of headless/no-window Play sessions, not a bug in
`VehicleRuntimeController`'s own keyboard-reading code. Worked around
by splitting the per-frame simulate step into `internal void
Tick(VehicleInput)`, which `FixedUpdate` now delegates to and which
tests can call directly with explicit input — still exercising the
real Content-asset → `WTRLRuntime.Advance` → `Transform` chain, just
without depending on unreliable simulated hardware state. See
`VehicleRuntimeControllerTests.cs`'s class doc comment and its
`KeyboardInputSimulationIsUnreliableInHeadlessBatchmode` test (which
documents rather than hides the limitation) for the full trail.

**Still open**: a real, non-`-nographics` interactive Play session
(the user actually pressing Play and watching WASD move something on
screen) has not happened — that's the one thing this automated
verification cannot itself confirm, since it needs an actual window
and hardware keyboard focus.

## Real content now exists

`WTRL.Editor.HeroContentBuilder` (see `Editor/` — the project's first
real editor tooling) programmatically creates a full hero-1965
`VehicleDefinitionAsset` + its four sub-assets in
`Content/Generated/`. Building this surfaced and fixed a real Unity
Editor bug — see `Content/CONTRACT.md`'s "A real Unity Editor bug
found and fixed" section.

## Best-effort UI/systems pass (2026-09-20) — functional, not designed

Added, all confirmed to compile clean in a real Unity Editor:

- **`WorldStreamingController`** — wires `WTRL.World.WorldStreamingGrid`
  (previously fully presentation-agnostic, nothing consumed its
  load/unload deltas) to a follow target's transform. Does NOT
  instantiate/destroy any content itself — no cell-content prefabs
  exist yet — it only raises the real signal for a future consumer.
- **`VehicleAudioController`** — maps `VehicleAudioState`'s 6 procedural
  layers to 6 real `AudioSource`s (volume from `Gain`, pitch from
  `FrequencyHz` against a reference frequency). No audio clips are
  assigned anywhere — silent until real recorded loops exist.
- **`TelemetryHud`**, **`GarageScreen`**, **`DynoScreen`** — functional
  IMGUI (`OnGUI`) screens, not Canvas/UGUI. No scene, prefab, or font
  asset existed to build real UI against, and hand-authoring
  RectTransform layouts blind would be worse than an honest plain
  panel. `GarageScreen` only lists the 3 part ids
  `VehicleConfigurationResolver` actually recognizes
  (`WTRL.Garage.SampleContent.RecognizedParts`) — buying/installing a
  part is a real `CareerTransaction`, not a mock. `DynoScreen` re-runs
  `DynoSimulation.Run` live as its 3 sliders move.
- **`CareerStateHolder`** — minimal scene-level owner of one fresh
  `CareerState()`; nothing wires it to a real save file yet.
- **`SimpleFollowCamera`** — plain lerp-follow, not Cinemachine-based
  despite `com.unity.cinemachine` being installed. Configuring a real
  Cinemachine rig needs visual iteration this pass can't do blind.
- **Touch + tilt input** added to `VehicleRuntimeController` (additive
  with keyboard): tap-zone throttle/brake, accelerometer-based steering
  with deadzone + power-curve response. This is NOT a port of Rev16.1's
  real `MobileInputMath.cs` (that archive wasn't read in this pass) —
  a reasonable equivalent shape, flagged as such in the code.

**None of this has been visually reviewed.** Every piece above compiles
and (where covered by `VehicleRuntimeControllerTests`) runs without
exceptions, but nobody has looked at the Garage/Dyno/HUD screens on
screen, heard the audio, or driven with tilt on a real device. Treat
all of it as a first functional pass awaiting real review, not a
finished feature.
