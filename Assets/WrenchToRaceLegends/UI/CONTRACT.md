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

## Real track geometry in the scene, and a second orientation-bug hunt (2026-09-20)

`VerticalSlice.unity`'s Foundry Row circuit is now a real ribbon-road
mesh (`Art/Tracks/foundry-row-circuit.fbx`, generated by
`racinggame/BlenderPipeline/scripts/generate_world_tracks.py`), not
just primitive node markers (which are kept alongside it for AI-
waypoint visibility).

**A second real orientation bug was found and fixed differently from
the vehicle fix.** The same FBX-export axis-remap issue that affected
the vehicle models recurred for the track mesh (length landed on Y
instead of Z). This time, instead of applying a corrective rotation on
the Unity side (the vehicle approach), the correction was baked
directly into the exported vertex data in the Blender script itself --
because a THIRD bug was found while investigating: in this
environment's `-nographics` headless batchmode,
`Transform.localToWorldMatrix` (and `Renderer.bounds`) reproducibly do
NOT reflect a rotation applied to an object immediately after
instantiation, even after `Physics.SyncTransforms()` and even when
`Transform.rotation.eulerAngles` itself correctly reports the new
value. This made a runtime-rotation fix unverifiable through this
project's usual bounds-diagnostic method for a flat, direction-
sensitive mesh like a road surface (a wrong rotation would silently
mirror the track or invert its face normals with no way to catch it
via this environment's tooling).

Fixed by making `generate_world_tracks.py` emit already-correctly-
oriented vertices directly (pre-compensating for the exporter's known
remap in the vertex construction itself, plus a matching face-winding
reversal to keep normals pointing up after the necessary X-mirror),
verified by loading a **freshly-instantiated, untouched** copy of the
resulting FBX (no post-instantiation rotation involved) and confirming
its bounds exactly match the C# waypoints' real coordinate range.

**Re-confirmed the vehicle fix from the earlier pass is NOT affected
by the newly-found batchmode quirk**: that fix's rotation value is
correctly serialized into `VerticalSlice.unity` as a real
`PrefabInstance` modification override (`m_LocalRotation` = the exact
quaternion for -90° about X, found by grepping the saved scene file's
override *values*, not just its property-path list) -- the quirk only
affects reading back a rotation's EFFECT within the same batchmode
script execution, not whether the assignment itself is correctly
saved. A real Unity Editor loading this scene normally will apply that
saved rotation correctly.

`ModelBoundsDiagnostic.cs` now documents this batchmode-matrix quirk
in its own class doc, as a standing warning against reusing its
pattern to verify a post-instantiation rotation fix in the future.

## Elevation, barriers, and real texture for every track (2026-09-20)

Closes the three gaps named directly in a follow-up request: no
elevation, no barriers, no texture.

**Elevation**: Whisperwood Forest Circuit and Cliffside Coastal Circuit
now have real per-node vertical elevation (`elevationM` arrays in
`world_tracks_manifest.json` -- a 22m crest for Whisperwood, a 24m
single steep drop for Cliffside matching `Racing/SampleContent`'s own
comment identifying node 4 as "the signature drop"). This is a real
Y-axis height change baked into the ribbon mesh, confirmed via
`ModelBoundsDiagnostic` (Y-size jumped from ~0 to ~28-30m for exactly
these two tracks, matching the authored elevation range, while every
other track stayed flat). **Still not a terrain/heightmap system** --
this project has none; it's linear interpolation between hand-placed
waypoint heights on an otherwise flat-shaded ribbon, not real ground
geometry the vehicle could feel through suspension travel.

**Barriers**: every track now has two low wall ribbons (one per edge),
generated the same way as the road surface, with a red/white striped
material. A single straight wall per side, not crash-tested guardrail
geometry or a real barrier system.

**Texture**: the road surface has a real procedurally-generated
asphalt texture (subtle per-pixel noise plus a dashed white
centerline, tiled every 8m along the track via real per-vertex UVs)
instead of a flat color; barriers get the red/white stripe texture.

**A real Blender/Unity interop bug was found and worked around while
adding this.** Blender's FBX exporter's `embed_textures=True` produced
a file that (confirmed via a raw byte-level string search) genuinely
contains the texture data, but Unity's FBX importer never wired the
resulting material's texture slot regardless of how the source image
was packed or saved on the Blender side (tried both `Image.pack()` and
saving to an actual PNG file first — neither worked). Rather than keep
chasing that specific interop gap, the generator now also writes each
texture as a plain, separately-importable PNG file
(`Art/Tracks/Textures/`), and `VerticalSliceSceneBuilder` loads those
directly and builds the URP/Lit materials from them in code, bypassing
FBX texture extraction entirely. Confirmed working by grepping the
saved scene file for the materials' actual `_BaseMap` texture GUIDs
and matching them against the real texture assets' own GUIDs.

Re-confirmed 104/104 EditMode + 4/4 PlayMode tests still pass.

## Vehicle sub-materials, textured ground, and building-shaped facility markers (2026-09-20)

A further, open-ended "improve graphics/visuals/assets" pass. Three
changes, each verified by scene-file inspection rather than a
screenshot (still no interactive-Editor visual confirmation exists in
this environment):

**Vehicle sub-materials (fixing a real regression from the earlier
visual pass)**: `AttachVehicleModel` used to paint every single
renderer -- tires, glass, chrome trim, lamps, the whole interior --
the same flat body-paint color. This was checked directly against the
real mesh part names (confirmed via a Blender `bpy.data.objects` name
dump of `1967_crownfire_v7.blend`: `BODY_SHELL`, `TIRE.NNN`,
`RIM.NNN`, `GLASSHOUSE`, `HEADLAMP_BEZEL`, `TAIL_LAMP`, `SEAT_BACK`,
`STEERING_WHEEL`, `V8_BLOCK`, etc. -- these are real, meaningful part
names the source `.blend` already carries, not something invented
this pass). `AttachVehicleModel` now keys off `renderer.gameObject
.name` (case-insensitive substring match) to assign one of 7 distinct
materials: body paint, matte black tire, metallic rim, a genuinely
transparent glass material (URP `_Surface`=Transparent, alpha blend,
`ZWrite` off, `_SURFACE_TYPE_TRANSPARENT` keyword), chrome trim
(bumpers/grille/mirrors/handles/pillars/bezels), warm lamp material
(headlamps/tail lamps), and a dark interior material (seats/dash/
steering wheel/engine block/intake/brakes). Verified by grepping the
rebuilt scene for `_SURFACE_TYPE_TRANSPARENT` (appears exactly twice
-- once per vehicle's glass material, as expected) and for the raised
`m_Materials:` reference count on vehicle renderers (up from 1 shared
material to 7 distinct ones).

**Textured ground plane**: the ground was a single flat dark color.
Generated a real 512x512 procedural grass/dirt texture via Blender's
own pixel API (same "author pixels directly, save as plain PNG,
skip FBX embedding entirely" pipeline already proven necessary for
the track asphalt/barrier textures -- see the entry above on why FBX
texture embedding doesn't survive Unity's importer). Saved to
`Assets/WrenchToRaceLegends/Art/Environment/Textures/
world_ground_grass.png` and also mirrored into `racinggame/
BlenderPipeline/export/world_tracks/textures/` for provenance.
`BuildGround` now loads it directly and tiles it 20x across the
ground plane via `mainTextureScale`. There is still no real terrain
system -- the ground remains a flat plane with zero height variation,
which is an honest, documented limitation, not a claim of finished
environment art. Verified by grepping the rebuilt scene for the
texture asset's own `.meta` GUID (`6451feec7f15d184c81f448dfb5ceeff`)
and confirming it appears in the saved scene file.

**Building-shaped facility markers**: "Garage" and "Gas Station" used
to be two identical bare cubes distinguished only by color.
`BuildFacilityMarker` now builds a small building silhouette per
facility: a body cube, a peaked roof cube rotated 45 degrees, and an
emissive sign panel colored to match the facility's own color. Still
a placeholder, not real building art -- no real facility geometry
exists anywhere in the research corpus to model against, and this is
explicitly documented as such in the method's doc comment. Verified
by grepping the rebuilt scene for `m_Name: Body`, `m_Name: Roof`, and
`m_Name: Sign` (6 matches total = 2 facilities x 3 parts each, as
expected).

Re-confirmed 104/104 EditMode + 4/4 PlayMode tests still pass, and
`Scripts/validate_structure.sh` still reports 16 assemblies, 70 C#
files, no reference cycles.

## Real terrain height variation (2026-09-20)

Closes the ground-flatness gap named directly by the user after the
previous pass ("no terrain system exists" was called out as a known
limitation, not fixed at the time).

`BuildGround` no longer creates a `PrimitiveType.Plane` (which has
zero height variation by construction, no matter what texture is on
it). It now builds a real 120x120-quad mesh (14,641 vertices) with
per-vertex height from a two-octave `Mathf.PerlinNoise` field
(±6m amplitude), via a new `BuildTerrainMesh()` helper.

**A real placement bug was caught and fixed before ever running this
in Unity**, by re-checking the actual waypoint data instead of
assuming the track sat near the world origin: an initial draft
faded the terrain height to zero via a radial falloff from `(0,0,0)`
with only a 45m flat radius. But `Racing.SampleContent
.FoundryRowCircuitLine()`'s real waypoints span x:[-20,220],
z:[0,100] -- bounding-box center (100,50), far from the origin -- so
that draft would have left most of the actual track sitting on
sloped, noisy terrain while the flat track mesh itself stayed at a
fixed height, a visible seam/clipping bug. Fixed by switching to a
rectangular flat zone (`FlatZoneMin`=(-60,-40), `FlatZoneMax`=
(260,140) -- the real track bbox plus 40m margin, also covering both
facility markers) with a 25m smoothstep feather, and by centering the
terrain mesh itself on the track's real bounding-box center (100,50)
instead of the origin, at a large enough half-size (300m) to still
extend well past the flat zone in every direction.

**A second bug was caught the same way (by inspecting the saved
scene file, not by looking at it)**: the `MeshCollider` added to the
Ground GameObject was never actually given the generated mesh --
`ground.AddComponent<MeshCollider>()`'s return value was discarded
instead of having `.sharedMesh` set, so the ground would have had a
visible terrain mesh but zero collision surface. Fixed by capturing
the component and assigning `meshCollider.sharedMesh = mesh`.

Verified: the rebuilt scene's console log reports a real height range
(`[-5.15, 4.45] m`) rather than a flat 0, confirming actual
displacement happened; the flat zone bounds are logged and checked
by hand against the real track waypoint range; and the saved scene
file was inspected directly for both the `MeshFilter` and
`MeshCollider` components on `Ground`, confirming both reference the
same embedded `TerrainMesh` object (a real Mesh asset serialized
inline into the `.unity` file, 14,641 vertices / 86,400 indices,
matching the 120x120 grid) rather than one of them being left null.
As always, none of this has been confirmed by an actual screenshot or
human eyes in an interactive Editor session -- that capability does
not exist here.

Re-confirmed 104/104 EditMode + 4/4 PlayMode tests still pass, and
`Scripts/validate_structure.sh` still reports 16 assemblies, 70 C#
files, no reference cycles.
