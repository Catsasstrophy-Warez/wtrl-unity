# WTRL.World — contract

Content types ported from `SwiftRacer/Sources/WTRLCore/Content/
Definitions.swift` (`TrackDefinition`, `FacilityDefinition`,
`CountyDefinition`). The streaming grid is a port of the *design*, not
the code, of `SwiftRacer/Sources/RacingGame/Rendering/WorldStreaming.swift`
(a Wave24 RealityKit-specific implementation that was never wired into
anything). Depends on `WTRL.Core` (empty) only.

## Public API

**Content**: `TrackDefinition` (real, physics-affecting
`BankingDegrees`, not cosmetic), `FacilityDefinition`, `CountyDefinition`.

**Streaming**: `WorldCellId` (a grid coordinate), `StableWorldSeed`
(deterministic per-cell seed/sample generation — two cells with the same
coordinates always produce the same seed, so unloading and reloading one
reconstructs identical content), `WorldStreamingGrid` — tracks which
cells should be active around a position and reports load/unload deltas
from the previous update. **Deliberately presentation-agnostic**: it
never touches a `GameObject`, prefab, or Addressable — that's
`WTRL.Runtime`'s job, wrapping this in a `MonoBehaviour` that actually
instantiates/destroys scene content per cell.

## Deliberate deviations from the Swift source

1. **Presentation stripped out entirely.** The Swift source's
   `WorldStreamingController.update(playerPosition:under:)` directly
   manipulated a RealityKit `Entity` scene graph and called a
   `BlackridgeCellFactory` that built literal tree/pole/barrier geometry.
   None of that is portable or wanted here — `WTRL.World` only answers
   "which cells, and what's their seed," never "what does a cell look
   like." This is exactly the split `PIVOT-PLAN.md`'s content-porting map
   called out in advance ("this is genuinely reusable thinking, not
   reusable code").
2. **`StableWorldSeed.Unit`'s known numerical deviation** — see the XML
   doc on that method. Swift's 64-bit `Int` makes
   `Int(truncatingIfNeeded: seed)` a full bit-reinterpretation; C#'s
   32-bit `int` (the natural width for a cell coordinate) makes the
   equivalent cast a real truncation. Different derived value for the
   same input than the Swift original, though still deterministic and
   uniformly distributed. This is a cosmetic prop-scattering utility, not
   physics-critical, so exact cross-language parity wasn't worth widening
   `WorldCellId` to `long` for — flagged, not silently accepted.

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project. **0 errors, 0 warnings** on the first attempt. 4
new tests for `WorldStreamingGrid`/`StableWorldSeed` (determinism, first-
load count for a given radius, unload-on-move, idempotence without
movement) — no Swift-test equivalent existed since the original was
never wired into anything to test. All pass. See `WTRL.Events/CONTRACT.md`
for the full project-wide count.

## Not yet ported / not yet designed

- Hub layout content itself — Blackridge's actual garage/parts-shop/gas-
  station/diner/track placement (`SwiftRacer/Sources/RacingGame/World/
  TrackLayout.swift`/`WorldBuilder.swift`) is real, sourced coordinate
  data that hasn't been brought over yet. This assembly currently has the
  *types* to hold that data (`TrackDefinition`/`FacilityDefinition`/
  `CountyDefinition`) but no populated catalog.
- Track route/checkpoint data for AI line-following (`WTRL.Racing`'s
  `TrackLineDefinition`/`TrackNode` already exist and could be populated
  from the same source once it's ported) — the connective content between
  `WTRL.World`'s track metadata and `WTRL.Racing`'s AI perception isn't
  wired up yet.
- The actual cell-content authoring (Blender-produced trees/poles/
  barriers/buildings) — real work already happening in a separate track
  (`racinggame/BlenderPipeline/`), not this assembly's concern.

## First authored track content (2026-09-20)

`World/SampleContent.FoundryRowCircuit()` — the project's first
authored (non-fixture) `TrackDefinition`. "Foundry Row" is one of the
two Blackridge vertical-slice candidates `PROJECT-MAP-UNITY-MOBILE.md`
names. Paired with `WTRL.Racing.SampleContent.FoundryRowCircuitLine()`
via a duplicated string id, not a shared reference — see
`Career/CONTRACT.md`'s account of why (a real cross-assembly compile
error this pass caught and fixed). `LengthM` is the actual polyline
length of the paired racing line's nodes, cross-checked by
`SampleContentTests.TrackDefinitionLengthMatchesTheActualPolylineLength`.
Node shape/dimensions are placeholders, not derived from a real survey.

## The full documented world, built (2026-09-20)

Closes the "build the whole documented world" request. Added
`DistrictDefinition` (new type — districts/zones were named, real
content in the research corpus but had no corresponding type anywhere
in this port) and real content for every named location
`racinggame/DEEP-CONTENT-CATALOG.md`'s "Complete named locations"
section lists: both counties (Blackridge, San Triana), all 11
districts/zones across them, all 4 named race facilities (Redline
Raceway, Cutback Tri-Oval, Longbow Speedway, Highbank Superspeedway),
plus 2 generic service facilities and a "Blackridge Road Course Park"
grouping facility (that last one's own name is invented — the corpus
never names a specific road-course facility).

Also added `TrackDefinition`s for all 3 of the corpus's "original
road-course principles" (forest elevation, coastal signature-drop,
technical tight) under this pass's own names (Whisperwood Forest
Circuit, Cliffside Coastal Circuit, Ironclad Technical Circuit — not
sourced, flagged the same way the rival-generation names were), plus
the 3 named oval classes and the quarter-mile drag strip. San Triana's
`FacilityIds` is deliberately left empty — the corpus never names a
facility against that county, only its districts.

Every `TrackDefinition.LengthM` is the *actual* measured polyline
length of its paired `WTRL.Racing.SampleContent` racing line (verified
by `SampleContentTests.AllWorldTracksHaveAMatchingRacingLineWithSameIdAndAccurateLength`,
which found and required correcting 6 of 7 initial length estimates
after the real geometry was built), not an independent guess.
