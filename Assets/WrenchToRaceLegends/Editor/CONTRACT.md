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
the full caveat.

**Naming decision, resolved 2026-09-20** (previously open): `hero-1965`
(code default) vs. `hero_1967` (research corpus's actual first hero
generation) stays as `hero-1965` permanently, not renamed. The corpus's
own `hero_generation_catalog.json` flags `hero_1967`'s OEM torque
values and period routing as unresolved "researchGates" -- there is no
better sourced data to switch to today, and renaming would break
`CareerState.SelectedVehicleId`'s default, every persistence test, and
all 5 `hero1965-*` `CanonicalBuildRecipes` entries for zero real gain.
Revisit if/when hero_1967's real specs are actually researched.

## Second builder: Marsh Gen 1 (2026-09-20)

`MarshContentBuilder.BuildMarshGen1()` — same pattern as
`HeroContentBuilder`, creating the rival roster's first real content
asset (`marsh-gen1`, matching `catalog_manifest.json`'s
`marsh_gen1_1991` Blender blockout profile for mass/wheelbase). This is
the first non-hero vehicle to exist as real `WTRL.Content` data, used
by `WTRL.Racing.AiVehicleSession` (see `Racing/CONTRACT.md`) to prove
AI-controlled real physics end to end.

## Not yet done

- No validators (canon/content/serialization checks) — PIVOT-PLAN.md's
  originally-envisioned role for this assembly, still open.
- No builders for the other assemblies' content (Garage parts, RPG
  build recipes, World tracks/facilities) or the remaining 27 rival
  generations — only Hero and Marsh Gen 1 have builders so far.
- No batch importer connecting `racinggame/ImportedVehicleCorpus`'s
  real research-corpus JSON catalogs to these builders — both builders
  hardcode their numbers in C#, neither reads from any external data
  source yet.

## Real content validators + a systemic unused-asmdef-reference audit (2026-09-20)

New `ContentValidator.cs` (`Assets/WTRL/Validate Content`) is the
"validators" half of this assembly's original vision (PIVOT-PLAN.md
described "content builders, validators" together; only builders
existed until now). Checks the specific real invariants this project's
content has actually needed guarded: no duplicate
`CanonicalBuildRecipes` ids, exactly 5 recipes per vehicle generation,
non-degenerate weight-to-power bounds, and every `Racing.SampleContent`
track-id constant resolving to a real, non-empty racing line whose own
`TrackId` matches. Most of these are already guarded by
`BuildRecipeTests`/`SampleContentTests`; this exposes the same checks
as a real Editor menu command runnable without opening the test
runner. Required re-adding `WTRL.Garage` to this assembly's own
references (removed in the audit below, then genuinely needed again by
this new file) -- a real example of why "remove everything unused"
audits should re-verify after each subsequent real addition, not just
once.

Also ran a project-wide audit for the exact class of bug found twice
already (`Persistence`/`Career` referencing assemblies they didn't use)
across all 16 assemblies: found and removed 18 more dead references
across `Editor` (8: World/Events/Garage/Lab/RPG/Career/Persistence/
Runtime), `Events` (2: Vehicle/World), `Lab` (1: Racing), `Racing` (1:
Events), `Runtime` (7: World/Events/Racing/Garage/RPG/Career/
Persistence), and `UI` (1: RPG). Every removal was verified real (not
just a `using` check, since implicit compiler-recognized types like
`WTRL.Core`'s `IsExternalInit` polyfill are used with no `using`
statement anywhere -- excluded from this audit for that reason) via
grep for both `using X` and qualified `X.Type` references, then
confirmed safe by a full recompile + test rerun after each batch of
removals (never broke a single test). `WTRL.Runtime` in particular went
from 10 references down to 3 (`Core`, `Vehicle`, `Lab`) -- it turned
out to genuinely need almost none of what its asmdef claimed.

137/137 EditMode + 22/22 PlayMode tests pass throughout, 16 assemblies
/ 92 C# files, no reference cycles.
