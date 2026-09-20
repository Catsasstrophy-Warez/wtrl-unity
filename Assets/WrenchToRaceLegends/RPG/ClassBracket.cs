namespace WTRL.RPG
{
    // New design from 48-RPG-SYSTEMS-SPEC.md S2.2's three-axis gating
    // table, structurally close to the archived Rev16.1 project's
    // Prototype~/RPG/PartsGating.cs `ClassBracket` (never-compiled
    // reference code). Point ceilings are the same unreasoned
    // placeholders that file used -- see CONTRACT.md.

    /// <summary>Which EVENTS a built car can enter, based on the car's own
    /// stats -- deliberately a different axis from
    /// <see cref="ReputationTier"/> (which parts a shop will sell) and
    /// property tier (whether parts/labour exist at a location at all).
    /// The spec (S2.2) is explicit these three must stay genuinely
    /// distinct or "the RPG layer collapses into one disguised
    /// meter."</summary>
    public enum ClassBracketTier { Street, Club, SemiPro, Pro }

    public static class ClassBracket
    {
        // Placeholder ceilings -- see CONTRACT.md for why these need a
        // playtesting pass, not a documents-only decision.
        public const int StreetCeiling = 15;
        public const int ClubCeiling = 35;
        public const int SemiProCeiling = 60;

        public static ClassBracketTier BracketForPoints(int totalClassPoints) =>
            totalClassPoints <= StreetCeiling ? ClassBracketTier.Street :
            totalClassPoints <= ClubCeiling ? ClassBracketTier.Club :
            totalClassPoints <= SemiProCeiling ? ClassBracketTier.SemiPro :
            ClassBracketTier.Pro;

        /// <summary>A build "qualifies" for an event's bracket only if it's
        /// AT or BELOW that bracket's ceiling -- a maxed-out build is too
        /// fast for a lower bracket, matching the spec's "the car's own
        /// stats" framing (this is a ceiling check, not an exact-tier
        /// match).</summary>
        public static bool QualifiesForBracket(int totalClassPoints, ClassBracketTier eventBracket) =>
            (int)BracketForPoints(totalClassPoints) <= (int)eventBracket;
    }
}
