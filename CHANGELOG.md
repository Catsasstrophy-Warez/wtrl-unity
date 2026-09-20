# WTRL-Unity Changelog

Append-only chronological log of real work sessions on this project.
Moved out of PIVOT-PLAN.md on 2026-09-20 (that file had grown past
1,100 lines of pure narrative accretion, mixing a live architecture
reference with an ever-growing history -- a real maintenance concern
flagged by a deep-dive documentation-health audit). PIVOT-PLAN.md is
now the architecture/decisions document only; PROJECT-STATUS.md is
the short, in-place-edited (never appended to) current-state summary.
This file is the one place that keeps growing -- that's its job.

**Process rule, added 2026-09-20 after finding the same mistake 5
times in one session**: when a CONTRACT.md's "known gaps"/"not yet
done" section is about to be closed by new work, the commit that
closes it MUST edit that original bullet in place (strike it, note
what closed it and when) -- never just append a new "CLOSED"
section further down and leave the original claim standing. A
CONTRACT.md that contradicts itself is worse than one with an
honestly-stale timestamp, because a reader has no way to tell which
half is current without re-deriving it from the code, which is
exactly the work this documentation exists to save.

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

- 2026-09-20: A "do all 20" push against the roadmap from a full
  project analysis. Full test/build re-verification first (125/125
  EditMode, 18/18 PlayMode, 16/87, zero self-intersecting polygons on
  the vehicles -- all unchanged, confirming last pass's work held).
  Then, in order: (1) audited every assembly for the unused-reference
  smell found twice before -- removed 18 more dead references across 6
  assemblies (`Runtime` alone went from 10 references to 3), verified
  safe by full recompile+retest after each batch. (2) Found the
  "RivalIntimidation/TrackAI port" and "WTRLRuntime MonoBehaviour
  wrapper" roadmap items were BOTH already done in earlier sessions --
  `Racing/RivalIntimidation.cs`+`TrackAIDriver.cs`+their tests, and
  `UI/VehicleRuntimeController.cs`'s real `FixedUpdate()` loop -- their
  CONTRACT.md entries were just never updated when the work landed.
  Fixed both stale entries (the same documentation-hygiene lesson
  flagged three times now). (3) Found the real narrower gap inside the
  intimidation "port": the intimidation-aware `TrackAiDriver.Input`
  overload was tested in isolation but never called from the real
  `AiVehicleSession.Step` loop -- wired it via new optional
  `Intimidation`/`Proximity` properties, sampling
  `RivalDeterministicSample` by the session's own advancing tick (not
  wall-clock), confirmed deterministic-replay-safe by a test running
  two independent sessions with identical inputs to bit-identical
  results. (4) Built real contact/overtake DETECTION primitives
  (`ContactDetector`/`TrackProgress`/`OvertakeTracker`) closing
  `Career/CONTRACT.md`'s named blocker -- deliberately detection-only,
  not yet wired into a caller with both vehicles' live state each
  frame (real, separate follow-on work). (5) Wired `ResultsScreen` into
  `VerticalSliceSceneBuilder` via new `RaceSessionController`, a real
  `RaceFlowController` driven by an arcade-style start-zone lap
  detector, wired to the same `CareerStateHolder` the scene already
  builds -- the full drive->lap->finish->results->career chain now
  exists in one scene. (6) Built `ContentValidator.cs`, the
  "validators" half of `WTRL.Editor`'s original vision (only
  "builders" existed before). (7) Investigated a duplicate-effort near-
  miss: wrote a new `WTRL.Core.DeterministicSample` before discovering
  `WTRL.Racing.RivalDeterministicSample` already existed identically
  and is already the one everything uses -- deleted the duplicate
  rather than ship two competing implementations, documented in
  `Core/CONTRACT.md` so the next person checks first. (8) Resolved the
  `hero-1965`/`hero_1967` naming question as a permanent decision (not
  a rename): the research corpus itself flags `hero_1967`'s OEM specs
  as an unresolved "researchGate," so there is no better data to
  rename toward today -- documented in both `HeroContentBuilder.cs` and
  `Editor/CONTRACT.md`. Remaining roadmap items (batch-converting the
  parts research corpus into gameplay content, authoring more vehicle/
  recipe/track content, the Workshop remove/install/repair/diagnose
  flow, golden-fixture physics parity tests, vehicle-specific art
  gates, vehicle PBR maps, prefab+physics-anchor generation, and World
  district streaming) were investigated and scoped but not built this
  pass -- each is a genuinely large, multi-session effort in its own
  right, several needing either human visual judgment
  (`REFERENCE-MODELING-ACCEPTANCE.md`'s unmet gates) or research data
  that doesn't exist yet (no golden physics fixtures found anywhere in
  `SwiftRacer`/`racinggame`), not something safely compressible into
  this pass without risking exactly the kind of unverified, invented
  content this project's whole discipline exists to prevent. 137/137
  EditMode + 22/22 PlayMode tests pass (up from 125/18), 16 assemblies
  / 92 C# files, no reference cycles (Claude).

