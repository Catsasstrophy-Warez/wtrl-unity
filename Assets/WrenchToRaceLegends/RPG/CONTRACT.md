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

## Deliberately not implemented yet (see `OUTPUT-RPG-Design.md` §"Open questions")

- **`BuildRecipe`** (spec Part 1) — needs a tuning-table snapshot shape
  that lives in `WTRL.Garage`, but `WTRL.RPG` has no dependency on
  `WTRL.Garage`. Three options are laid out in the design doc; none
  chosen yet because it's a real architecture decision, not an
  implementation detail. Don't add a `WTRL.Garage` dependency to this
  assembly to unblock this without deciding that question first.
- **`Prototype~/RPG/ReputationTraits.cs`'s rival-AI-adaptation-from-
  player-traits** and **`DiagnosticSkill.cs`'s mentor-dialogue-shortening**
  — real prior design thinking, but **not in `48-RPG-SYSTEMS-SPEC.md`**
  (the only spec document this design pass actually read). May be
  covered by `49-PLAYER-CHARACTER-RPG.md`/`50-ACTION-PILLAR-EXPANDED.md`,
  neither read this pass — read those before adopting or discarding
  either system.

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project (this assembly has no dependencies, so it was
checked standalone rather than alongside the others). **0 errors, 0
warnings** on the first build attempt (after fixing one real bug the
attempt caught: `ClassBracketTier` enum values compared with `<=`
directly, which C# doesn't allow on enums without a cast — fixed by
casting to `int` before comparing). 12 new tests, each asserting a
specific numbered claim from `48-RPG-SYSTEMS-SPEC.md` (cited in the test
name/comment) rather than a vague behavior check, so a future spec
change surfaces as a specific failing test. All 12 pass.
