# WTRL.Editor — contract

Was an empty stub assembly (asmdef only, zero source files) until this
pass, despite PIVOT-PLAN.md describing an intended future role (content
builders, validators, adapted from Rev16.1's "Rebuild + Validate"
editor-menu pattern). Now contains the project's first real editor
tooling.

## Public API

`HeroContentBuilder.BuildHero1965()` — `[MenuItem("Assets/WTRL/Build
Hero-1965 Content Assets")]`, also callable in batchmode via
`-executeMethod WTRL.EditorTools.HeroContentBuilder.BuildHero1965`.
Creates a full hero-1965 `WTRL.Content` configuration (Engine,
Transmission, Suspension, Tire, Surface, Vehicle) as real `.asset`
files under `Content/Generated/`, using `ScriptableObject
.CreateInstance` + `AssetDatabase.CreateAsset` rather than hand-authored
YAML.

## Why this exists instead of hand-authoring `.asset` files

Every `WTRL.Content` CONTRACT.md entry up to this point deliberately
avoided hand-typing ScriptableObject `.asset` YAML, reasoning that a
malformed one would be worse than none. This builder is the resolution
of that deferral: real content, created through the actual Editor API,
verified by inspection of the resulting files' GUIDs and cross-
references rather than assumed correct.

## A real Unity Editor bug found while building this

The first version of this builder produced a broken
`VehicleDefinition_Hero1965.asset` — `m_Script: {fileID: 0}`, a
malformed `m_EditorClassIdentifier` — while its sibling assets
(Engine/Transmission/Suspension/Tire) serialized correctly. Root cause,
confirmed by isolation testing: **only the first `ScriptableObject`
type declared in a `.cs` file reliably gets a correct serialized
script reference when created via `AssetDatabase.CreateAsset`** in
this Unity version's batchmode. This wasn't a stale-cache or
wrong-path artifact — a brand-new asset at a never-used path exhibited
the same bug. Fixed by splitting every `WTRL.Content` type into its
own file (see `Content/CONTRACT.md` for the full account). Left a
comment in each of those files warning against re-consolidating them.

## Values note

`BuildHero1965`'s numbers mirror the fixture values already used
throughout `Tests/EditMode` (`WTRLRuntimeTests.MakeVehicle`/
`MakeEngine`/etc.) for consistency with what the test suite already
treats as a plausible mid-60s muscle-car baseline. They are **not**
sourced/cited research-corpus data the way this project's other
content is graded — see `HeroContentBuilder.cs`'s own doc comment for
the full caveat, including the still-unresolved `hero-1965` (code
default) vs. `hero_1967` (research corpus's actual first hero
generation) naming mismatch.

## Not yet done

- No validators (canon/content/serialization checks) — PIVOT-PLAN.md's
  originally-envisioned role for this assembly, still open.
- No builders for the other assemblies' content (Garage parts, RPG
  build recipes, World tracks/facilities, Racing rival profiles) —
  only the hero vehicle has a builder so far.
- No batch importer connecting `racinggame/ImportedVehicleCorpus`'s
  real research-corpus JSON catalogs to these builders — `BuildHero1965`
  hardcodes its numbers in C#, it doesn't read from any external data
  source yet.
