# WTRL.Garage — contract

Ported from `SwiftRacer/Sources/WTRLCore/Content/Definitions.swift`
(`PartDefinition`, `InstalledComponent`, `BuildRecipeDefinition`) and
`VehicleConfigurationResolver.swift`. Depends on `WTRL.Core` (empty) and
`WTRL.Vehicle`.

**This is genuinely thin.** SwiftRacer's own garage/workshop logic at the
`WTRLCore` layer is thin — most of what makes a workshop feel real
(inspection, physical part removal/install interaction, service stains,
evidence-first diagnosis) is UI/presentation work in `SwiftRacer/Sources/
RacingGame/` that hasn't been touched by this porting pass, plus design
intent in `racinggame/05-specifications/25-GARAGE-DESIGN.md` and
`PROJECT-MAP-UNITY-MOBILE.md`'s "Workshop" migration-matrix row that
hasn't been built yet at all. Don't mistake this assembly's current
small size for the workshop system being close to done.

## Public API

`PartDefinition`, `InstalledComponent`, `BuildRecipeDefinition` (two
fields — `RequiredTransmissionId`, `RequiredCrankType` — are sourced
content the Swift original explicitly flags as "not yet read or enforced
anywhere," carried over with the same honesty; a third,
`RequiredDifferentialType`, **is now enforced** — see below).

