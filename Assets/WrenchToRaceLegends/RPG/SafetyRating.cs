using System;

namespace WTRL.RPG
{
    // New design from 48-RPG-SYSTEMS-SPEC.md Part 3, structurally close
    // to the archived Rev16.1 project's Prototype~/RPG/DriverProgression.cs
    // (never-compiled reference code) whose SafetyEvent enum already
    // matched the spec's table term-for-term. See
    // Assignments/OUTPUT-RPG-Design.md for the full reasoning.

    /// <summary>Per 48-RPG-SYSTEMS-SPEC.md S3.2's table exactly. Deliberately
    /// does NOT include anything about the instability/ragged-edge meter —
    /// the spec (S3.4) is explicit that losing grip at the limit is
    /// physics, not conduct, and must never move this rating. Only contact
    /// and deliberate track-limit abuse do.</summary>
    public enum SafetyEvent
    {
        PlayerCausedContact,
        OffTrackCutForAdvantage,
        CausedRivalSpinOrRetire,
        CleanOvertakeNoContact,
        EventCompletedZeroIncidents,
        DefensiveHoldNoContact,
    }

    /// <summary>Tracks *how* results were achieved, independent of the pace
    /// rating (which is derived elsewhere, from event results themselves).
    /// Deliberately bounded [0,100], not an accumulating score — S3.2:
    /// "bounded metrics communicate 'where do I stand' far better than an
    /// accumulating number with no ceiling."</summary>
    public sealed class SafetyRatingState
    {
        public double Rating { get; private set; } = 100; // starts clean

        public void RecordEvent(SafetyEvent evt)
        {
            var delta = evt switch
            {
                SafetyEvent.PlayerCausedContact => -8,
                SafetyEvent.OffTrackCutForAdvantage => -5,
                // "Decreases sharply" per the spec's own wording -- the
                // single worst thing a player can do to this rating.
                SafetyEvent.CausedRivalSpinOrRetire => -15,
                SafetyEvent.CleanOvertakeNoContact => 1,
                SafetyEvent.EventCompletedZeroIncidents => 3,
                SafetyEvent.DefensiveHoldNoContact => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(evt), evt, null),
            };
            Rating = Math.Max(0, Math.Min(100, Rating + delta));
        }

        public bool WasResultClean(double cleanThreshold = 85) => Rating >= cleanThreshold;

        /// <summary>Deep copy -- see <see cref="ReputationState.Clone"/>'s
        /// doc comment for why this exists (CareerTransaction atomicity
        /// once a command actually mutates this type mid-transaction).</summary>
        public SafetyRatingState Clone() => new() { Rating = Rating };
    }

    public enum DriverLicenseGrade { Provisional, Club, Contender, Licensed }

    /// <summary>Per 48-RPG-SYSTEMS-SPEC.md S3.3: at the street tier, a low
    /// Safety Rating only softly suppresses reputation gain (not modeled
    /// here -- that's ReputationState's job to apply). From the
    /// professional licence onward, a minimum Safety Rating is required to
    /// ENTER knockout events specifically -- not a punishment screen, a
    /// locked entry list.</summary>
    public sealed class DriverLicenseState
    {
        public DriverLicenseGrade Grade { get; private set; } = DriverLicenseGrade.Provisional;

        /// <summary>Minimum Safety Rating required to enter a knockout
        /// event once licensed at Contender grade or above. Below
        /// Contender, no minimum applies -- the street tier's aggression
        /// economy (20-CONCEPTS.md S10) makes contact a viable, costed
        /// strategy there, not a locked-out one.</summary>
        public double MinimumSafetyRatingForKnockoutEntry { get; init; } = 70;

        public bool PermitsKnockoutEntry(SafetyRatingState safety)
        {
            if (Grade == DriverLicenseGrade.Provisional || Grade == DriverLicenseGrade.Club) return true;
            return safety.Rating >= MinimumSafetyRatingForKnockoutEntry;
        }

        public void Promote(DriverLicenseGrade grade) => Grade = grade;

        /// <summary>Deep copy -- see <see cref="ReputationState.Clone"/>'s
        /// doc comment for why this exists.</summary>
        public DriverLicenseState Clone() => new() { Grade = Grade, MinimumSafetyRatingForKnockoutEntry = MinimumSafetyRatingForKnockoutEntry };
    }
}
