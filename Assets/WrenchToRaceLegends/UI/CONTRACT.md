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

## Real vehicle geometry + visual pass (2026-09-20)

Both vehicles in `VerticalSlice.unity` are no longer invisible empty
GameObjects. Exported real geometry from the project's own documented
"accepted blockout baseline" (`racinggame/BlenderPipeline/
REFERENCE-MODELING-ACCEPTANCE.md`), NOT the disqualified procedural
fleet output the earlier rival-blockout pass used —
`correct_axis_heroes/1967_crownfire_v7.blend` (97 mesh objects) and
`correct_axis_heroes/marsh_nsx91_v6.blend` (92 mesh objects), the
latest versions past the doc's own last-recorded v5/v4 baseline. FBX
files live in `Assets/WrenchToRaceLegends/Art/Vehicles/`.

**A real orientation bug was caught and fixed via bounds measurement,
not eyes**: the source data authors length along its own Y axis and
height along Z (raw bounds ~2.35 × 4.99 × 1.46 for the hero body,
matching width/length/height in that order — not Unity's width/height
/length). Confirmed by writing `Editor/ModelBoundsDiagnostic.cs` and
comparing against two different FBX export axis-remap settings, which
produced *identical* bounds either way — proving the issue was in the
source mesh data's own axis usage, not the exporter's axis-remap
option. Fixed with a `-90°` X-axis rotation applied to both models on
instantiation (`VerticalSliceSceneBuilder.ModelAxisCorrection`), which
brings the bounds into a plausible Unity-space car shape
(width ≈ 2.35, height ≈ 1.46, length ≈ 4.99).

Flat "paint" materials (deep red for hero, silver for Marsh, both
metallic/glossy) are applied to every renderer on each model — the
source models have no real paint/material authoring yet per
`REFERENCE-MODELING-ACCEPTANCE.md`'s own outstanding-work list, so a
plain solid color is a genuine improvement over Unity's default
missing-material magenta, not a finished paint job.

Also added: a modest URP post-processing volume (subtle bloom, a
contrast/saturation lift, light vignette — safe, well-understood
defaults, not tuned by looking at the scene), warm directional
sunlight + trilight ambient + distance fog (replacing flat default
white light), an asphalt-colored ground material, and emissive
orange track-marker cylinders (replacing plain gray spheres). A new
`AiVehicleController` (wraps `WTRL.Racing.AiVehicleSession` as a
MonoBehaviour) drives the Marsh vehicle around the circuit under AI
control, so the scene now has two moving cars, not one.

**A second real Unity bug was found and fixed while wiring this up**:
loading the Marsh `VehicleDefinitionAsset` via `AssetDatabase
.LoadAssetAtPath<T>` immediately before `EditorSceneManager.NewScene`
consistently returned null for a valid, existing, independently-
loadable asset — while the identically-loaded hero asset (assigned to
a component right away) worked fine. Likely explanation: creating a
new scene lets Unity unload ScriptableObject assets nothing yet holds
a live reference to; the hero asset survived because it was assigned
to `controller.vehicle` immediately, the Marsh asset didn't survive
being held as a bare local across many intervening lines. Fixed by
reloading it right before use instead of caching it from before the
scene switch. Confirmed via three isolation attempts (a standalone
diagnostic method, the non-generic `LoadAssetAtPath` overload) before
landing on the real fix — worth remembering for any future
Editor-scripted content that loads an asset well before the scene
that will reference it exists.

**None of this has been seen.** Every claim above is backed by a
compile pass, a bounds measurement, or a file diff — not a screenshot.