- 2026-09-20: A "deep, comprehensive analysis" pass -- 3 parallel
  research audits (architecture/code quality, content completeness/
  data-authenticity, art-pipeline + documentation health) plus direct
  verification, aimed specifically at finding what prior audits missed
  rather than re-confirming what they already found. Real results:
  **(1) one genuine latent bug** in `RaceSessionController`'s lap-
  detection heuristic -- a hardcoded 2x radius multiplier meant a
  vehicle had to travel >30m from start before a lap could register at
  all, which would silently hang any race on a track smaller than that
  forever. Fixed with two independent, validated radius fields and a
  loud error instead of a silent hang; new test confirms both the
  warning and the recovery. **(2) two more stale CONTRACT.md claims**
  found (`Lab/CONTRACT.md` claiming `WTRLRuntime`/`WTRL.Runtime` didn't
  exist, `Events/CONTRACT.md` claiming event-preflight gating wasn't
  implemented -- both real and already closed in earlier sessions,
  just never marked). That's 5 stale-doc instances found across this
  whole session now -- a real, recurring process gap, not isolated
  incidents. **(3) content-completeness accounting, with real
  fractions**: 2 of 159 researched vehicles have real in-game content
  (~1.3%); 3 real gameplay-functional parts against a 1,560-entry inert
  research catalog (under 0.2%); of 8 real tracks, names are 5/8
  corpus-sourced honestly flagged, the rest self-disclosed as invented;
  the "sourced/real" citation discipline was spot-checked against 2
  actual corpus source files and found accurate both times. **(4) art
  gates: still 2 of 10 met** (wheel centers/radius/track/axle, and
  wheel-arch-cut-into-body-topology) -- confirmed via the acceptance
  doc's own honest changelog, not re-litigated. Vehicles confirmed to
  have zero PBR maps (the ground/track PBR pass never touched them).
  Zero scenery (trees/buildings/poles) exists in any scene beyond the
  barrier walls already built. **(5) flagged, not fixed**: this file
  (`PIVOT-PLAN.md`) is now 1,085+ lines of pure narrative accretion --
  a real maintenance concern worth a future restructure (append-only
  changelog + a short separately-maintained "current state" summary),
  not attempted this pass. All findings independently verified (not
  trusted from agent self-report) before acting: re-read the actual
  bug's code, re-ran the actual test suite, re-checked the actual
  citation source files. 137/137 EditMode + 23/23 PlayMode tests pass,
  16 assemblies / 92 C# files, no reference cycles (Claude).

