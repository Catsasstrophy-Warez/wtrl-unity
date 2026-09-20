# WTRL.Content — contract

New assembly, not a port from SwiftRacer (Swift never needed this layer
because it wasn't built for a Unity Inspector). Answers the open
question every prior `CONTRACT.md` flagged as "whatever eventually owns
content resolution" — this is that owner, in the narrowest form that
still respects the no-catalog discipline every other assembly
established.

## Public API

`EngineDefinitionAsset`, `TransmissionDefinitionAsset`,
`SuspensionDefinitionAsset`, `TireDefinitionAsset`,
`VehicleDefinitionAsset` — `ScriptableObject`s with `[CreateAssetMenu]`,
each holding inspector-editable fields and a `ToDefinition()` method
that produces the corresponding `WTRL.Vehicle` record.

## Deliberate design choice: still no lookup-by-id

This assembly does **not** introduce a global catalog or an
id-to-asset dictionary anywhere. `VehicleDefinitionAsset` holds direct
object references to its `EngineDefinitionAsset`/
`TransmissionDefinitionAsset`/`SuspensionDefinitionAsset`/
`TireDefinitionAsset` (Unity object references, assigned by hand in
the Inspector), not string ids resolved against something. A scene
that wants a vehicle still needs a direct reference to one of these
assets — the same "caller must supply it, nothing is ever silently
substituted" rule `VehicleSimulation.Step`/`WTRLRuntime.Advance` already
enforce. `VehicleDefinitionAsset.Awake()`-time validation (in
`WTRL.UI.VehicleRuntimeController`) fails loud, not silent, if any
sub-asset is unassigned.

If a real catalog (load-by-id from Addressables, a `Resources` folder,
or similar) is wanted later, it belongs in a separate loader that
*produces* one of these direct references — not a change to this
assembly's shape.

## UNVERIFIED — no dotnet-based verification is possible here

Every prior WTRL assembly (Vehicle through Runtime) was verified via a
throwaway `dotnet build`/`dotnet test` project, because none of them
reference `UnityEngine`. This assembly's entire purpose is Unity
integration (`ScriptableObject`, `[CreateAssetMenu]`), so that method
does not apply — there is no way to compile or test this code outside
an actual Unity Editor.

**This code has never been compiled by anything.** The only Unity
Editor installations available in this environment (6000.6.0f1,
6000.7.0a6 — neither matches the project's pinned 6000.0.58f2, which
is not installed here) are unlicensed; a batchmode open attempt during
this session hung indefinitely at
`[Licensing::Module] Licensing is not yet initialized.` waiting for a
license client that requires the user's own interactive sign-in, which
cannot be done on their behalf. The process was killed rather than left
hanging.

**Before trusting this code**, the user (or a future session with a
licensed Editor) must open the project and confirm the Console shows
zero compile errors. Likely candidates for a first-compile fix, since
this was written without ever seeing Unity's actual compiler diagnostics:
- `double` fields showing as edit boxes rather than sliders in the
  Inspector is expected (no `[Range]` attributes were added — this is
  intentionally left for whoever tunes these values by feel).
- `double[]` (used for `TransmissionDefinitionAsset.ratios`) should
  serialize fine as a native Unity array type, but has not been
  Inspector-tested.

## Not yet done

- No actual `.asset` instances exist — creating one (e.g. "hero-1965")
  requires the Editor's Create menu; hand-authoring a ScriptableObject
  `.asset` YAML file was deliberately avoided in this pass rather than
  risk shipping a malformed one the user would have to debug blind.
- No `SurfaceDefinition` wrapper yet (not needed until a scene actually
  varies surface — `WTRLRuntime.Advance`'s `surface` parameter is
  nullable/optional, unlike the others).
