# WTRL.RPG design

Written by Claude, not Gemini — see the note at the top of
`OUTPUT-Rev16.1-Audit.md`. Same brief, same scope, same verification
discipline (`Prototype~` content treated as reference-only prior
thinking, never as working code).

## What `48-RPG-SYSTEMS-SPEC.md` actually specifies

Three concrete systems, each tied into existing content rather than
designed in isolation:

**1. Named build recipes.** A saved snapshot of the seven-subsystem
tuning table, player-named, reloadable. Two kinds: **free-form**
(organizational only) and **target** (a build brief with checkable
thresholds — weight-to-power ratio in a range, minimum downforce, a
required differential type — that unlocks a title and sometimes a
livery on completion). Thirty-five target recipes fall naturally out of
the existing five-tier trim ladder × seven hero generations, though the
spec explicitly says not all need to exist as day-one content. A second,
free source of target recipes: a build that happens to satisfy an
existing race-format objective set can surface as a suggested recipe
name at zero new-authoring cost. Recipes interact with class brackets
(a recipe's stats imply its bracket), with repairable/replace-only parts
(a loaded old recipe should flag parts due for replacement, not silently
hand over a worn build), and with mentor dialogue (a completed target
recipe deserves a specific line, not generic praise).

**2. Two-axis gating — money and reputation, kept genuinely distinct
from two other existing gates.** Money buys what the catalogue shows;
reputation determines what the catalogue shows at all, independent of
money. The spec is explicit that this must stay separate from
**property tier** (facility capability — a driveway has no lift for a
differential swap, full stop, regardless of trust or cash) and **class
brackets** (the built car's own stats determine which events it can
enter) — three axes, not one collapsed meter, or "the RPG layer
collapses into one disguised meter." Reputation accrues from named-rival
wins (diminishing on repeat wins against an already-beaten rival),
shop-driver job completion, and touge duel wins specifically (weighted
higher than outrun wins, since format skill is build-independent).
Four reputation tiers gate the parts catalogue: **Unknown** (stock-
replacement only) → **Known** (performance unlocked) → **Respected**
(race tier unlocked) → **Trusted** (forced induction/crank swaps
unlocked — the rarest parts, behind the highest trust tier
specifically).

**3. Safety Rating, given an actual formula for the first time.** A
second, independent rating alongside pace (which is already implicit
from event results). Moves on real events: player-caused contact and
off-track cuts for advantage decrease it (rival spins/retirals caused by
the player decrease it sharply); clean overtakes, zero-incident event
completion, and successful defensive holds increase it, modestly. It's
explicitly **bounded, not unbounded** — a 0-100 scale communicates
"where do I stand" better than an accumulating score. Tier-dependent
meaning is the actual point: at the street tier it's tracked but low
values only softly suppress reputation gain; from the professional
licence onward, a minimum Safety Rating is *required to enter* knockout
events — not a punishment screen, a locked entry list, mechanically
enforcing the narrative's "unlearn what won you street races" arc. It
explicitly never penalizes the instability/ragged-edge meter's own
findings — losing grip at the limit is physics, not conduct; only
contact and deliberate track-limit abuse move it.

## What from `Prototype~/RPG/` is worth keeping

**Reminder, since this is never-compiled reference code, not verified
working code**: none of this ran. Judged purely as prior design thinking
to react to.

- **`PartsGating.cs`'s `ReputationTier { Unknown, Known, Respected,
  Trusted }`** matches the spec's four tiers exactly, with concrete
  numeric thresholds (20/60/120 points) for tier transitions. Worth
  keeping the tier names and the "numeric score → tier" shape; the exact
  threshold values are unreasoned placeholders (no citation to a
  content-resolution pass), so treat them as needing the same
  "UNJUSTIFIED — genuine playtesting target" honesty its own
  `DriverLicense.dirtyResultsBeforeDemotion` field already models.
- **`ClassBracket` `{ Street, Club, SemiPro, Pro }`** with point
  ceilings (15/35/60) and `QualifiesForBracket(points, tier)` — real,
  useful, and already correctly kept separate from `PropertyTier`
  (`Driveway, Garage, ProShop, Warehouse`) exactly as the spec's §2.2
  table demands. Keep this three-axis separation as a hard design
  constraint in `WTRL.RPG`/`WTRL.Career` — it's the one thing the spec
  itself calls out as easy to collapse by accident.
- **`DriverProgression.cs`'s `SafetyRating`** (`0-100`, bounded,
  `SafetyEvent` enum matching the spec's table term-for-term) and
  **`DriverLicense`** (`Grade { Provisional, Club, Contender,
  Licensed }`, `PermitsEntry(ClassBracket.Tier)`, tracking clean-result
  streaks and distinct shop-driver job types completed) — this is a
  faithful, already-thought-through implementation of Part 3. Worth
  porting the shape closely.
- **`BuildRecipe.cs`** (a `ScriptableObject` with `RecipeType {FreeForm,
  Target}`, `SatisfiesTarget(...)`, `partsNeedingReplacementAtSaveTime`)
  — matches Part 1 closely, including the worn-parts flag the spec
  explicitly asks for.
- **`ReputationTraits.cs`** — derives named traits (`HasCleanRacerTrait`,
  `HasNoStrangerToContactTrait`, `HasReadsACarFastTrait`) from a running
  safety-rating average and applies them to rival AI. **Not in the spec
  I read** (`48-RPG-SYSTEMS-SPEC.md` doesn't mention player-trait-driven
  rival AI adaptation) — flagging as a genuine question below, not
  silently adopting it.
- **`DiagnosticSkill.cs`** — tracks usage counts (rival inspections,
  telemetry reads, dyno sessions) and unlocks deeper diagnostic detail
  past a threshold, plus a `MentorPreemptionFactor01` that presumably
  shortens mentor dialogue as the player demonstrates they already
  understand the car. **Also not in `48-RPG-SYSTEMS-SPEC.md`** — likely
  covered by `49-PLAYER-CHARACTER-RPG.md`, which this pass did not read
  (flagged as an open question below rather than guessed at).

## Proposed `WTRL.RPG` structure

```csharp
namespace WTRL.RPG {
    // Part 2 — reputation
    public enum ReputationTier { Unknown, Known, Respected, Trusted }
    public sealed class ReputationState {
        public double Points { get; private set; }
        public ReputationTier Tier => /* threshold lookup, see open question below */;
        public void RecordEvent(ReputationEvent evt) { /* diminishing returns vs. an already-beaten rival */ }
    }
    public enum ReputationEvent { NamedRivalWin, ShopDriverJobCompleted, TougeDuelWin, OutrunWin }

