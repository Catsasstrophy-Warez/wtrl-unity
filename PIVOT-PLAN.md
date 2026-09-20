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
