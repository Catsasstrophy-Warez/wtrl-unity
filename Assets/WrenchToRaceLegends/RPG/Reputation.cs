using System;

namespace WTRL.RPG
{
    // New design, not a Swift port -- no prior WTRLCore implementation
    // exists for the RPG layer. Designed from
    // racinggame/05-specifications/48-RPG-SYSTEMS-SPEC.md Part 2, with
    // ReputationTier's four names and shape adopted from the archived
    // Rev16.1 project's Prototype~/RPG/PartsGating.cs (never-compiled
    // reference code -- see Assignments/OUTPUT-RPG-Design.md for the full
    // reasoning). Point thresholds are explicitly unreasoned placeholders
    // inherited from that same reference code; they need a playtesting
    // pass, not a documents-only design decision -- see this file's
    // CONTRACT.md.

    public enum ReputationTier { Unknown, Known, Respected, Trusted }

    /// <summary>What can move reputation, per 48-RPG-SYSTEMS-SPEC.md
    /// S2.3: named-rival wins (diminishing on repeat wins against an
    /// already-beaten rival), shop-driver job completion, and touge duel
    /// wins specifically weighted higher than outrun wins since format
    /// skill there is build-independent.</summary>
    public enum ReputationEvent { NamedRivalWin, ShopDriverJobCompleted, TougeDuelWin, OutrunWin }

    /// <summary>Deliberately separate from <see cref="Vehicle.VehicleTuning"/>-
    /// adjacent concerns and from <c>ClassBracket</c>/property-tier
    /// gating — the spec (S2.2) is explicit that collapsing these three
    /// axes into one meter defeats the entire point of the system.
    /// Determines what a parts catalogue *shows*, independent of
    /// money.</summary>
    public sealed class ReputationState
    {
        // Placeholder thresholds inherited from Rev16.1's Prototype~ code
        // (itself explicitly unreasoned there) -- see CONTRACT.md.
        public double KnownThreshold { get; init; } = 20;
        public double RespectedThreshold { get; init; } = 60;
        public double TrustedThreshold { get; init; } = 120;

        public double Points { get; private set; }

        /// <summary>How many times each named rival has been beaten, so a
        /// repeat win against an already-beaten rival diminishes rather
        /// than repeating the same reward (S2.3: "there's nothing left to
        /// prove against someone already beaten").</summary>
        private readonly System.Collections.Generic.Dictionary<string, int> _rivalWinCounts = new();

        public ReputationTier Tier =>
            Points >= TrustedThreshold ? ReputationTier.Trusted :
            Points >= RespectedThreshold ? ReputationTier.Respected :
            Points >= KnownThreshold ? ReputationTier.Known :
            ReputationTier.Unknown;

        public void RecordNamedRivalWin(string rivalId, double baseValue = 10)
        {
            var priorWins = _rivalWinCounts.TryGetValue(rivalId, out var count) ? count : 0;
            // Diminishing returns: each subsequent win against the same
            // rival is worth progressively less, floored so it never
            // reaches zero (a win is still a win) but never rewards
            // grinding the same easy rival as richly as a first win.
            var diminished = baseValue / (1 + priorWins * 0.5);
            Points += Math.Max(baseValue * 0.1, diminished);
            _rivalWinCounts[rivalId] = priorWins + 1;
        }

        public void RecordEvent(ReputationEvent evt, double baseValue = 0)
        {
            switch (evt)
            {
                case ReputationEvent.ShopDriverJobCompleted:
                    Points += baseValue > 0 ? baseValue : 5;
                    break;
                case ReputationEvent.TougeDuelWin:
                    // Weighted higher than a plain outrun win -- format
                    // skill there is independent of the build (S2.3).
                    Points += baseValue > 0 ? baseValue : 8;
                    break;
                case ReputationEvent.OutrunWin:
                    Points += baseValue > 0 ? baseValue : 4;
                    break;
                case ReputationEvent.NamedRivalWin:
                    throw new InvalidOperationException(
                        $"Use {nameof(RecordNamedRivalWin)} for {nameof(ReputationEvent.NamedRivalWin)} -- it needs the rival's id for diminishing returns.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(evt), evt, null);
            }
        }
    }
}
