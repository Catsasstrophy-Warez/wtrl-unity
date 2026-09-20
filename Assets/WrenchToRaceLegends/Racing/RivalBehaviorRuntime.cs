using System.Collections.Generic;
using System.Text;

namespace WTRL.Racing
{
    // Ported from SwiftRacer/Sources/WTRLCore/Runtime/WorldConsequences.swift
    // (RivalMemory) and RivalBehaviorRuntime.swift.

    public struct RivalMemory
    {
        public int Encounters;
        public int WinsAgainstPlayer;
        public int LossesToPlayer;
        public double Pressure;

        public void Record(bool playerWon)
        {
            Encounters += 1;
            if (playerWon) LossesToPlayer += 1; else WinsAgainstPlayer += 1;
            Pressure = System.Math.Max(0, System.Math.Min(1, (LossesToPlayer - WinsAgainstPlayer) * 0.08 + 0.5));
        }
    }

    public struct RivalBehaviorSnapshot
    {
        public Dictionary<string, RivalMemory> Memories;
        public Dictionary<string, RivalIntimidationState> Intimidation;
    }

    /// <summary>Canonical owner of persistent rival history and its derived
    /// intimidation state. Intimidation is never persisted independently —
    /// it is always recomputed from <see cref="RivalMemory"/>, so the two
    /// truths cannot drift apart. Ported as a mutable class rather than
    /// Swift's mutating-struct pattern, since C# consumers (e.g. a
    /// MonoBehaviour-owned career service) will hold and mutate a single
    /// long-lived instance rather than copy-on-write value semantics.</summary>
    public sealed class RivalBehaviorRuntime
    {
        private readonly Dictionary<string, RivalMemory> _memories = new();

        public void Restore(IReadOnlyDictionary<string, RivalMemory> memories)
        {
            _memories.Clear();
            foreach (var kv in memories) _memories[kv.Key] = kv.Value;
        }

        public void RecordResult(string rivalId, bool playerWon)
        {
            var memory = _memories.TryGetValue(rivalId, out var existing) ? existing : new RivalMemory();
            memory.Record(playerWon);
            _memories[rivalId] = memory;
        }

        public RivalMemory Memory(string rivalId) =>
            _memories.TryGetValue(rivalId, out var m) ? m : new RivalMemory();

        public RivalIntimidationState Intimidation(string rivalId, bool witnessedDirtyRacing = false,
            bool toldTruth = false, bool isEnduranceFormat = false)
        {
            if (!RivalIntimidation.Ceilings.TryGetValue(rivalId, out var ceilings)) return new RivalIntimidationState();
            return RivalIntimidation.Update(ceilings, Memory(rivalId).LossesToPlayer, witnessedDirtyRacing, toldTruth, isEnduranceFormat);
        }

        public RivalBehaviorSnapshot Snapshot()
        {
            var intimidation = new Dictionary<string, RivalIntimidationState>();
            foreach (var rivalId in RivalIntimidation.Ceilings.Keys) intimidation[rivalId] = Intimidation(rivalId);
            return new RivalBehaviorSnapshot { Memories = new Dictionary<string, RivalMemory>(_memories), Intimidation = intimidation };
        }

        /// <summary>Deep copy of the raw memory dictionary (intimidation is
        /// always derived, never copied). Added so `WTRL.Career
        /// .CareerTransaction` can clone-mutate-commit this state
        /// atomically alongside `CareerState`'s other fields, instead of
        /// the reference-copy `CareerState.Clone()` used before any
        /// command actually mutated this type mid-transaction.</summary>
        public RivalBehaviorRuntime Clone()
        {
            var copy = new RivalBehaviorRuntime();
            copy.Restore(_memories);
            return copy;
        }
    }

    /// <summary>Stable FNV-1a derived sample. Inputs are explicit simulation
    /// facts (rival id, sim tick, channel name) — never wall-clock time or
    /// global RNG state, so this stays reproducible inside a deterministic
    /// fixed-step sim. See WTRL.Vehicle's own determinism discipline
    /// (FixedStepClock) for why this matters.</summary>
    public static class RivalDeterministicSample
    {
        private static ulong Hash(string text)
        {
            ulong h = 1469598103934665603UL;
            foreach (var b in Encoding.UTF8.GetBytes(text))
            {
                h ^= b;
                unchecked { h *= 1099511628211UL; }
            }
            return h;
        }

        public static double Unit(string rivalId, ulong tick, string channel) =>
            (double)(Hash($"{rivalId}|{tick}|{channel}") & 0x1fffffffffffffUL) / 0x1fffffffffffffUL;

        public static double Signed(string rivalId, ulong tick, string channel) =>
            Unit(rivalId, tick, channel) * 2 - 1;
    }
}