    // Part 3 — safety rating (kept fully separate from ReputationState —
    // this is the one thing the spec is loudest about not collapsing)
    public sealed class SafetyRatingState {
        public double Rating { get; private set; } = 100; // starts clean, bounded [0,100]
        public void RecordEvent(SafetyEvent evt) { /* real deltas from 48 S3.2's table */ }
    }
    public enum SafetyEvent {
        PlayerCausedContact, OffTrackCutForAdvantage, CausedRivalSpinOrRetire,
        CleanOvertakeNoContact, EventCompletedZeroIncidents, DefensiveHoldNoContact,
    }
    public enum DriverLicenseGrade { Provisional, Club, Contender, Licensed }
    public sealed class DriverLicenseState {
        public DriverLicenseGrade Grade { get; private set; }
        public bool PermitsEntry(ClassBracketTier eventBracket, SafetyRatingState safety) { /* the professional-tier gate */ }
    }

    // Part 1 — build recipes (depends on WTRL.Garage for the tuning-table
    // snapshot shape, so this type likely needs to live partly in
    // WTRL.Career instead if WTRL.RPG must stay Garage-independent per
    // the existing asmdef graph — open question below)
    public enum RecipeKind { FreeForm, Target }
    public sealed record BuildRecipe(
        string Name, RecipeKind Kind, /* tuning snapshot */ object TuningSnapshot,
        double? TargetWeightToPowerMin, double? TargetWeightToPowerMax,
        double? TargetMinDownforceN, string? RequiredDifferentialType,
        string? UnlockedTitle, string? UnlockedLiveryId);
    public static class BuildRecipeEvaluator {
        public static bool SatisfiesTarget(BuildRecipe recipe, /* resolved vehicle stats */ object current) { ... }
    }

