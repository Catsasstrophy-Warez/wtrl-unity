# WTRL → Unity Open-World Pivot Plan

Written 2026-09-19. This is the coordination document for converting Wrench
to Race Legends from the Swift/RealityKit `SwiftRacer` project into a full
3D open-world action/RPG/sim/racing game for mobile, built in Unity, with
Blender for authored art, and development split across three AI
assistants (Claude, ChatGPT, Gemini). Read this before touching any code.

**This is the authoritative architecture document.**
`racinggame/PROJECT-MAP-UNITY-MOBILE.md` is a companion production/
content-pipeline document (Blender authoring pipeline, world/track
production, milestones, performance budgets, validation strategy) — real
and additive, but it must defer to this document on project location,
module names, and architecture decisions. It was reconciled to do so on
2026-09-19 after initially diverging (different project path, different
module names, no reflection of the hub-plus-instanced-events decision).
If you're extending either document, keep them consistent.

## Decisions already made (do not re-litigate without the project owner)

1. **Start clean.** This is a new Unity project (`WTRL-Unity/`, this
   folder), not a resume of any archived revision.
2. **Rev16.1 is reference, not a base.** `archive/unity-revisions/
   WrenchToRaceLegends_Unity6_Rev16.1_FixesReapplied.zip` (under
   `Desktop/racing game/`) is a real, code-complete-but-content-empty
   Unity 6000.0.58f2 project — 12 assemblies, 108 C# files, a
   procedurally-generated test scene, no authored 3D art. Its
   **architecture and validated systems are the reference**: assembly
   boundaries, the mobile tilt/touch input bridge, the event-preflight
   gating logic (reputation/fuel/loadout checks before a race can start),
   and the "Living Rival Career" content generator. None of its actual
   code has been copied in yet — every port must be re-verified against
   this new project's structure, not blindly copied.
3. **World structure: hub world + instanced events.** One persistent,
   streamed hub (Blackridge and its surroundings) that the player free-
   roams, with races/missions/encounters launched as separate instanced
   scenes/subscenes rather than everything living in one seamless open
   world. This matches Rev16.1's own event-preflight model (a race is
   already a gated, entered/exited thing, not a location you just drive
   into) and is far more tractable on mobile hardware than a fully
   seamless open world.
4. **Everything in `SwiftRacer/` gets ported, and also kept as reference.**
   `D:\Claudeprojx\SwiftRacer\` is not deleted or archived-and-forgotten.
   Its content (vehicle catalog, rival AI values, track definitions,
   build recipes) is real, sourced, and already fought for — see "Content
   porting map" below for where each piece goes.
5. **Development is split across three AI assistants**, not three copies
   of the same agent. See "Multi-agent division of labor" — the split is
   by module ownership with an explicit written contract per module,
   because these are three different tools with no shared memory of each
   other's sessions. This document, plus a `CONTRACT.md` per assembly, is
   the only thing they share.

## Why this pivot, briefly

`SwiftRacer` is a real, deterministic, well-tested simulation core with
zero authored 3D content and a RealityKit presentation layer that (per
`SwiftRacer/Documentation/BUILD-READINESS.md`) has never been compiled or
rendered by anything — no Xcode/Swift toolchain has been available in this
project's entire authoring history. Moving to Unity + Blender gets a
mature, artist-friendly content pipeline and a much larger asset/asset-
store ecosystem for a mobile open-world scope, at the cost of re-porting
the simulation core from Swift to C#. Given the RealityKit layer was never
even verified to compile, the Swift-side cost of this pivot is smaller
than it would be for a shipped, verified app.

## New project structure

```
WTRL-Unity/
  Assets/WrenchToRaceLegends/
    Core/       WTRL.Core      — math, deterministic RNG, shared value types. No deps.
    Vehicle/    WTRL.Vehicle   — vehicle definitions + physics step (port of WTRLCore/Simulation). Deps: Core.
    World/      WTRL.World     — hub-world streaming, POI placement, terrain/prop authority. Deps: Core.
    Events/     WTRL.Events    — instanced-event lifecycle (enter/preflight/complete/exit). Deps: Core, Vehicle, World.
    Racing/     WTRL.Racing    — race rules, AI drivers, rival intimidation. Deps: Core, Vehicle, Events.
    Garage/     WTRL.Garage    — parts, installation, build recipes. Deps: Core, Vehicle.
    Lab/        WTRL.Lab       — dyno, diagnostics, validation-lab-style experiments. Deps: Core, Vehicle, Racing, Garage.
    RPG/        WTRL.RPG       — NEW: stats, skills, quests, dialogue, reputation-as-RPG-progression. Deps: Core.
    Career/     WTRL.Career    — economy, progression, ties RPG+Garage+Racing+Events together. Deps: Core, Vehicle, Garage, Racing, Events, RPG.
    Persistence/WTRL.Persistence — save/load. Deps: Core, Vehicle, Garage, Career, RPG, World.
    Runtime/    WTRL.Runtime   — composition root, bootstraps a running game. Deps: everything below it.
    UI/         WTRL.UI        — screens/HUD. Deps: everything.
    Editor/     WTRL.Editor    — editor tooling (content builders, validators). Editor-only platform.
    Tests/EditMode, Tests/PlayMode — Unity Test Framework, mirrors Rev16.1's pattern.
