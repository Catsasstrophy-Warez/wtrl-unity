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
`SurfaceDefinitionAsset`, `VehicleDefinitionAsset` — `ScriptableObject`s
with `[CreateAssetMenu]`, each holding inspector-editable fields and a
`ToDefinition()` method that produces the corresponding `WTRL.Vehicle`
record. Each type lives in its own `.cs` file (see "A real Unity Editor
bug" below for why).

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

## A real Unity Editor bug found and fixed (2026-09-20)

Real `.asset` instances now exist (see below) — created programmatically
via `WTRL.Editor.HeroContentBuilder` (`AssetDatabase.CreateAsset`)
rather than hand-authored YAML, specifically to avoid shipping a
malformed one. The first attempt surfaced a genuine, reproducible Unity
Editor bug: **only the first `ScriptableObject` type declared in a
`.cs` file reliably gets a correct serialized script reference
(`m_Script`/`m_EditorClassIdentifier`) when created via
`AssetDatabase.CreateAsset` in this environment's batchmode.** Every
subsequent type in the same file got `m_Script: {fileID: 0}` and a
malformed `m_EditorClassIdentifier` (missing the namespace/class dot
separator) — confirmed reproducible across multiple runs, a full
`AssetDatabase.Refresh()`, and even a brand-new never-used asset path,
ruling out caching/stale-guid explanations. Confirmed the fix by
isolating each type into its own file: every asset then serialized
with a correct script reference. All 6 `WTRL.Content` types now live in
their own `.cs` files for this reason — **do not consolidate them back
into one file** without re-confirming this isn't still a problem in
whatever Unity version is current at the time.

**Real `.asset` instances created and verified**: `Content/Generated/`
holds a full hero-1965 configuration (Engine/Transmission/Suspension/
Tire/Surface/Vehicle), built by `HeroContentBuilder.BuildHero1965`.
Cross-references between them (e.g. `VehicleDefinitionAsset.engine`)
are real Unity object-reference GUIDs, confirmed by direct inspection
of the generated `.asset` YAML. Field values mirror the fixture data
already used throughout `Tests/EditMode` (not sourced research-corpus
data — see `HeroContentBuilder.cs`'s own doc comment).

**Still not covered**: no Inspector-based hand-editing of these assets
has happened (only programmatic creation), so double-click-and-tweak
UX (in particular `TransmissionDefinitionAsset.ratios`'s `double[]`
field) is confirmed to serialize correctly on write but not confirmed
pleasant to edit by hand in the Inspector.
