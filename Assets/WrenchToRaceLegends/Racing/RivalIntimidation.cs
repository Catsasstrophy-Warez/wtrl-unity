using System;
using System.Collections.Generic;

namespace WTRL.Racing
{
    // Ported from SwiftRacer/Sources/WTRLCore/Simulation/RivalIntimidation.swift.
    // Every ceiling value below traces to 52-CONTENT-RESOLUTION-PASS-1.md
    // Part 1 / 45-RIVAL-DEVELOPMENT.md Part 1 in the research corpus —
    // see the per-rival comments, carried over verbatim from the Swift
    // source. These are real, sourced values, not placeholders.

    public readonly struct RivalIntimidationCeilings
    {
        public readonly double BrakePointBiasCeilingM;
        public readonly double PassAttemptSuppressionCeiling;
        public readonly double DefensivePositionErrorCeilingM;
        public readonly double LaunchReactionDelayCeilingMs;
        public readonly int OverallRampLosses;
        public readonly int? DefensivePositionErrorRampLossesOverride;
        public readonly bool HasEnduranceSpecificBehaviour;
        public readonly double ShortEventCeilingMultiplier;

        public RivalIntimidationCeilings(double brakePointBiasCeilingM, double passAttemptSuppressionCeiling,
            double defensivePositionErrorCeilingM, double launchReactionDelayCeilingMs, int overallRampLosses,
            int? defensivePositionErrorRampLossesOverride = null, bool hasEnduranceSpecificBehaviour = false,
            double shortEventCeilingMultiplier = 1.0)
        {
            BrakePointBiasCeilingM = brakePointBiasCeilingM;
            PassAttemptSuppressionCeiling = passAttemptSuppressionCeiling;
            DefensivePositionErrorCeilingM = defensivePositionErrorCeilingM;
            LaunchReactionDelayCeilingMs = launchReactionDelayCeilingMs;
            OverallRampLosses = overallRampLosses;
            DefensivePositionErrorRampLossesOverride = defensivePositionErrorRampLossesOverride;
            HasEnduranceSpecificBehaviour = hasEnduranceSpecificBehaviour;
            ShortEventCeilingMultiplier = shortEventCeilingMultiplier;
        }
    }

    public struct RivalIntimidationState : IEquatable<RivalIntimidationState>
    {
        public double BrakePointBiasM;
        public double PassAttemptSuppression;
        public double DefensivePositionErrorM;
        public double LaunchReactionDelayMs;

        public bool Equals(RivalIntimidationState other) =>
            BrakePointBiasM.Equals(other.BrakePointBiasM) && PassAttemptSuppression.Equals(other.PassAttemptSuppression) &&
            DefensivePositionErrorM.Equals(other.DefensivePositionErrorM) && LaunchReactionDelayMs.Equals(other.LaunchReactionDelayMs);
        public override bool Equals(object? obj) => obj is RivalIntimidationState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(BrakePointBiasM, PassAttemptSuppression, DefensivePositionErrorM, LaunchReactionDelayMs);
    }

    public static class RivalIntimidation
    {
        public const double MaxBrakePointBiasM = 15.0;
        public const double MaxDefensivePositionErrorM = 2.5;

        public static readonly IReadOnlyDictionary<string, RivalIntimidationCeilings> Ceilings =
            new Dictionary<string, RivalIntimidationCeilings>
            {
                // "High brakePointBias ceiling specifically in technical sections —
                // the one place his philosophy fails him" / "low ceiling on
                // passAttemptSuppression even at high reputation" (45 §1.1).
                ["reyes"] = new RivalIntimidationCeilings(
                    0.75 * MaxBrakePointBiasM, 0.25, 0.4 * MaxDefensivePositionErrorM, 120, 10),
                // "High launchReactionDelay ceiling — he's the rival most rattled at
                // the line" / "low defensivePositionError even under pressure" (45 §1.2).
                ["kade"] = new RivalIntimidationCeilings(
                    0.4 * MaxBrakePointBiasM, 0.4, 0.2 * MaxDefensivePositionErrorM, 280, 8),
                // "defensivePositionError rises fastest of all six" once beaten on a
                // bumpy circuit (45 §1.3) — hence the override below.
                ["vogel"] = new RivalIntimidationCeilings(
                    0.4 * MaxBrakePointBiasM, 0.4, 0.85 * MaxDefensivePositionErrorM, 120, 10,
                    defensivePositionErrorRampLossesOverride: 4),
                // "Near-zero passAttemptSuppression ceiling, ever" (the most extreme
                // single value in the whole table) / "final appearances show the
                // highest brakePointBias of his own arc" (45 §1.4).
                ["duquesne"] = new RivalIntimidationCeilings(
                    0.65 * MaxBrakePointBiasM, 0.05, 0.4 * MaxDefensivePositionErrorM, 120, 6),
                // "The most conditional parameter set of the six" — near-zero in
                // short events, rising sharply in endurance formats (45 §1.5).
                ["osei"] = new RivalIntimidationCeilings(
                    0.6 * MaxBrakePointBiasM, 0.6, 0.6 * MaxDefensivePositionErrorM, 0.6 * 300, 12,
                    hasEnduranceSpecificBehaviour: true, shortEventCeilingMultiplier: 0.1),
                // "Rises slowest of all six," uniformly — his real advantage is trail
                // braking (actual physics), not AI-assisted intimidation (45 §1.6).
                ["marsh"] = new RivalIntimidationCeilings(
                    0.15 * MaxBrakePointBiasM, 0.15, 0.15 * MaxDefensivePositionErrorM, 0.15 * 300, 30),
            };

        /// <summary>Recomputes a rival's earned intimidation state from
        /// scratch each call — see the Swift source's doc comment for why
        /// (avoids the earned state and its inputs ever drifting out of
        /// sync).</summary>
        public static RivalIntimidationState Update(RivalIntimidationCeilings ceilings, int lossesToRival,
            bool witnessedDirtyRacing = false, bool toldTruth = false, bool isEnduranceFormat = false)
        {
            var formatMultiplier = ceilings.HasEnduranceSpecificBehaviour && !isEnduranceFormat
                ? ceilings.ShortEventCeilingMultiplier
                : 1.0;

            double Earned(double ceiling, int rampLosses)
            {
                if (rampLosses <= 0 || lossesToRival <= 0) return 0;
                var fraction = Math.Min(1.0, (double)lossesToRival / rampLosses);
                return fraction * ceiling * formatMultiplier;
            }

            var state = new RivalIntimidationState
            {
                BrakePointBiasM = Earned(ceilings.BrakePointBiasCeilingM, ceilings.OverallRampLosses),
                LaunchReactionDelayMs = Earned(ceilings.LaunchReactionDelayCeilingMs, ceilings.OverallRampLosses),
            };

            var suppression = Earned(ceilings.PassAttemptSuppressionCeiling, ceilings.OverallRampLosses);
            if (witnessedDirtyRacing) suppression *= 0.5;
            state.PassAttemptSuppression = Math.Max(0, Math.Min(ceilings.PassAttemptSuppressionCeiling, suppression));

            var defensiveRamp = ceilings.DefensivePositionErrorRampLossesOverride ?? ceilings.OverallRampLosses;
            var defensiveError = Earned(ceilings.DefensivePositionErrorCeilingM, defensiveRamp);
            if (toldTruth) defensiveError += 0.1 * ceilings.DefensivePositionErrorCeilingM;
            state.DefensivePositionErrorM = Math.Max(0, Math.Min(ceilings.DefensivePositionErrorCeilingM * 1.1, defensiveError));

            return state;
        }
    }
}