```

All 15 `.asmdef` files already exist with this reference graph (no cycles).
`Packages/manifest.json` adds URP (mobile rendering), Input System (touch/
tilt, matching Rev16.1's approach), Cinemachine (camera), Addressables
(hub-world streaming + instanced-event loading), and Timeline (narrative/
cutscenes) on top of Rev16.1's baseline packages. **Package version numbers
are best-effort** — nobody has opened this in an actual Unity Editor yet in
this environment (none is installed here); the first person who does should
let Package Manager resolve/adjust them and commit whatever it produces.

## Content porting map (SwiftRacer → Unity)

| SwiftRacer source | Unity destination | Notes |
|---|---|---|
| `WTRLCore/Simulation/VehicleSimulation.swift`, `DynamicsSubsystems.swift`, `PowertrainSolver.swift`, `EngineSolver.swift` | `WTRL.Vehicle` | The 120 Hz fixed-step deterministic vehicle model — wheels, suspension, tires, engine/clutch/shift, differentials, banking assist. This is the highest-value, most load-bearing port. Port the math, not the Swift idioms; re-derive tests from `WTRLCoreTests.swift`/`WTRLAdvancedTests.swift` in the new NUnit test assemblies rather than transliterating them. |
| `WTRLCore/Content/CanonicalContent.swift`, `Definitions.swift` | `WTRL.Vehicle` (definitions) + `WTRL.World` (tracks/facilities as hub-world locations) | 7 hero generations, 6 rivals, tracks (including the 3 real banked ovals — `SwiftRacer/Sources/RacingGame/World/WorldBuilder.swift`'s `makeBankedOvalTrack` has the actual banking-angle math worth porting to a Blender-authored or procedural Unity mesh), build recipes, race definitions. |
| `WTRLCore/Content/EnrichedRev36VehicleCatalog.swift` (186 entries) + `racinggame/10-vehicle-research-library/` (105/159 dossiers) | `WTRL.Vehicle` content data + reference | Real-world research data for calibration reference, not necessarily 1:1 game content — see `racinggame/ImportedVehicleCorpus/REFERENCE-BOUNDARY.md`, which independently arrived at "186 catalog entries" under a "Rev36/Rev38" Swift lineage. That corpus and this catalog likely share an origin; reconcile them once, not twice. |
| `WTRLCore/Simulation/RivalIntimidation.swift`, `TrackAI.swift` | `WTRL.Racing` | Real, sourced per-rival AI parameters and the fixed determinism approach (seed from simulation tick, never a live RNG) — port the *values* exactly, they're cited to `52-CONTENT-RESOLUTION-PASS-1.md`. |
| `WTRLCore/Runtime/GoldenVerticalSlice.swift`, `WorldConsequences.swift` (`RivalMemory`), `RacingGame/GameState.swift`'s `RivalBehaviorRuntime` wiring | `WTRL.Career` + `WTRL.Racing` | Persistent rival history driving intimidation — port the "derive, never separately persist" discipline, it prevented a real bug class in `SwiftRacer`. |
| `WTRLCore/Content/ValidationLaboratory*.swift`, `EvidenceTopology*.swift` | `WTRL.Lab` | The evidence→proposal→materialize→validate→adopt pipeline. Real and tested in Swift but never actually adopted a vehicle — same caveat applies here; port the pipeline, don't expect it to be wired to anything on day one. |
| `RacingGame/World/WorldBuilder.swift`, `TrackLayout.swift` | `WTRL.World` | Checkpoint/route data and the hub layout (garage, parts shop, gas station, diner, Forestcrest, 3 ovals) — this is your hub-world's actual content list. Reproduce as a Blender-authored scene, not procedural primitives, this time. |
| `RacingGame/Rendering/*` (Wave24 additions: `WorldStreamingController`, `RepeatedWorldGeometry`, `AtlasMaterialLibrary`, LOD/impostor components) | `WTRL.World` | These were RealityKit-specific and never wired in, but the *design* (160 m streaming cells, stable per-cell seeds, atlas materials, impostor distance policy) ports directly to Addressables-based Unity scene streaming — this is genuinely reusable thinking, not reusable code. |
| Nothing yet exists for RPG systems | `WTRL.RPG` | New. This is the one module with no SwiftRacer source — build it from `racinggame/05-specifications/48-RPG-SYSTEMS-SPEC.md` and Rev16.1's `Assets/WrenchToRaceLegends/Prototype~/RPG/` (excluded-from-build reference code, per `WHERE-WE-ARE.md` — read it, don't compile it as-is). |
| `SwiftRacer/Documentation/ArtDirection/` (Wave25 Visual DNA spec) + `racinggame/Art/`, `ArtSource/`, `11-vehicle-concept-art/` | Blender source + Unity import | The art-direction contract (naming, LOD/wheel-node requirements, provenance/rights rules) is engine-agnostic — keep using it verbatim for Blender exports. |

## Rev16.1 patterns worth deliberately reusing

- The `Tools > Wrench to Race Legends > ... > Rebuild + Validate` editor
  menu pattern for content generation/validation — adapt for hub-world
  and instanced-event content builders.
- Event preflight gating (reputation/chapter/lineage/homologation/fuel/
  loadout checks before `StartRace`) — this is exactly the instanced-
  event entry contract this pivot needs.
- The structural validator script pattern (`Scripts/validate_rev16_
  structure.sh`: GUID/meta integrity, duplicate GUIDs, assembly cycles,
  TODO/FIXME sweep, declared-test counts) — write an equivalent for this
  project immediately, since it's the one validation category that
  doesn't need a Unity Editor to run.
- `PlayerVehicleInput.controlScheme` (Auto = tilt+touch) and
  `MobileTouchInputBridge` — the mobile input contract already exists and
  was already being validated on-device; don't redesign it.

## Multi-agent division of labor

**Update, 2026-09-19: ChatGPT and Gemini turned out not to be available
for this project.** Both first assignments (`ChatGPT-Rev16.1-Audit-
Brief.md`, `Gemini-RPG-Spec-Brief.md`) were completed by Claude instead
— see `Assignments/OUTPUT-Rev16.1-Audit.md` and `Assignments/OUTPUT-RPG-
Design.md`, each opening with a note saying so. **This project is
currently being built solo, not across three assistants.** The module-
ownership split below is kept as written in case that changes later
(the briefs and `CONTRACT.md` convention are exactly what a second
assistant would need to onboard), but nothing below should be read as
"in progress elsewhere" — if it's not shipped in this repo, it hasn't
been done by anyone.

Claude, ChatGPT and Gemini have no shared memory of each other's
sessions. The only things they'd share, if this becomes multi-assistant
again, are: this repository (via git — see below), this document, and a
`CONTRACT.md` file inside each assembly folder once one exists. **Every
module owner must treat its own `.asmdef`'s public API as a contract
other modules depend on — changing a public signature without updating
`CONTRACT.md` is the failure mode that will actually break this
arrangement.**

Suggested split (adjust freely, but keep ownership boundaries at assembly
edges, not inside one):

- **Claude**: `WTRL.Core`, `WTRL.Vehicle`, `WTRL.Racing`, `WTRL.Lab` — the
  simulation core and its direct consumers, since this is the most
  directly portable from `SwiftRacer` and benefits from the same
  file-by-file verification discipline already established this session.
- **ChatGPT**: `WTRL.World`, `WTRL.Events`, hub-world Blender pipeline
  coordination — spatial/streaming systems and the instanced-event
  lifecycle.
- **Gemini**: `WTRL.RPG`, `WTRL.Career`, `WTRL.UI` — the new RPG layer and
  the player-facing progression/UI systems that consume everything else.

`WTRL.Garage`, `WTRL.Persistence`, `WTRL.Runtime`, `WTRL.Editor` are
integration points — whoever touches them last before a merge should post
a short note in this file's changelog (add one below) saying what
changed and why, since they're the seams where the other three assistants'
work actually meets.

**Use git.** This folder is a real git repository with a public remote:
**https://github.com/Catsasstrophy-Warez/wtrl-unity** (branch `main`).
The `racinggame/` research corpus is now a separate repo too:
**https://github.com/Catsasstrophy-Warez/wtrl-racinggame**. Both were
public by the project owner's explicit choice. **ChatGPT and Gemini can
now actually clone/pull these** — their assignments are no longer
limited to document-only work for lack of somewhere to push code to.
Update each assignment brief (or just tell them directly) to clone the
relevant repo instead of working from pasted context alone.

**Meta-file GUID trap**: the first person to open this project in an
actual Unity Editor generates every `.meta` file's GUIDs. Whoever does
this first must commit those `.meta` files *before* anyone else opens
their own copy of the project — if two people independently generate
GUIDs for the same assets, Unity will treat them as different objects and
every cross-reference (prefab slots, asmdef references by GUID, scene
object links) silently breaks on merge. This is a real, well-known Unity
multi-person-project failure mode, not a hypothetical.

## First assignments (do these before any module code)

Starting to write module code in parallel before anyone has actually read
the two things that should shape `WTRL.RPG` and the world/event layer
would mean redoing work once that reading happens anyway. So the first
round is reading and reporting, not coding:

1. **ChatGPT — audit Rev16.1.** Full brief:
   `Assignments/ChatGPT-Rev16.1-Audit-Brief.md`. Output: a written audit
   report (the brief specifies where). This directly informs `WTRL.World`/
   `WTRL.Events`, ChatGPT's eventual module ownership above, so the same
   assistant does both.
2. **Gemini — read the RPG spec.** Full brief:
   `Assignments/Gemini-RPG-Spec-Brief.md`. Output: an `WTRL.RPG` design
   doc (the brief specifies where). Directly informs `WTRL.RPG`/
   `WTRL.Career`, Gemini's eventual ownership above.
3. **Claude (this session)** — while those two run, the parallel task is
   writing `CONTRACT.md` for `WTRL.Core` and `WTRL.Vehicle` and starting
   the vehicle-simulation port, so there's a real API for either other
   assistant to build against once their reading pass is done.

Both briefs are self-contained — each assistant has no memory of this
conversation, so each brief restates the project context it needs rather
than assuming it.

## Immediate next actions

1. ~~Turn `WTRL-Unity/` into a git repository~~ **Done**, ~~push to a
   remote~~ **Done** —
   https://github.com/Catsasstrophy-Warez/wtrl-unity (public, `main`).
2. Open the project once in an actual Unity 6000.0.58f2 Editor to let it
   generate `.meta` files and resolve the package manifest — nothing in
   this project has been opened by any Editor yet, matching this
   session's long-standing "verified by reading, not by compiling"
   caveat carrying over from `SwiftRacer`. **Commit the resulting `.meta`
   files immediately** — see the GUID trap above.
3. Run the two first assignments above.
4. ~~Port `WTRL.Vehicle`'s core step function~~ **Done** — see
   `Assets/WrenchToRaceLegends/Vehicle/CONTRACT.md` for the full API,
   deliberate deviations from the Swift source, and how it was actually
   verified (real `dotnet build`/`dotnet test`, not just reading — see
   that file for why this was possible when nothing else in this project
   has been). Remaining vehicle-adjacent work: `WTRL.Racing` (rival AI,
   depends on this), the content-catalog architecture question CONTRACT.md
   flags under "deliberate deviations" #1, and re-running the same 5 tests
   inside an actual Unity Editor once one opens this project.
5. ~~Write `CONTRACT.md` for `WTRL.Core` and `WTRL.Vehicle`~~ **Done.**
   `WTRL.Core`'s is short because the assembly is still empty — nothing
   in the `WTRL.Vehicle` port needed anything from it.

## Assembly decision index

One line per assembly's `CONTRACT.md`, so "what did we decide and why"
doesn't require opening eleven files to answer. Full reasoning always
lives in the linked file — this is a pointer, not a replacement.

| Assembly | CONTRACT.md | Key decision / deviation |
|---|---|---|
| Core | `Core/CONTRACT.md` | One infra file: `IsExternalInit` polyfill needed by every `init`/`record`-using assembly to compile under Unity's .NET Standard 2.1 profile. No game-logic types yet. |
| Vehicle | `Vehicle/CONTRACT.md` | No content catalog: every definition is a required caller-supplied parameter. Definitions are C# `record`s (for `with`-copying). |
| Racing | `Racing/CONTRACT.md` | `RivalBehaviorRuntime` is a mutable class (deviation from Swift's struct) so its memory/intimidation state can be shared by reference across a session. `AiVehicleSession` now drives real `VehicleSimulation.Step` physics under AI control end to end. |
| Garage | `Garage/CONTRACT.md` | `VehicleConfigurationResolver` uses `with`-expressions on Vehicle's records; first place the record conversion paid off. |
| Lab | `Lab/CONTRACT.md` | `RuntimeTelemetryRing` is a fixed-capacity circular buffer, not an unbounded list — bounds memory for long play sessions. |
| RPG | `RPG/CONTRACT.md` | New layer, no Swift source. `BuildRecipeProgress` references a Garage `BuildRecipeDefinition` by string id only, to avoid an RPG→Garage type dependency (see Career/Garage/RPG three-way tradeoff below). |
| BuildRecipe (Garage/RPG boundary) | `Garage/CONTRACT.md`, `RPG/CONTRACT.md` | Satisfaction-check logic lives in Garage (needs Vehicle/Engine types); the progression/reward wrapper lives in RPG (string-id reference only) — resolved 3-way tradeoff, not a default. |
| World | `World/CONTRACT.md` | `StableWorldSeed` has a documented 32-bit-vs-64-bit-Int numerical deviation from Swift's original. |
| Events | `Events/CONTRACT.md` | Real 4-phase `RaceRuntimeState`, not Rev16.1's 8-state shape — the 8-state wrapper is flagged as future work once scene-loading exists, not implemented speculatively. |
| Career | `Career/CONTRACT.md` | `CareerState.Clone()`/`CopyFrom()` must both list every field or transactions silently reset unlisted fields to default — caught once already; documented as a standing warning for the next field added. Now also: `CareerCommand.RecordRaceOutcome` closes the long-flagged race-completion → RPG/Racing wiring gap; caught and fixed a real atomicity bug (RPG/Racing state was reference-copied, not deep-cloned) while building it. `RaceSession` wires `RaceRules` to it. |
| Persistence | `Persistence/CONTRACT.md` | Added a real (mechanical, not architectural) `WTRL.Racing` asmdef dependency. `runEvidence`/`dynoRuns`/`ghostReplays` deliberately not in the save DTO yet (flagged, not silently dropped). |
| Runtime | `Runtime/CONTRACT.md` | `new FixedStepClock()` silently zero-inits instead of calling its `hz=120` constructor (struct-specific C# gotcha) — caught by `dotnet test` hanging, not by the compiler or reading. First bug this whole pass caught only by running tests. |
| Content | `Content/CONTRACT.md` | Answers "who owns content resolution": `ScriptableObject` wrappers with direct object references, still no lookup-by-id anywhere. **Verified**: compiles clean and has real generated `.asset` instances (hero-1965). Found and fixed a real Unity bug where only the first ScriptableObject type per file gets a correct script reference — every type now lives in its own file. |
| UI | `UI/CONTRACT.md` | First `MonoBehaviour` (`VehicleRuntimeController`) — smallest possible vertical slice (Content asset → Runtime → Transform). **Verified end-to-end** via 4 passing PlayMode tests run through a real Unity Play session. Found a real headless-batchmode limitation (simulated keyboard input doesn't survive a frame) — worked around with an `internal Tick()` test seam, not a game-code bug fix. |
| Editor | `Editor/CONTRACT.md` | Was an empty stub; now has two content builders: `HeroContentBuilder` (the vehicle bug above was found while building this) and `MarshContentBuilder` (the rival roster's first real content asset). |

## Changelog

- 2026-09-19: Initial pivot plan and project skeleton created (Claude).
- 2026-09-19: Added first-assignment briefs for ChatGPT (Rev16.1 audit)
  and Gemini (RPG spec read); noted the git-remote and meta-GUID gaps
  found on review (Claude).
- 2026-09-19: Ported `WTRL.Vehicle` from `SwiftRacer`'s
  `VehicleSimulation`/`DynamicsSubsystems`/`PowertrainSolver`/
  `Definitions`/`AdvancedDefinitions` — the full 120 Hz deterministic
  vehicle model. Verified with a real compiler for the first time in
  this project's history (a throwaway `dotnet build`/`dotnet test`
  project, since these files are plain C# with no `UnityEngine`
  dependency): 0 errors, 0 warnings, 5/5 ported tests passing — one of
  which failed on first attempt and caught a genuine mistranslation
  (see `Vehicle/CONTRACT.md`). Removed the Swift original's dependency
  on a global content catalog reached into from inside the physics step
  (`tire`/`suspension` are now required parameters) — a deliberate
  architecture change, documented in `Vehicle/CONTRACT.md` (Claude).
- 2026-09-19: Reconciled `racinggame/PROJECT-MAP-UNITY-MOBILE.md` against
  this document — it had been written independently (by another
  assistant, not asked to) with a conflicting project location and
  module names. Fixed to match this document; its genuinely additive
  content (Blender pipeline, milestones, performance budgets, validation
  strategy) was preserved. Added the cross-reference above so this
  doesn't happen silently again (Claude).
- 2026-09-19: Pushed both repos to public GitHub remotes (project
  owner's choice — public is fine): `wtrl-unity` and `wtrl-racinggame`,
  both under the `Catsasstrophy-Warez` account. ChatGPT and Gemini can
  now clone/pull instead of working from pasted context only (Claude).
- 2026-09-19: Ported `WTRL.Racing` from `SwiftRacer`'s `TrackAI`/
  `RivalIntimidation`/`DriverController`/`RivalBehaviorRuntime` — AI line-
  following, the real sourced per-rival intimidation ceilings (6 rivals,
  every value traced to the research corpus), and the memory→intimidation
  derivation discipline. Verified the same way as `WTRL.Vehicle`: a
  throwaway `dotnet build`/`dotnet test` project (0 errors, 0 warnings,
  8/8 new tests passing, 13/13 combined with `WTRL.Vehicle`'s). Also
  committed and pushed in-progress `BlenderPipeline/` generation work in
  the `racinggame` repo that had accumulated uncommitted since its remote
  didn't exist yet (Claude).
- 2026-09-19: Ported `WTRL.Garage` from `SwiftRacer`'s
  `VehicleConfigurationResolver`/`Definitions.swift` (PartDefinition,
  InstalledComponent, BuildRecipeDefinition). This forced a real change
  to the already-shipped `WTRL.Vehicle`: its six definition types are now
  C# `record`s instead of plain classes, so the resolver can derive a
  modified copy via `with` instead of mutating a shared instance in place
  (documented in both assemblies' `CONTRACT.md`). Verified the same way
  as the prior two ports: 0 errors, 0 warnings (after 2 nullable fixes),
  6 new tests (5 ported + 1 new, guarding the record/`with` behavior),
  18/18 passing combined with `WTRL.Vehicle`/`WTRL.Racing`. Also caught
  and documented a real filename collision in the throwaway verification
  project itself (`Vehicle/Definitions.cs` vs `Garage/Definitions.cs`
  silently overwriting each other in a flat copy) for whoever verifies
  the next assembly this way (Claude).
- 2026-09-19: Ported `WTRL.Lab` from `SwiftRacer`'s `DynoSimulation`,
  `RunEvidence.swift` (VehicleConfigurationFingerprint, TestRunEvidence,
  RunComparison), and `Telemetry.swift` (RuntimeTelemetryRing). Same
  no-catalog deviation as every prior assembly. Verified: 0 errors, 0
  warnings (after 2 nullable fixes), 6 new tests (2 ported + 4 new,
  covering fingerprint order-independence and telemetry-ring wraparound
  that had no direct Swift-test equivalent to port), 24/24 passing
  combined with Vehicle/Racing/Garage. `WTRL.Vehicle`, `WTRL.Racing`,
  `WTRL.Garage`, `WTRL.Lab` are now all ported and verified — the next
  unblocked pieces (`World`, `Events`, `Career`, `RPG`) either need
  fresh design (no direct Swift source) or are waiting on ChatGPT's/
  Gemini's assignments, still with no OUTPUT files as of this entry
  (Claude).
- 2026-09-19: ChatGPT and Gemini confirmed unavailable — both first
  assignments completed by Claude instead (`Assignments/OUTPUT-
  Rev16.1-Audit.md`, `Assignments/OUTPUT-RPG-Design.md`). Updated the
  "Multi-agent division of labor" section to say so plainly: this
  project is being built solo for now. Also shipped `WTRL.RPG`'s
  well-specified parts (`ReputationState` with diminishing-returns
  rival wins, `SafetyRatingState`+`DriverLicenseState` with the
  professional-tier knockout-entry gate, `ClassBracket`) straight from
  the RPG design doc's own recommendations — verified 0 errors/0
  warnings (one real enum-comparison bug caught and fixed), 12/12 new
  tests passing, each citing a specific spec section. `BuildRecipe`
  deliberately deferred — it needs a real architecture decision
  (does WTRL.RPG depend on WTRL.Garage?) laid out as an open question
  in the design doc, not resolved unilaterally. `WTRL.Vehicle`,
  `WTRL.Racing`, `WTRL.Garage`, `WTRL.Lab`, and now `WTRL.RPG` are
  ported/designed and verified (Claude).
- 2026-09-19: Resolved the `BuildRecipe` open architecture question from
  `RPG/CONTRACT.md` (per your instruction to resolve it myself and
  finish RPG): went with option 3 from `OUTPUT-RPG-Design.md`. The
  satisfaction check (`BuildRecipeEvaluator.SatisfiesTarget`) lives in
  `WTRL.Garage` since it needs Vehicle/Engine/Differential types; the
  player-progression wrapper (`SavedBuildRecipe`) lives in `WTRL.RPG`
  and references its target by string id only, so `WTRL.RPG` still has
  no dependency on `WTRL.Garage`. This also newly enforces
  `BuildRecipeDefinition.RequiredDifferentialType`, previously flagged
  as sourced content nothing read anywhere — checked against the real
  35-recipe corpus (`"lsd"`/`"lsdRace"` are the only values ever used).
  7 new tests, 43/43 passing project-wide across all five real
  assemblies (`Vehicle`, `Racing`, `Garage`, `Lab`, `RPG`). `WTRL.RPG`
  is now feature-complete against everything `48-RPG-SYSTEMS-SPEC.md`
  specifies, modulo the two open questions in `OUTPUT-RPG-Design.md`
  about systems that aren't in that spec at all (Claude).
- 2026-09-19: Ported `WTRL.World` and `WTRL.Events`. `WTRL.Events` is the
  real, working race-flow implementation from `SwiftRacer`'s
  `RaceRuntime.swift` (`RaceRuntimeState`/`RaceRules`,
  `DragRaceRuntimeState`/`DragRaceRules`) — explicitly the correct
  reference this document already pointed to, distinct from the stale
  `RaceDisciplineRuntime` lineage rejected across several uploads.
  `WTRL.World` ports `TrackDefinition`/`FacilityDefinition`/
  `CountyDefinition` plus the DESIGN (not the RealityKit code) of
  Wave24's cell-based world streaming — a presentation-agnostic
  `WorldStreamingGrid` that reports load/unload deltas without touching
  any scene object, deliberately leaving actual content instantiation to
  `WTRL.Runtime`. Verified: 0 errors, 0 warnings, 11 new tests (6 ported
  term-for-term from `WTRLCoreTests`/`WTRLAdvancedTests` + 5 new, no
  Swift-test equivalent existed for the never-wired-in streaming grid).
  **54/54 passing project-wide across all seven real assemblies now
  shipped**: Vehicle, Racing, Garage, Lab, RPG, World, Events. Applied
  the Rev16.1 audit's race-flow recommendation as a deferred note rather
  than acting on it now (layering its 8-state shape around this 4-state
  core needs WTRL.Runtime's scene-loading/results-flow to exist first —
  documented in Events/CONTRACT.md, not implemented speculatively)
  (Claude).
- 2026-09-19: Ported `WTRL.Career`'s `CareerTransaction`/`CareerCommand`
  from `SwiftRacer`'s `CareerTransaction.swift` (atomic batch apply —
  clone, mutate, commit only on full success), plus new `CareerState`
  (the runtime model tying together Garage/Racing/Events/RPG state) and
  ported `VehicleHistoryEvent`. Deliberately left `.recordEvidence`
  unported — needs `WTRL.Lab.TestRunEvidence`, a dependency `WTRL.Career`
  doesn't have; flagged as an open question rather than resolved
  unilaterally (same shape as the `BuildRecipe` question, see Career/
  CONTRACT.md). Verified: 0 errors, 0 warnings, 9 new tests (1 ported +
  8 new), 63/63 passing project-wide across all eight real assemblies
  now shipped (Vehicle, Racing, Garage, Lab, RPG, World, Events,
  Career). Moving on to WTRL.Persistence next per instruction (Claude).
- 2026-09-19: Ported `WTRL.Persistence` from `SwiftRacer`'s
  `CareerSave.swift`/`CareerSaveCodec` — the lenient-decode DTO shape
  (every field optional with a default, hero-gen1->hero-1965 rename,
  selected-vehicle-must-be-owned invariant), using System.Text.Json.
  Added a real (mechanical, not architectural) asmdef dependency:
  `WTRL.Racing`, needed because `CareerState.RivalBehavior` exposes
  `RivalMemory` (a `WTRL.Racing` type) that this assembly must name
  directly to serialize. Caught and fixed a real near-bug along the
  way: adding save-relevant fields to `CareerState` without updating
  `Clone()`/`CopyFrom()` would have meant every `CareerTransaction
  .Apply` silently reset the player's selected vehicle and dyno slider
  values — fixed before any test was written against it, documented in
  both files. Deferred `runEvidence`/`dynoRuns`/`ghostReplays` (need
  `WTRL.Lab` or a not-yet-ported replay type) for the same reason
  `WTRL.Career` deferred `.recordEvidence`. Verified: 0 errors, 0
  warnings, 7 new tests (round-trips, the rename migration, the
  ownership invariant, garbage-JSON resilience) with no Swift-test
  equivalent to port. **70/70 passing project-wide across all nine
  real assemblies now shipped**: Vehicle, Racing, Garage, Lab, RPG,
  World, Events, Career, Persistence. Moving on to WTRL.Runtime next
  per instruction (Claude).
- 2026-09-19: Ported `WTRL.Runtime` from `SwiftRacer`'s
  `WTRLRuntime.swift`/`PresentationState.swift`/`SamplingPolicy.swift`
  — the composition root tying `VehicleSimulation.Step` to a
  `FixedStepClock`, deriving presentation/audio state, and recording
  rate-gated telemetry. This is the tenth and final assembly in this
  porting pass and completes the explicit "Career, then Persistence,
  then Runtime" sequence. Same no-catalog deviation as every prior
  assembly: `Advance` takes fully-resolved definitions every call
  rather than resolving them from a global catalog that doesn't exist.
  Caught and corrected one design inconsistency via self-review before
  compiling (an early draft made `suspension` nullable with a
  fallback default, contradicting `VehicleSimulation.Step`'s own
  established discipline). **Also caught a real, previously-unnoticed
  bug via `dotnet test` itself** (not self-review, not the compiler):
  `new FixedStepClock()` doesn't call the constructor overload with a
  default `hz` parameter for a struct — it silently zero-initializes,
  giving `Step == 0` and turning `Consume` into an infinite loop. This
  compiled clean with 0 warnings and only surfaced as `dotnet test`
  hanging with `testhost` burning CPU; fixed by constructing with the
  rate spelled out explicitly everywhere. Documented at length in
  Runtime/CONTRACT.md as the first bug this whole pass that neither
  the compiler nor self-review caught. Verified: 0 errors, 0 warnings
  on build; 6 new tests (a corrected fixed-step-determinism check now
  driven through the real public API, four-corner telemetry evidence,
  telemetry rate-gating, reset/shift-mode/diagnostics coverage).
  **76/76 passing project-wide across all ten real assemblies now
  shipped**: Vehicle, Racing, Garage, Lab, RPG, World, Events, Career,
  Persistence, Runtime. This completes every assembly named in
  PIVOT-PLAN's original content-porting map; next steps are Unity-
  Editor-side integration (MonoBehaviour wrappers, scene setup) which
  cannot be verified further via throwaway `dotnet` projects alone
  (Claude).
- 2026-09-20: Full-project analysis and recommendations pass. Attempted
  the top recommendation (open the project in a real Unity Editor for
  the first time) via `Unity.exe -batchmode -nographics -quit`: it hung
  indefinitely at `[Licensing::Module] Licensing is not yet
  initialized.`, waiting for an interactive license activation that
  requires the user's own sign-in. Killed the process rather than leave
  it running — **this remains blocked on the user activating a Unity
  license themselves**, and is still the single highest-priority next
  action once that's done. Corrected an error from the prior day's
  analysis: `racinggame`'s two ~85MB `.zip` files were already
  `.gitignore`d and never committed — no repo-bloat cleanup was
  actually needed there.
  Made progress on everything not gated by a licensed Editor: added
  `WTRL.Content` (new assembly — `ScriptableObject` wrappers around
  `WTRL.Vehicle`'s definitions, answering the long-flagged "who owns
  content resolution" question without reintroducing a lookup-by-id
  catalog) and gave `WTRL.UI` its first real code
  (`VehicleRuntimeController`, a `MonoBehaviour` wiring a Content asset
  through `WTRLRuntime.Advance` to a `Transform` — the smallest
  possible end-to-end vertical slice). **Both are explicitly
  UNVERIFIED** — they reference `UnityEngine`, so this project's usual
  `dotnet build`/`dotnet test` verification method does not apply, and
  no licensed Editor was available to compile them for real. Said so
  plainly in both new `CONTRACT.md` files rather than presenting them
  as verified work; deliberately did not hand-author any `.unity`
  scene or ScriptableObject `.asset` file, since a malformed one would
  be a worse outcome than no scene at all — those need the Editor's
  own Create menu. Added the "Assembly decision index" table above so
  the now-thirteen `CONTRACT.md` files have a single one-line-per-
  assembly pointer instead of requiring a full read of each to answer
  "what did we decide and why" (Claude).
- 2026-09-20: **The Unity license blocker is resolved** — the user
  activated a Unity Personal license, which unblocked the batchmode
  open that had previously hung indefinitely. This is a real milestone:
  the first time in this entire project's history (spanning both the
  Swift/RealityKit era, which was never compiled by anything, and this
  Unity pivot) that any of this code has been opened and compiled by
  the actual engine it's meant to run in. The project was retargeted
  from its original pin (6000.0.58f2, not installed in this
  environment) to 6000.6.0f1, the closest available install.
  Two real, project-wide compile errors surfaced, both invisible to
  every prior `dotnet build`/`dotnet test` verification pass because
  neither depends on `UnityEngine`: (1) missing `IsExternalInit` (every
  `init`/`record` in the project needs it under Unity's .NET Standard
  2.1 profile — fixed once, project-wide, via a new `WTRL.Core/
  IsExternalInitPolyfill.cs`), and (2) the C# 11 `required` keyword not
  being available under Unity's default language version. A per-
  assembly `.rsp` language-version override (Unity's documented
  mechanism) was tried first for (2) but did not take effect in a real
  compile for reasons not fully diagnosed even after a full `Library/
  Bee` cache wipe; rather than layer a second speculative workaround on
  top of an already-unverified one, every `required` usage in `Career`,
  `Garage`, `Lab`, and `RPG` was converted to the constructor-required-
  plus-init-optional pattern `Vehicle/Definitions.cs` already
  established, and a stray raw-string-literal test fixture (also a C#
  11 feature) and an NUnit `Has.Exactly(n).Items` constraint not
  supported by the bundled Test Framework version were fixed alongside.
  Re-ran the throwaway `dotnet test` suite after these edits to confirm
  no behavioral regressions (still 76/76 passing) before re-attempting
  the Editor compile.
  **Result: all 15 gameplay assemblies (plus the still-empty `WTRL.
  Editor`) now compile with zero errors in a real, licensed Unity
  Editor**, and every `.meta` file this project needed was generated
  for the first time. `WTRL.Content` and `WTRL.UI` (added in the
  previous entry, previously marked UNVERIFIED since they reference
  `UnityEngine` and can't go through the `dotnet` verification path)
  are now confirmed to compile too — see their updated `CONTRACT.md`s.
  **Still not verified**: nobody has pressed Play yet. Compiling proves
  the code is well-formed; it doesn't prove `VehicleRuntimeController`
  actually moves anything at runtime. See `UI/CONTRACT.md`'s "Manual
  steps still required" for the next, human-in-the-Editor step (Claude).
- 2026-09-20: Closed the biggest content gap from the vehicle-asset
  audit: added Blender blockout assets for all 28 rival vehicle
  generations (Marsh 7, Reyes 5, Vogel 5, Kade 4, Osei 4, Duquesne 3) —
  previously zero, despite being the actual opponent roster. Own
  scaffold, not sourced canon (flagged in `racinggame/BlenderPipeline/
  export/rival_blockouts_pass1/README.md`); needs a project-owner pass
  for real names and researched dimensions.
  Then, working through the outstanding "press Play" verification: gave
  `WTRL.Editor` its first real content (`HeroContentBuilder`, generating
  real hero-1965 `.asset` instances programmatically) and `WTRL.Content`
  a `SurfaceDefinitionAsset` (the one definition type that didn't have
  one). Building the hero assets surfaced **a real, reproducible Unity
  Editor bug**: only the first `ScriptableObject` type declared in a
  `.cs` file gets a correct serialized script reference via
  `AssetDatabase.CreateAsset` in this batchmode environment — every
  subsequent type in the same file silently gets a broken one
  (`m_Script: {fileID: 0}`). Confirmed via isolation (a fresh asset at a
  never-used path reproduced it too, ruling out caching). Fixed by
  splitting every `WTRL.Content` type into its own file. Documented at
  length in `Content/CONTRACT.md` and `Editor/CONTRACT.md`.
  Replaced `VehicleRuntimeController`'s `[SerializeField] private`
  vehicle field with a public one (testability, same Inspector
  behavior) and wrote `WTRL.Tests.PlayMode.VehicleRuntimeControllerTests`
  — the project's first PlayMode tests, run through a real
  `Unity.exe -runTests -testPlatform PlayMode` session (not the
  throwaway `dotnet test` method every non-Unity assembly uses, since
  this one needs the actual engine). Found and fixed two more real
  issues along the way, both in test code rather than game code: (1) a
  test ordering bug (`AddComponent` invokes `Awake()` synchronously,
  before a same-line field assignment can happen — fixed by creating
  GameObjects inactive first); (2) a genuine environment limitation,
  not a bug — simulating a held keyboard key via `InputSystem
  .QueueStateEvent` doesn't survive one `FixedUpdate` in headless
  `-nographics` batchmode, confirmed via diagnostic logging across
  several iterations. Worked around by extracting `VehicleRuntimeController
  .FixedUpdate`'s simulate-and-apply step into an `internal Tick(VehicleInput)`
  method (`InternalsVisibleTo`-exposed to the test assembly) so the
  real Content→Runtime→Transform chain is still exercised end-to-end,
  without depending on unreliable simulated hardware input. Documented
  the limitation in a dedicated test rather than hiding it.
  **Result: 4/4 new PlayMode tests passing, plus all 77 EditMode tests
  now also independently re-confirmed passing through the real Unity
  Test Runner** (previously only ever confirmed via the throwaway
  `dotnet test` method). This is the strongest verification state this
  project has had at any point in its history — real compiled code,
  real generated content, real Play-session-level test coverage, all
  inside the actual target engine. Structural validator: 16 assemblies,
  49 C# files, no reference cycles (Claude).
- 2026-09-20: Closed three more long-flagged gaps in one pass — a real
  first circuit, race-session orchestration, and race-completion wiring
  into Career/RPG/Racing.
  Added `World/SampleContent.FoundryRowCircuit()` and `Racing/
  SampleContent.FoundryRowCircuitLine()`, the project's first authored
  (non-fixture) track content, sharing a duplicated string id rather
  than a reference since `WTRL.Racing` has no `WTRL.World` dependency.
  Added `Career/RaceSession.cs`, wiring `WTRL.Events.RaceRules`/
  `RaceRuntimeState` (countdown → running → lap/sector progression →
  finished) to a new `CareerCommand.RecordRaceOutcome`.
  That command is the actual fix for the gap every `CareerTransaction`/
  `CareerState` note has flagged since Career was first ported: nothing
  called `ReputationState.RecordEvent`/`SafetyRatingState.RecordEvent`/
  `RivalBehaviorRuntime.RecordResult` on race completion, because no
  structured result data existed. `RaceOutcomeDetail` is that data.
  **Caught and fixed a real atomicity bug while building it**:
  `CareerState.Clone()` reference-copied `RivalBehavior`/
  `ReputationState`/`SafetyRating`/`DriverLicense` — safe only because
  no command mutated them yet, which its own doc comment already
  warned about. `RecordRaceOutcome` is the first command that does;
  fixed by adding real `Clone()` methods to all four types (`WTRL.RPG`
  ×3, `WTRL.Racing` ×1) and using them in `CareerState.Clone()`. A
  dedicated regression test (`RecordRaceOutcomeIsAtomicOnFailure
  AlongsideOtherCommands`) guards against this specific bug recurring.
  **Caught a second real bug, this time from the real Unity Editor, not
  `dotnet test`**: an early version of `Racing/SampleContent.cs`
  referenced `WTRL.World.SampleContent` directly and failed to compile
  with CS0103 in the Editor -- Racing's asmdef has no World reference.
  The throwaway `dotnet test` verification method used throughout this
  project missed this entirely, because it copies every assembly's
  files into one flat folder, which hides real asmdef boundary
  violations. Fixed by duplicating the shared id as a string literal
  instead (same pattern `BuildRecipeProgress` already uses to avoid a
  `WTRL.Garage` dependency). **Noted as a standing gap in this
  project's verification method**: cross-assembly reference errors
  between two assemblies with no dependency on each other need a real
  Editor compile to catch, not just `dotnet test`.
  11 new tests added, verified via both the throwaway `dotnet test`
  method (87/87) and, for the first time on a same-day basis, the real
  Unity Editor Test Runner (88/88 — one more than the dotnet count
  reflects a pre-existing discrepancy in how the two methods enumerate
  tests, not a new failure). Structural validator: 16 assemblies, 54 C#
  files, no reference cycles (Claude).
- 2026-09-20: Brought a second vehicle onto the Foundry Row circuit,
  driven by AI under real physics (roadmap steps 8-9).
  Added `MarshContentBuilder` (`WTRL.Editor`) — the rival roster's first
  real `WTRL.Content` asset (`marsh-gen1`), matching the
  `marsh_gen1_1991` Blender blockout's mass/wheelbase from the earlier
  rival-blockout pass. Real script-reference GUIDs confirmed correct
  (the per-file-type fix from the Hero pass holds for a second builder).
  Added `Racing/AiVehicleSession.cs` — the AI-driving analog of
  `WTRLRuntime`/`VehicleRuntimeController`: drives real
  `VehicleSimulation.Step` physics every call, sourcing input from
  `TrackAiDriver` instead of a keyboard. This closes the gap between
  two previously separate proofs (`VehicleRuntimeControllerTests`: real
  physics, player input, no AI; `SampleContentTests`'s AI-line test:
  real AI perception, simplified kinematic movement, not real physics)
  — this is the first test coverage exercising real physics under AI
  control end to end, including a 7200-step finite-state stability
  check mirroring `WTRL.Vehicle`'s own fuzz-input determinism
  discipline. 3 new tests, verified via both the throwaway `dotnet
  test` method (90/90) and the real Unity Editor Test Runner (91/91).
  Structural validator: 16 assemblies, 57 C# files, no reference
  cycles (Claude).
- 2026-09-20: Best-effort pass on the remaining roadmap items that
  don't need visual review to be real (steps 16-25, 27), per explicit
  instruction to do everything possible and defer visual judgment to a
  later review — everything below compiles and passes tests, but
  nothing has been looked at on screen or heard.
  **Pure logic, fully tested** (verified via both `dotnet test`, 97/97,
  and the real Unity Editor Test Runner, 98/98 EditMode + 4/4 PlayMode):
  `Career/EventPreflightService.cs` (event-preflight gating, scoped to
  reputation + Safety Rating/license only — the other axes Rev16.1's
  audit checked, chapter/lineage/homologation/fuel/loadout, aren't real
  systems in this project yet); `Events/RaceFlowController.cs` (the
  8-state wrapper this project has referenced as future work since
  Events was first ported); `Garage/SampleContent.cs`'s
  `Hero1965TrackBuild()` (the project's first fully satisfiable build
  recipe, 0 of 35 existed before) and `RecognizedParts` (the 3 part ids
  `VehicleConfigurationResolver` actually does anything with).
  **Unity-dependent, compiles clean, exercised by existing PlayMode
  tests where applicable, but visually unreviewed**: `WorldStreaming
  Controller` (wires the previously-unconsumed `WorldStreamingGrid` to
  a follow target), `VehicleAudioController` (maps the 6 procedural
  audio layers to real `AudioSource`s — no clips assigned anywhere),
  `TelemetryHud`/`GarageScreen`/`DynoScreen` (functional IMGUI screens,
  not designed UI — no Canvas/prefab/font asset existed to build real
  UI against), `CareerStateHolder`, `SimpleFollowCamera` (plain lerp,
  not Cinemachine despite the package being installed — real rig setup
  needs visual iteration), and touch/tilt input added to
  `VehicleRuntimeController` (NOT a port of Rev16.1's real
  `MobileInputMath.cs`, which wasn't available to read this pass — a
  reasonable equivalent shape, flagged as such).
  **Assembled all of it into one real scene**:
  `Editor/VerticalSliceSceneBuilder.cs` creates
  `Assets/WrenchToRaceLegends/Scenes/VerticalSlice.unity` — a Foundry
  Row circuit blockout (primitive markers at the real
  `SampleContent.FoundryRowCircuitLine` waypoints) plus two facility
  markers (Garage, Gas Station), the hero-1965 vehicle wired to every
  system above, a plain follow camera, and a directional light. No
  mesh/texture/lighting authoring happened — primitives and default
  materials only. This is the project's first real scene file.
  **Not done, and flagged as not achievable this pass**: step 29
  (playtesting the reputation/class-bracket thresholds) needs an actual
  human playing the game, which cannot happen inside this session.
  The single most important next step is still the one this pass could
  not do itself: open `VerticalSlice.unity`, press Play, and look at
  what's there (Claude).
- 2026-09-20: Visual/graphics improvement pass, per request. Both
  vehicles in `VerticalSlice.unity` were, until this pass, literally
  invisible empty GameObjects — closed that first. Exported real
  geometry from `racinggame/BlenderPipeline/REFERENCE-MODELING-
  ACCEPTANCE.md`'s own documented "accepted blockout baseline"
  (`correct_axis_heroes/1967_crownfire_v7.blend`,
  `correct_axis_heroes/marsh_nsx91_v6.blend` — the latest versions past
  that doc's last-recorded v5/v4), NOT the disqualified procedural
  fleet output the earlier rival-blockout pass used. Caught and fixed a
  real source-data orientation bug via bounds measurement rather than
  eyes (the source authors length along Y and height along Z; a -90°
  correction on instantiation fixes it — see `UI/CONTRACT.md` for the
  full diagnostic trail, including that two different FBX exporter
  axis-remap settings produced identical (wrong) bounds, proving the
  issue was in the source data, not the export step).
  Added flat metallic paint materials (deep red hero, silver Marsh) —
  genuine improvement over Unity's default missing-material magenta,
  not a finished paint job, since the source models have no material
  authoring yet. Added a modest URP post-processing volume (bloom,
  contrast/saturation, vignette), warm directional light + trilight
  ambient + distance fog, an asphalt ground material, and emissive
  track-marker cylinders (replacing plain gray spheres).
  Added `Racing/AiVehicleController` (MonoBehaviour wrapper around the
  already-tested `AiVehicleSession`) so Marsh now visibly drives the
  Foundry Row circuit under AI control — the scene has two moving cars.
  **Caught a second real Unity bug**: loading the Marsh content asset
  right before `EditorSceneManager.NewScene()` consistently returned
  null for a valid asset, while the identically-loaded hero asset
  (assigned to a component immediately) worked fine — likely Unity
  unloading an unreferenced ScriptableObject asset across the scene
  switch. Fixed by reloading immediately before use instead of caching
  across the scene creation. Verified via 3 isolation attempts before
  landing on the real fix.
  Re-confirmed 98/98 EditMode tests passing after all of the above.
  **None of this has been seen** — every claim is backed by a compile
  pass, a bounds measurement, or a file diff, not a screenshot.
  Structural validator: 16 assemblies, 70 C# files, no reference
  cycles (Claude).
- 2026-09-20: Built the whole documented world, then generated real
  Blender geometry for it, per explicit request.
  Added `WTRL.World.DistrictDefinition` (new type -- districts/zones
  were real, named content in `racinggame/DEEP-CONTENT-CATALOG.md` but
  had no corresponding type anywhere in this port) and real content for
  every named location that document lists: both counties (Blackridge,
  San Triana), all 11 districts/zones, all 4 named race facilities
  (Redline Raceway, Cutback Tri-Oval, Longbow Speedway, Highbank
  Superspeedway), plus the 3 "original road-course principle" archetypes
  under this pass's own invented names (Whisperwood Forest/Cliffside
  Coastal/Ironclad Technical Circuit -- flagged as not sourced, same as
  the rival-generation names). Added `WTRL.Racing.SampleContent
  .BuildOvalLine`, a reusable procedural stadium-loop generator, rather
  than hand-placing oval waypoints 3 times. Every `TrackDefinition
  .LengthM` is the real measured polyline length of its paired racing
  line, not a guess -- a new cross-check test found and required
  correcting 6 of 7 initial length estimates.
  Then generated real ribbon-road Blender geometry for all 8 tracks
  (`racinggame/BlenderPipeline/scripts/generate_world_tracks.py`,
  matching the exact same waypoint coordinates as the C# content) and
  wired Foundry Row's mesh into `VerticalSlice.unity`, replacing what
  was only primitive node markers.
  **Found and fixed a second real orientation bug, and a third
  underlying tooling bug while chasing it.** The track mesh hit the
  same FBX axis-remap issue the vehicle models did (length landing on
  the wrong axis) -- but fixing it exposed that `Transform
  .localToWorldMatrix`/`Renderer.bounds` do NOT reliably reflect a
  rotation applied immediately after instantiation in this
  environment's `-nographics` headless batchmode, even when `Transform
  .rotation.eulerAngles` correctly reports the new value. This made a
  runtime-rotation fix (the vehicle approach) unverifiable for a flat,
  direction-sensitive mesh. Fixed instead by baking the axis
  correction directly into the Blender export script's vertex
  construction, verified against a freshly-instantiated, untouched
  copy (no post-instantiation rotation involved) matching the C#
  waypoints' real coordinates exactly.
  **Re-confirmed the earlier vehicle rotation fix is NOT compromised by
  this newly-found quirk** -- its rotation value is correctly present
  in `VerticalSlice.unity` as a real `PrefabInstance` override (found
  by grepping the saved scene's actual override *values*, not just
  property-path names); the quirk only affects reading a rotation's
  effect back within the same batchmode execution, not whether Unity
  correctly saves and later re-applies it.
  6 new tests, all passing via both the throwaway `dotnet test` method
  (103/103) and the real Unity Editor Test Runner (104/104 EditMode +
  4/4 PlayMode). Structural validator: 16 assemblies, 70 C# files, no
  reference cycles (Claude).
- 2026-09-20: Closed the three gaps named directly in a follow-up
  request: no elevation, no barriers, no texture.
  Added real per-node elevation for Whisperwood Forest Circuit (a 22m
  crest) and Cliffside Coastal Circuit (a 24m single steep drop at
  node 4, matching `Racing/SampleContent`'s own "signature drop"
  comment) via a new `elevationM` array in `world_tracks_manifest.json`
  -- confirmed via `ModelBoundsDiagnostic` (Y-size jumped from ~0 to
  ~28-30m for exactly these two tracks). Still not a real terrain
  system (none exists) -- linear interpolation between hand-placed
  waypoint heights on a flat-shaded ribbon.
  Added barrier walls (two per track, striped material) and a real
  procedurally-generated asphalt texture with a dashed centerline
  (tiled via real per-vertex UVs) to every one of the 8 tracks.
  **Found and worked around a real Blender/Unity FBX interop bug**:
  Blender's `embed_textures=True` export genuinely contains the
  texture data (confirmed via raw byte search of the exported file),
  but Unity's FBX importer never wired the resulting material's
  texture slot, regardless of how the source image was packed/saved on
  the Blender side. Rather than keep chasing that gap, switched to
  exporting each texture as a separate, plain PNG file and loading it
  directly in `VerticalSliceSceneBuilder`, bypassing FBX texture
  extraction entirely -- confirmed working by grepping the saved
  scene's actual material texture GUIDs against the real texture
  assets' own GUIDs, an exact match.
  Re-confirmed 104/104 EditMode + 4/4 PlayMode tests still pass.
  Structural validator: 16 assemblies, 70 C# files, no reference
  cycles (Claude).

- 2026-09-20: Further open-ended "improve graphics/visuals/assets"
  pass. Fixed a real regression from the earlier visual pass:
  `AttachVehicleModel` was painting every renderer -- including tires
  and glass -- the same flat body-paint color. The source `.blend`
  files already carry real per-part mesh names (`TIRE`, `RIM`,
  `GLASSHOUSE`, `HEADLAMP_BEZEL`, `SEAT_BACK`, etc., confirmed via a
  direct Blender name dump), so vehicles now get 7 distinct materials
  keyed by part name: body paint, matte tire, metallic rim, a real
  transparent glass material, chrome trim, warm lamp material, and a
  dark interior material. Generated a procedural grass/dirt ground
  texture via the same "author pixels in Blender, save as a plain PNG,
  skip FBX embedding" pipeline already proven for track textures, and
  wired it into the ground plane (still a flat plane, no terrain
  height variation -- documented as a known limitation, not fixed
  this pass). Replaced the two identical colored-cube facility markers
  with a building silhouette (body + peaked roof + emissive sign).
  Verified via scene-file GUID/keyword/name inspection (no visual
  screenshot capability exists in this environment): the transparent-
  glass keyword appears exactly twice (once per vehicle), the ground
  texture's own GUID appears in the saved scene, and `Body`/`Roof`/
  `Sign` sub-objects appear 6 times (2 facilities x 3 parts).
  Re-confirmed 104/104 EditMode + 4/4 PlayMode tests still pass.
  Structural validator: 16 assemblies, 70 C# files, no reference
  cycles (Claude).

- 2026-09-20: Fixed the ground-flatness gap the user pointed at
  directly after the previous pass. `BuildGround` now builds a real
  120x120-quad terrain mesh (14,641 vertices) with Perlin-noise height
  displacement (+-6m) instead of a `PrimitiveType.Plane`. **Caught a
  real placement bug before running it**: an initial draft flattened
  terrain height within only a 45m radius of the world origin, but
  `Racing.SampleContent.FoundryRowCircuitLine()`'s real waypoints span
  x:[-20,220] z:[0,100] -- centered nowhere near the origin -- which
  would have left most of the actual track sitting on sloped terrain.
  Fixed by using a rectangular flat zone sized to the real track
  bounding box (plus margin, also covering both facility markers) and
  centering the terrain mesh on the track's real center instead of the
  origin. **Caught a second real bug** the same way (scene-file
  inspection, not visual): the new `MeshCollider` was added but never
  given the generated mesh, so the ground would have had a visible
  surface with zero collision. Fixed by assigning
  `meshCollider.sharedMesh` directly. Verified via the rebuilt scene's
  logged height range (real displacement, not a flat 0), by checking
  the flat-zone bounds against the actual waypoint coordinates by
  hand, and by confirming both `MeshFilter` and `MeshCollider` on the
  `Ground` object reference the same embedded `TerrainMesh` in the
  saved scene file. Re-confirmed 104/104 EditMode + 4/4 PlayMode tests
  still pass. Structural validator: 16 assemblies, 70 C# files, no
  reference cycles (Claude).

- 2026-09-20: Two concrete world-content/gameplay gaps closed after a
  full "what's left to build" audit. (1) New `WorldTrackSceneBuilder`
  wires the other 7 tracks' already-generated FBX/texture assets into
  real Unity scenes under `Scenes/Tracks/` (road+barrier mesh, real
  waypoint markers, an AI vehicle, camera, lighting) -- they existed on
  disk but nothing had ever loaded them. redline-raceway stays excluded
  since it's a drag strip with no authored AI racing line, documented
  as an honest gap rather than inventing one. (2) Closed the
  race-completion -> Career wiring gap `RaceOutcomeDetail.cs` itself
  flagged: `RaceFlowController` now fires a `Completed` event (using
  only `WTRL.Events`' own types, keeping the existing one-way assembly
  dependency intact) and new `Career.RaceCompletionBridge` subscribes
  and applies a `RaceOutcomeDetail` via the already-existing
  `CareerTransaction.RecordRaceOutcome`. Deliberately does NOT set
  `RivalId`/win-detection fields, since no contact/rival-position
  system exists to back them honestly -- setting `PlayerWon=false`
  unconditionally would incorrectly record a loss every time, which is
  worse than not recording anything. 3 new tests drive a real
  `RaceFlowController` through its full phase sequence and assert
  `CareerState` was actually mutated by the fired event. 104 -> 107
  EditMode tests pass, 4/4 PlayMode, 16 assemblies / 72 C# files, no
  reference cycles (Claude).
  Separately: two parallel background attempts at the vehicle-art
  acceptance-gate backlog (wheel-arch boolean cuts, panel seam
  geometry, collision hulls, LODs) each produced real, independently
  verified Blender output, but against DIFFERENT source baseline
  versions and into different, uncoordinated export folders --
  duplicated, divergent, and not reconciled or wired into Unity's
  actual vehicle FBX assets. Left as-is (committed locally in
  `racinggame`, not pushed) rather than picking one arbitrarily;
  reconciling into one canonical output and wiring it into
  `WTRL-Unity/Assets/.../Art/Vehicles/` is real remaining work, not
  done here (Claude).

- 2026-09-20: A large "do as much as possible" push against 6 named
  gaps. **Garage**: `CanonicalBuildRecipes.All` ports all 35 real build
  recipes from the original Swift game's `CanonicalContent.swift`
  (0->1->35 of 35), and new `PartCatalogImporter` deserializes the
  real 1,560-entry `master_parts_catalog.json` research corpus
  (mirrored into `Assets/StreamingAssets/Corpus/`). Found and fixed a
  real schema mistake mid-build (dependencies/consequences are
  structured objects, not strings) and a real wrong assumption in the
  verifying test (some entries are deliberately generation-less
  "universal" parts). Deliberately does NOT fabricate Price/
  ReputationRequired/performance-delta numbers to force this research
  data into `PartDefinition` -- that catalog genuinely has no
  game-balance fields, confirmed by reading real entries.
  **Touch/tilt input**: already existed (`VehicleRuntimeController`) --
  confirmed via code read, not re-built.
  **Mobile camera**: `SimpleFollowCamera` gained real one-finger orbit
  + two-finger pinch-zoom via `EnhancedTouch`, with the actual math in
  hardware-independent methods so it's unit-testable (touch-hardware
  simulation is known-unreliable in this project's headless PlayMode
  runs, per an earlier finding).
  **Results-screen UI**: new `ResultsScreen`, phase-gated to
  `RaceFlowController.Phase == Results`, Continue button calls
  `Complete()`. NOT yet wired into `VerticalSliceSceneBuilder`'s scene
  (no active `RaceFlowController` there yet) -- an honest, named gap.
  **Milestone M8**: added real, narrow-scope infrastructure for 4 of
  its items -- `LocalizationTable` (English only, `ResultsScreen`
  migrated as the one example), `AnalyticsConsent` (opted-out by
  default, no real vendor SDK exists to wire to, one real call site in
  `RaceCompletionBridge`), and `DeviceQualityProfile` (3 fixed tiers, no
  automatic device detection -- impossible without real hardware).
  `CareerStateHolder` now actually calls `CareerSaveCodec` on real
  Unity lifecycle hooks (`Awake`/`OnApplicationPause`/`OnDestroy`) to a
  real file -- closing "save/load exists but nothing calls it".
  **Device testing** remains completely untouched: genuinely impossible
  without real hardware, not attempted.
  125/125 EditMode + 18/18 PlayMode tests pass (up from 107/4), 16
  assemblies / 85 C# files, no reference cycles (Claude).

- 2026-09-20: A deep-dive architecture/quality audit (full test run
  first: reconfirmed 125/125 EditMode + 18/18 PlayMode + 16 assemblies/
  85 files/no cycles, unchanged), then a targeted code review looking
  specifically for undocumented issues rather than restating known
  CONTRACT.md gaps. Found and fixed 3 real issues: (1) `WTRL.Persistence
  .asmdef` referenced `WTRL.World` with zero actual usage anywhere in
  that assembly (verified via grep for both `using` and qualified
  `WTRL.World.*` references before removing) -- removed. (2)
  `WTRL.Career.asmdef` referenced `WTRL.Vehicle` with the only mention
  anywhere being inside a doc comment, not code -- removed. Both
  removals reconfirmed against a full recompile + test rerun (still
  125/125 + 18/18) as the actual verification, not just "should be
  safe." (3) `Career/CONTRACT.md`'s "Not yet ported / not yet designed"
  section still listed the race-completion -> RPG/Career wiring gap as
  open, contradicting that same file's own later "Closed a long-flagged
  gap" and "Race-completion -> Career wiring closed" entries from
  earlier the same day -- a real internal self-contradiction, not
  caught when the wiring was closed because the original gap entry was
  never struck or updated. Fixed by striking the resolved claim in
  place with a note explaining the documentation-hygiene lesson (update
  the original gap entry in the same pass that closes it, don't just
  append a new entry further down). Also surfaced, not yet acted on:
  `Garage/PartCatalogImporter`'s whole JSON-import layer has zero
  production call sites (test-only for now, by design per its own
  CONTRACT.md, but worth flagging as it grows); a 124-vs-125 EditMode
  test-count curiosity (grep counts 124 `[Test]` attributes across
  `Tests/EditMode/*.cs`, but the real Unity Test Runner has
  consistently reported 125 across several runs this session -- not
  re-investigated further, noted for whoever looks next) (Claude).

- 2026-09-20: Reconciled the two divergent vehicle-art passes flagged
  as the top recommendation from the prior audit. Directly re-verified
  both candidate FBX outputs by re-importing each into a clean Blender
  scene and counting real objects/verts/faces, rather than trusting
  either pass's own changelog self-report -- this caught that one pass
  (`export/HeroCrownfire.fbx`/`MarshNsx.fbx`, built from v5/v4
  baselines) was genuinely broken: it silently dropped 30 and 24 mesh
  objects respectively (67/68 objects vs. the real 97/92), and its own
  self-reported "2235 verts / 1320 faces" for Crownfire didn't even
  match a fresh re-import of its own file (6108/5414 verts/faces) --
  that pass's verification step was itself wrong, not just its export.
  The other pass (`export/pipeline_updated/`, built from the v7/v6
  baselines that were already the real Unity-wired ones) had the
  correct 97/92 object counts with real added wheel-arch/seam
  geometry. Adopted that one as canonical: copied into
  `WTRL-Unity/Assets/.../Art/Vehicles/HeroCrownfire.fbx`/`MarshNsx.fbx`
  (replacing the pre-arch/seam versions) plus their LOD/collision FBX
  siblings (not yet wired into any scene builder). Deleted the broken
  pass's entire output from `racinggame` rather than leave two
  candidate sources on disk. Also fixed `REFERENCE-MODELING-
  ACCEPTANCE.md`'s "Current accepted blockout baselines" section,
  which had been pointing at the wrong (v5/v4) baseline files this
  whole time -- a stale pointer that predated both of today's passes.
  Found one new real defect while verifying: Unity's FBX importer logs
  17 "self-intersecting polygon discarded" warnings on the new
  `Crownfire_BODY_SHELL`, a real mesh-quality side effect of the
  boolean/bevel operations -- confirmed cosmetic-scale (overall bounds
  and object count unchanged via `ModelBoundsDiagnostic`), left open
  as a known defect rather than silently ignored. Rebuilt
  `VerticalSlice.unity` and all 6 track scenes against the new FBX;
  125/125 EditMode + 18/18 PlayMode tests still pass, 16 assemblies /
  85 C# files, no reference cycles (Claude).

- 2026-09-20: A full "run everything through Blender again" quality
  pass, closing the exact defect the prior entry left open. Diagnosed
  the self-intersecting-polygon warning with a real bmesh health check
  first (zero non-manifold edges, zero degenerate faces -- ruling out
  topology damage before concluding it was a boolean-solver seam
  artifact), then fixed both vehicle bodies with a new
  `clean_body_mesh_artifacts.py` (merge-by-distance + dissolve-
  degenerate + recalc normals). Re-verified via a fresh Unity re-import
  of the new FBX: zero self-intersecting warnings (down from 17), same
  97/92 object counts, identical overall bounds. Also caught and fixed
  a real mistake mid-pass: an initial texture regeneration accidentally
  copied stale, previously-cached 256px textures back into Unity
  instead of the freshly-generated 1024px ones (the generator script
  only ever wrote to a temp directory, not the repo's export folder --
  an existing, easy-to-miss quirk of this pipeline), caught by directly
  checking the copied files' real resolution with `file` rather than
  trusting the copy step succeeded. Raised every track's asphalt
  (256px->1024px) and barrier (64px->256px) texture resolution with
  new layered multi-octave noise, added a solid road-edge line
  (previously centerline-only), and added weathered/bolted barrier
  detail; raised the ground texture 512px->1024px with the same
  layered-noise technique. Still color-only procedural textures, not a
  real PBR pass (no normal/roughness/AO maps) -- an honest scope limit.
  Rebuilt `VerticalSlice.unity` and all 6 track scenes against every
  new asset; 125/125 EditMode + 18/18 PlayMode tests still pass, 16
  assemblies / 85 C# files, no reference cycles (Claude).

- 2026-09-20: The real PBR pass requested as a direct follow-up.
  racinggame's texture generators refactored so each surface's color
  painter shares its per-texel description with a new height-field-
  based normal/AO derivation (real finite-difference slope, not hand-
  painted) plus a hand-authored metallic-smoothness map correlated with
  the same surface features (barrier bolt heads are the one genuinely
  metallic/glossy surface in the scene). Caught a real bug before it
  reached Unity: the metallic-smoothness textures were silently saved
  as 24-bit RGB with the packed smoothness alpha channel discarded
  (`bpy.data.images.new`'s `alpha=False` default) -- caught by checking
  the real saved file format, fixed by passing `alpha=True`, and
  reconfirmed as real 32-bit RGBA before copying into Unity.
  New `PbrMaterialFactory.cs` wires `_BumpMap`/`_OcclusionMap`/
  `_MetallicGlossMap` with the correct URP/Lit keywords wherever this
  project builds a track/barrier/ground material (replacing bare
  `mainTexture` assignments in both scene builders). New
  `PbrTextureImportSettings.cs` forces the required Texture Type
  (Normal Map) and linear color space (sRGB off) on every generated map
  -- necessary for correct shader decoding, not cosmetic -- verified by
  re-reading each texture's own `.meta` file after running it, not
  assumed. Rebuilt `VerticalSlice.unity` and all 6 track scenes;
  confirmed exactly 3 materials carry the `_NORMALMAP`/
  `_METALLICSPECGLOSSMAP` keywords in the saved scene files (ground +
  asphalt + barrier, matching the 3 targeted surfaces). 125/125
  EditMode + 18/18 PlayMode tests pass, 16 assemblies / 87 C# files, no
  reference cycles. Vehicle materials were explicitly out of scope this
  pass -- ground-level generated surfaces only (Claude).