    // Class brackets — genuinely a third, separate axis per spec S2.2
    public enum ClassBracketTier { Street, Club, SemiPro, Pro }
    public static class ClassBracket {
        public static ClassBracketTier BracketForPoints(int totalClassPoints) { ... }
        public static bool QualifiesForBracket(int totalClassPoints, ClassBracketTier eventBracket) { ... }
    }
}
```

`WTRL.Career` (which already depends on `WTRL.RPG` in the existing
asmdef graph) is where `ReputationState`/`SafetyRatingState`/
`DriverLicenseState`/`ClassBracket` actually get wired to real game
events (a race finishing, a shop job completing) — `WTRL.RPG` itself
should stay free of any dependency on `WTRL.Racing`/`WTRL.Events`/
`WTRL.Garage`, matching its current empty-dependency asmdef.
`BuildRecipe` is the one type here that's awkward: it fundamentally
needs a tuning-table snapshot shape that lives in `WTRL.Garage`, but
`WTRL.RPG`'s asmdef currently has no dependency on `WTRL.Garage`. See
open questions.

## Mobile-scoping calls

- **In v1**: reputation tiers + parts gating (Part 2, it's small and load-bearing for the whole economy), Safety Rating + license grade (Part 3, ditto — the professional-tier gate is a real career-structure beat), and free-form build recipes (simple save/load, high player value, low implementation cost).
- **Deferred**: the full 35-target-recipe ladder (the spec itself says ship a handful first), `ReputationTraits`' rival-AI-adaptation-from-player-traits (not in the spec I read — genuinely new scope, not a v1-vs-later call, see open questions), and `DiagnosticSkill`'s mentor-dialogue-shortening (same — needs `49-PLAYER-CHARACTER-RPG.md` read first).
- Nothing here reads as PC/console-scale RPG complexity needing to be cut down — this spec is already scoped tightly to mobile (bounded ratings, small enum-driven event tables, no branching dialogue trees, no skill webs). The Prototype~ code's `ReputationTraits`/`DiagnosticSkill` additions are the only places that could grow past mobile scope if built without a cap on the number of derived traits/thresholds.

## Open questions for the project owner

1. **`ReputationTraits.cs`'s rival-AI-adaptation** and **`DiagnosticSkill.cs`'s mentor-preemption** aren't in `48-RPG-SYSTEMS-SPEC.md`. Are they covered by `49-PLAYER-CHARACTER-RPG.md` or `50-ACTION-PILLAR-EXPANDED.md` (neither read this pass — out of this brief's assigned scope)? If so, read those before finalizing `WTRL.RPG`'s scope; if not, decide whether they're wanted at all before porting Prototype~ code that implements systems the shipped spec never asked for.
2. **Where does `BuildRecipe` actually live?** It needs `WTRL.Garage`'s tuning-table shape but `WTRL.RPG` has no dependency on `WTRL.Garage` in the current asmdef graph. Options: add the dependency (breaks the current "RPG depends on nothing but Core" cleanliness), put `BuildRecipe` in `WTRL.Career` instead (which already depends on both), or have `WTRL.Garage` define the recipe type itself and `WTRL.RPG` only defines the reward (title/livery unlock). Recommend the third — keeps `WTRL.RPG` about progression/gating, not about tuning-table mechanics.
3. **Exact reputation-tier point thresholds** — the spec gives no numbers, `Prototype~`'s 20/60/120 are explicitly unreasoned placeholders. Needs a playtesting pass, not a design decision made from documents alone.