`BuildRecipeEvaluator.SatisfiesTarget(BuildRecipeDefinition,
VehicleDefinition, EngineDefinition)` (new — resolves the "where does
BuildRecipe live" question from `WTRL.RPG/CONTRACT.md`/`Assignments/
OUTPUT-RPG-Design.md`; the satisfaction check lives here since it needs
Vehicle/Engine/Differential types, while `WTRL.RPG.SavedBuildRecipe`
holds only the reward/progression state and references a target
definition by string id). Checks weight-to-power range and, when the
recipe specifies one, a required differential type — the real 35-recipe
corpus only ever uses `"lsd"`/`"lsdRace"`, both mapped to `ClutchLsd`/
`TorqueBiasing`; an unrecognized string fails rather than silently
passing.

`ResolvedVehicleConfiguration`, `VehicleConfigurationResolver.Resolve(
VehicleDefinition, TransmissionDefinition, SuspensionDefinition?,
IReadOnlyList<InstalledComponent>)` — applies a **hardcoded** set of
three part-ID effects (`final-drive-373`, `sport-tire`, `track-damper`).
Real, but exactly as limited as it sounds; nothing has extended this
since the Swift original either.

## Deliberate deviations from the Swift source

1. **No content catalog** (same as `WTRL.Vehicle`/`WTRL.Racing`) —
   `Resolve` takes the base `VehicleDefinition`/`TransmissionDefinition`/
   `SuspensionDefinition` directly instead of looking them up by
   `vehicleID`.
2. **This is what forced `WTRL.Vehicle`'s definition types from plain
   classes to C# `record`s** (see `Vehicle/Definitions.cs`'s updated
   header comment and `Vehicle/CONTRACT.md`). Swift's resolver mutates a
   local `var` copy of a value-type struct; a C# class with `init`-only
   properties can't do that without either mutating a shared instance in
   place (corrupting whatever else references the same canonical object)
   or hand-rolling a copy constructor per type. Records give the same
   "derive a modified copy" shape via `with` for free. **This changed an
   already-shipped, tested assembly — re-verify anything built against
   `WTRL.Vehicle`'s definition types if it assumed reference/class
   semantics.**

## Verification

Same method as `WTRL.Vehicle`/`WTRL.Racing`: a throwaway `dotnet build`/
`dotnet test` project containing all three assemblies' files (subfoldered
this time — a flat copy caused a real filename collision between
`Vehicle/Definitions.cs` and `Garage/Definitions.cs` that silently
overwrote one with the other on the first attempt; worth remembering for
whoever verifies the next assembly this way). **0 errors, 0 warnings**
(after fixing 2 nullable-reference warnings). 5 new tests ported from
`WTRLCoreTests.swift`/`WTRLAdvancedTests.swift`, plus one new test this
port added (`ResolvingDoesNotMutateTheOriginalDefinitions`, guarding the
record/`with` fix above) — all pass. **18/18 combined** with
`WTRL.Vehicle`'s 5 and `WTRL.Racing`'s 8.

## Not yet ported

- `CareerTransaction.swift`/`CareerCommand` (atomic multi-command apply:
  earn/spend/acquirePart/install/completeRace/recordEvidence/
  recordHistory) — this operates on `CareerSave`, a persistence-level
  type, so it belongs in `WTRL.Career`/`WTRL.Persistence` once those
  exist, not here, even though `acquirePart`/`install` are garage
  actions. Keep the atomicity property (`apply` either commits every
  command or none) when it's ported — that's the actual value of that
  function, not the individual command handlers.
- `DynoSimulation.swift` → `WTRL.Lab` (see `PIVOT-PLAN.md`'s migration
  matrix — Workshop/Lab are listed as two separate targets for exactly
  this split).
- The real workshop interaction layer (inspection, part removal/install
  presentation, service evidence) — no `WTRLCore`-layer source exists to
  port for this; it needs fresh design against `racinggame/05-
  specifications/25-GARAGE-DESIGN.md`.

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

## First fully satisfiable build recipe; recognized-parts catalog (2026-09-20)

`SampleContent.Hero1965TrackBuild()` — the project's first
end-to-end-satisfiable `BuildRecipeDefinition` (0 of 35 spec'd recipes
existed as real content before this), sized against the hero-1965
fixture numbers already used throughout `Tests/EditMode`.
`SampleContent.RecognizedParts` — the 3 part ids
`VehicleConfigurationResolver.Resolve` actually does anything with,
now available as real `PartDefinition` content for
`WTRL.UI.GarageScreen` to list rather than a UI screen inventing its
own disconnected part list.

## All 35 canonical build recipes ported (2026-09-20)

Closes "only 1 of 35 spec'd recipes exists as real content" from the
world-content gap audit. New `CanonicalBuildRecipes.All` is a straight
transcription of the real `buildRecipes` array from the original Swift
game's source
(`SwiftRacer/Sources/WTRLCore/Content/CanonicalContent.swift`) -- every
id, trim tier, weight-to-power band, and required-differential/
transmission/crank field is copied, not invented, including that
source's own comments about which bands are directly-sourced real
horsepower/mass figures versus open modeling decisions (e.g. the
mid-70s homologation rung's forced-induction path).

**Honest limitation**: only `hero-1965` (via `HeroContentBuilder`) has
a real `VehicleDefinitionAsset` in this Unity project. The other 6
vehicle ids these recipes reference (`hero-mid70s`, `hero-late80s`,
`hero-mid90s`, `hero-early00s`, `hero-mid10s`, `hero-2022`) have no
built content yet -- porting the recipe DATA doesn't require their
vehicle assets to exist, but a recipe can't be practically satisfied
in-game until its generation's real vehicle content is built. That
remains open, documented follow-on work.

Verified: 4 new tests confirm exactly 35 recipes with unique ids,
exactly 7 generations of exactly 5 recipes each, that
`hero1965-hipo-spec`'s real 271hp/1450kg band is satisfiable end-to-end
through the actual `BuildRecipeEvaluator`, and that the 2022
generation's real Voodoo/Predator mutual-exclusivity (manual-only vs.
automatic-only) transcribed correctly.

## Real batch content importer for the master parts catalog (2026-09-20)

Closes "no batch content importer connects the real JSON catalogs to
the game". New `PartCatalogImporter` deserializes the actual
`racinggame/ImportedVehicleCorpus/Content/Engineering/
master_parts_catalog.json` corpus (schema `wtrl.rev24.master-parts.v1`,
1,560 real entries across 30 families x 9 generations), mirrored into
`Assets/StreamingAssets/Corpus/` so the importer is self-contained and
testable without depending on a sibling repo's file layout.

**Honest limitation, deliberately not hidden**: this catalog is a real
ENGINEERING research taxonomy -- part identity, family, generation,
origin/quality tier, research-completeness status, and real structured
cross-family dependency/consequence rules -- NOT a game-balance
catalog. It has no price, reputation gate, or performance-delta field
for any entry (confirmed by reading real entries directly). So this
importer produces `ResearchPartRecord`s, a faithful transcription of
the real research data, and deliberately does NOT auto-convert them
into gameplay-ready `PartDefinition`s (which require Price/
ReputationRequired/TopSpeedDelta/AccelerationDelta) -- fabricating
those numbers to force 1,560 entries into the gameplay type would be
exactly the kind of invented content this project's discipline
rejects. Balancing real gameplay numbers against this real research
data is a separate, human-judgment-requiring pass.

Found and fixed a real schema mistake while building this: an initial
draft assumed `dependencies`/`consequences` were plain string arrays;
deserializing the ACTUAL file threw immediately, revealing they're
real structured objects (`ruleId`, `requiredFamily`, `threshold`,
`hardRequirement`, etc. -- e.g. "a high-airflow intake requires higher
fuel delivery capacity"). Fixed by modeling `PartDependencyRule`
properly instead of loosening the type to `object`. Also found that
some real entries (the `universal.*` ones) have a deliberately empty
`generationId` -- a genuine "applies to every generation" sentinel,
not a data-quality bug -- and adjusted the verifying test accordingly
rather than silently accepting a wrong assumption.

Verified against the real corpus file: entry/family/generation counts
match the document's own declared header counts exactly (1,560 / 30 /
9), every entry's `family` and non-empty `generationId` resolves to a
real family/generation record, and one specific real entry
(`hero_1967.engineblockbottomend.factory`) round-trips its exact real
field values.

## Batch-converting the research corpus into gameplay parts (2026-09-20)

Closes the half of "no batch importer connects the real JSON catalogs
to the game" that can honestly be done without a human: new
`ResearchPartConverter.ConvertAll` turns every one of the 1,560 real
`ResearchPartRecord`s into a real, priced, named `PartDefinition`.

**Deliberately NOT claimed as balanced game content**: the research
corpus has no price, reputation-gate, or performance-delta data for
any entry -- confirmed by reading real entries directly. `Price`/
`ReputationRequired` come from a deterministic formula over real
categorical fields the corpus DOES have (`Origin`/`Quality`/
`VariantLevel`) -- explicitly flagged as a placeholder pricing curve,
not researched or balance-tested. `TopSpeedDelta`/`AccelerationDelta`
are hardcoded to exactly zero for every converted part -- inventing
nonzero performance effects with no real physics basis would be
exactly the kind of fabrication this project's "flag invented vs.
sourced" discipline exists to prevent. Converted parts are deliberately
NOT wired into `VehicleConfigurationResolver.Resolve`'s switch
statement -- that resolver's entire value is that every recognized id
has a real, intentional mechanical effect; giving 1,560 entries
invented effects would defeat that guarantee. They're real, priced,
browsable inventory that mechanically no-ops when installed, same as
any other currently-unrecognized part id.

`ContentValidator.ValidateAll` now also checks the full converted
catalog: unique ids, positive prices, and (the one thing this
converter promises never to do) exactly zero performance deltas on
every single one of the 1,560 real entries -- confirmed clean by
actually running it, not assumed. 7 new EditMode tests, including one
that converts the entire real corpus and checks every resulting part.
149/149 EditMode tests pass.
