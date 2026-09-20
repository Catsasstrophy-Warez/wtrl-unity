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

## VERIFIED — compiles clean in a real, licensed Unity Editor

Update: the user activated a Unity Personal license after this
CONTRACT.md was first written, unblocking the batchmode-open attempt
that had previously hung indefinitely. `Unity.exe -batchmode -nographics
-quit` against Unity 6000.6.0f1 (the project's original pin,
6000.0.58f2, is not installed in this environment; the project was
retargeted to 6000.6.0f1) now completes successfully: `WTRL.Content.dll`
is produced in `Library/ScriptAssemblies`, and every `.meta` file this
assembly needed was generated for the first time.

Two real compile errors surfaced project-wide on this first successful
open (not specific to this assembly, but Content and everything
depending on it couldn't build until they were fixed — see
`Core/CONTRACT.md` and the individual `CONTRACT.md`s for `Career`,
`Garage`, `Lab`, `RPG`): a missing `IsExternalInit` polyfill (needed for
every `init`/`record` in the project under Unity's .NET Standard 2.1
profile) and the C# 11 `required` keyword not being available under
Unity's default language version — both fixed; see `Core/
IsExternalInitPolyfill.cs` and the `required`-removal notes in the
affected assemblies' `CONTRACT.md`s. This assembly itself needed no
changes.

**Still not covered by this verification**: no `.asset` instance of
any of these `ScriptableObject`s has been created and Inspector-tested
yet (see "Not yet done" below), so field serialization (in particular
`double[]` for `TransmissionDefinitionAsset.ratios`) is confirmed to
*compile* but not yet confirmed to *serialize/edit correctly* in the
Inspector.

## Not yet done

- No actual `.asset` instances exist — creating one (e.g. "hero-1965")
  requires the Editor's Create menu; hand-authoring a ScriptableObject
  `.asset` YAML file was deliberately avoided in this pass rather than
  risk shipping a malformed one the user would have to debug blind.
- No `SurfaceDefinition` wrapper yet (not needed until a scene actually
  varies surface — `WTRLRuntime.Advance`'s `surface` parameter is
  nullable/optional, unlike the others).
