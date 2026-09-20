# WTRL.RPG — contract

**New design, not a port.** No SwiftRacer/`WTRLCore` implementation of
the RPG layer exists anywhere — this is designed fresh from
`racinggame/05-specifications/48-RPG-SYSTEMS-SPEC.md`, informed by (but
not copied from) the archived Rev16.1 project's `Prototype~/RPG/`
folder, which is real prior design thinking that **never compiled and
was never verified in any way**. Full reasoning, spec summary, and what
was/wasn't adopted from `Prototype~`: `Assignments/OUTPUT-RPG-Design.md`.
Depends on `WTRL.Core` only (empty).

## Public API

**Reputation** (`Reputation.cs`): `ReputationTier {Unknown, Known,
Respected, Trusted}`, `ReputationEvent`, `ReputationState` — tracks
points, derives tier from thresholds, and (per spec S2.3) gives
diminishing returns for repeat wins against the same named rival via
`RecordNamedRivalWin(rivalId, baseValue)` specifically (calling the
generic `RecordEvent` with `NamedRivalWin` throws — it needs the rival
id, which the generic path can't supply).

**Safety Rating & license** (`SafetyRating.cs`): `SafetyEvent` (six
cases, matching spec S3.2's table exactly), `SafetyRatingState`
(bounded `[0,100]`, starts at 100), `DriverLicenseGrade {Provisional,
Club, Contender, Licensed}`, `DriverLicenseState.PermitsKnockoutEntry(
SafetyRatingState)` — per spec S3.3, only enforces a minimum safety
rating from `Contender` grade onward; `Provisional`/`Club` always
permit entry (the street tier's aggression economy makes contact a
viable, costed strategy, not a locked-out one).

**Class brackets** (`ClassBracket.cs`): `ClassBracketTier {Street, Club,
SemiPro, Pro}`, `ClassBracket.BracketForPoints`/`QualifiesForBracket` —
a ceiling check (a build at or below a bracket's point ceiling
qualifies for that bracket's events), not an exact-tier match.

**Deliberately kept as three fully separate types/axes** (`Reputation
State`, `DriverLicenseState`/`SafetyRatingState`, `ClassBracket`) per
spec S2.2's explicit warning against collapsing them into "one disguised
meter." Property tier (facility capability) isn't modeled here at
all — it's a fourth, still-separate axis the spec assigns to
`25-GARAGE-DESIGN.md` Part 7, i.e. `WTRL.Garage`'s domain, not RPG's.

## Known placeholder values — need playtesting, not a documents-only decision

`ReputationState`'s three tier thresholds (20/60/120) and
`ClassBracket`'s three point ceilings (15/35/60) are carried over
verbatim from Rev16.1's `Prototype~/RPG/PartsGating.cs`, where they were
**already explicitly unreasoned** (no citation to a content-resolution
pass, unlike most numbers in this project's research corpus). Do not
treat these as sourced content the way, say, `RivalIntimidation
.Ceilings` in `WTRL.Racing` are — they're real code with placeholder
tuning values, flagged as such in both files' comments.

## `BuildRecipe` — resolved (was an open question)

**Option 3 from `OUTPUT-RPG-Design.md`, implemented**: the recipe
satisfaction check needs `VehicleDefinition`/`EngineDefinition`/
`DifferentialKind`, so it lives in `WTRL.Garage.BuildRecipeEvaluator
.SatisfiesTarget(BuildRecipeDefinition, VehicleDefinition,
EngineDefinition)` — checks weight-to-power range and, when set, a
required differential type (only `"lsd"`/`"lsdRace"` ever appear in the
real 35-recipe corpus; both map to `ClutchLsd`/`TorqueBiasing`, and an
unrecognized string fails honestly rather than silently passing — this
project's standing discipline, not a new rule invented here).

`WTRL.RPG.SavedBuildRecipe` (`BuildRecipeProgress.cs`) holds only the
player-progression side — name, `RecipeKind {FreeForm, Target}`, an
`IsCompleted` flag, unlocked title/livery — and references its target
`BuildRecipeDefinition` **by string id only**, never by type. `WTRL.RPG`
still has no dependency on `WTRL.Garage`; the caller (eventually
`WTRL.Career`, which already depends on both) is responsible for calling
`BuildRecipeEvaluator.SatisfiesTarget` and only then calling
`SavedBuildRecipe.MarkCompleted()`.

Also newly enforced: `BuildRecipeDefinition.RequiredDifferentialType`,
previously flagged (both in the Swift original and this port) as
sourced content nothing read. **Still unenforced**:
`RequiredTransmissionId`/`RequiredCrankType` — exactly as documented on
those two properties; this pass didn't invent enforcement for them.

Verified with 7 new tests (weight-to-power range, differential mapping
including the "unrecognized type never silently passes" case, free-form
recipes always completing, target recipes requiring an explicit
`MarkCompleted()`, and a structural guard confirming `WTRL.RPG` never
references `WTRL.Garage`/`WTRL.Vehicle` types directly) — all pass. See
"Verification" below for the full project-wide test count.

## Deliberately not implemented yet (see `OUTPUT-RPG-Design.md` §"Open questions")

- **`Prototype~/RPG/ReputationTraits.cs`'s rival-AI-adaptation-from-
  player-traits** and **`DiagnosticSkill.cs`'s mentor-dialogue-shortening**
  — real prior design thinking, but **not in `48-RPG-SYSTEMS-SPEC.md`**
  (the only spec document this design pass actually read). May be
  covered by `49-PLAYER-CHARACTER-RPG.md`/`50-ACTION-PILLAR-EXPANDED.md`,
  neither read this pass — read those before adopting or discarding
  either system.

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project. The reputation/safety/license/bracket types were
checked standalone (0 dependencies); `BuildRecipeEvaluator`/
`SavedBuildRecipe` were checked alongside `WTRL.Vehicle`/`WTRL.Garage`
(the former needs their types, the latter doesn't but lives in the same
assembly). **0 errors, 0 warnings** throughout (one real bug caught and
fixed along the way: `ClassBracketTier` enum values compared with `<=`
directly, which C# doesn't allow on enums without a cast). 19 new tests
total (12 for reputation/safety/license/bracket, 7 for the BuildRecipe
split), each asserting a specific numbered claim from
`48-RPG-SYSTEMS-SPEC.md` rather than a vague behavior check. **43/43
passing project-wide**: `WTRL.Vehicle` (5) + `WTRL.Racing` (8) +
`WTRL.Garage` (5 + 4 new `BuildRecipeEvaluator` tests) + `WTRL.Lab` (6) +
`WTRL.RPG` (12 + 3 new `SavedBuildRecipe` tests).