- 2026-09-20: Wired the #1 item on PROJECT-STATUS.md's own "what would
  most change the picture next" list: contact/win-detection into a
  live per-frame caller. Added `LapProgressTracker` (unwraps per-lap
  arc length into total distance -- the honest substitute for "laps
  completed" for a vehicle with no `RaceFlowController` of its own) and
  `OvertakeTracker.LastFlipFavoredA` (which side became ahead) to
  `Racing/ContactDetector.cs`. `AiVehicleController` now exposes its
  real live `VehicleSimState`/rival id. New
  `RaceCompletionBridge.EnrichOutcome` hook lets `WTRL.UI
  .RaceSessionController` (which now optionally takes a `rival`
  reference) feed a real, per-frame-derived outcome -- `RivalId`/
  `PlayerWon` (from each vehicle's actual unwrapped total distance at
  race end)/`CleanOvertakeOccurred` -- into `CareerTransaction`, while
  `WTRL.Career` still has zero dependency on scene-level vehicle-
  position code. Fault-attribution fields
  (`PlayerCausedContact`/`CausedRivalSpinOrRetire`/
  `OffTrackCutForAdvantage`) stay unset even with a rival present --
  still no such detection exists, and setting them would be
  fabrication. Wired the real `MarshVehicle` as the rival in
  `VerticalSliceSceneBuilder`, verified via the actual fileID reference
  chain in the saved scene file (not assumed from the Editor script's
  logic alone).
  Found and fixed one real test-design flaw while writing this: an
  initial attempt to verify "player finishes farther ahead" via full
  vehicle-movement simulation fought the arcade lap-detection
  heuristic itself (a completed lap always ends near the start line,
  i.e. near-zero current track progress) -- switched to testing the
  real enrichment method directly against known progress values
  instead, which is both more robust and more precisely targeted at
  the actual new code. Also found and fixed a real test-setup bug (not
  a production bug): constructing a test rival via `AddComponent`
  triggered `Awake()` immediately with `vehicle` still unassigned,
  logging a real error before the test could configure it -- fixed by
  constructing the GameObject inactive first so `Awake` defers.
  10 new tests (5 EditMode covering `LapProgressTracker`/
  `OvertakeTracker.LastFlipFavoredA`, 5 PlayMode covering the real
  enrichment logic and a full live-race integration check). 142/142
  EditMode + 28/28 PlayMode tests pass, 16 assemblies / 92 C# files, no
  reference cycles (Claude).

- 2026-09-20: Items #2 and #3 from PROJECT-STATUS.md's "what would most
  change the picture next" list, in the same session as item #1 above.
  **#2, batch-converting the parts corpus**: new
  `Garage.ResearchPartConverter.ConvertAll` turns all 1,560 real
  `ResearchPartRecord`s into real, priced, named `PartDefinition`s.
  Explicitly not claimed as balanced content: Price/ReputationRequired
  come from a deterministic formula over real categorical fields
  (Origin/Quality/VariantLevel), flagged as placeholder pricing, not
  researched; TopSpeedDelta/AccelerationDelta are hardcoded to exactly
  zero for every part -- inventing performance effects with no physics
  basis would be fabrication. Deliberately NOT wired into
  `VehicleConfigurationResolver`'s switch statement, which exists
  specifically to guarantee every recognized part id has a real,
  intentional effect. `ContentValidator` now checks the full converted
  catalog (unique ids, positive prices, zero performance deltas) --
  confirmed clean by actually running it against all 1,560 real
  entries, not assumed. 7 new EditMode tests.
  **#3, more content in the established pattern**: new
  `HeroMid70sContentBuilder` is the project's 3rd real vehicle content
  asset, following the same pattern as Hero/Marsh -- but its mass
  (1580kg) and peak power (140hp) are the REAL, already-cited figures
  from `CanonicalBuildRecipes.cs`'s own mid-70s comment, reused rather
  than re-cited to avoid a second possibly-inconsistent citation of the
  same fact. New test confirms `hero75-full-compression` (one of the 5
  real `hero75-*` recipes) is satisfiable end-to-end against this real
  vehicle (1580/140 = 11.29 kg/hp, exactly the top of its real
  11.0-11.29 band) and correctly fails with the wrong differential.
  Verified the generated `.asset` files directly: correct script GUID,
  correct data, correct cross-references.
  150/150 EditMode + 28/28 PlayMode tests pass, 16 assemblies / 95 C#
  files, no reference cycles (Claude).
