using System;
using System.Collections.Generic;

namespace WTRL.World
{
    /// <summary>
    /// Ported from the DESIGN (not the code — see below) of
    /// SwiftRacer/Sources/RacingGame/Rendering/WorldStreaming.swift, a
    /// Wave24 RealityKit-specific implementation that was never wired
    /// into anything and is flagged in SwiftRacer's own
    /// `BUILD-READINESS.md` as unused. Its deterministic cell-grid and
    /// seed math are real and worth keeping; its RealityKit `Entity`/
    /// scene-graph parts (`WorldStreamingController.update`,
    /// `BlackridgeCellFactory`, `RepeatedWorldGeometry`) are not portable
    /// and not ported — this is exactly the split PIVOT-PLAN.md's content
    /// porting map called out ("this is genuinely reusable thinking, not
    /// reusable code").
    ///
    /// <see cref="WorldStreamingGrid"/> is therefore presentation-agnostic:
    /// it tracks which cell IDs SHOULD be active around a position and
    /// reports load/unload deltas, but never touches a Unity scene,
    /// GameObject, or asset. WTRL.Runtime is where a MonoBehaviour wraps
    /// this and actually instantiates/destroys scene content per cell.
    /// </summary>
    public readonly struct WorldCellId : IEquatable<WorldCellId>
    {
        public readonly int X;
        public readonly int Z;

        public WorldCellId(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(WorldCellId other) => X == other.X && Z == other.Z;
        public override bool Equals(object? obj) => obj is WorldCellId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Z);
        public override string ToString() => $"({X},{Z})";
    }

    /// <summary>Stable per-cell deterministic seed/sample generation —
    /// term-for-term port of the Swift splitmix64-style hash. Two cells
    /// with the same coordinates always produce the same seed, so
    /// unloading and reloading a cell reconstructs identical content
    /// (trees, poles, barriers in the Swift original's usage) rather than
    /// randomizing it each time.</summary>
    public static class StableWorldSeed
    {
        public static ulong Value(WorldCellId id, ulong salt)
        {
            unchecked
            {
                var x = (ulong)(long)id.X * 0x9E3779B185EBCA87UL;
                x ^= (ulong)(long)id.Z * 0xC2B2AE3D27D4EB4FUL;
                x ^= salt;
                x ^= x >> 30;
                x *= 0xBF58476D1CE4E5B9UL;
                x ^= x >> 27;
                x *= 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        /// <summary>KNOWN DEVIATION from the Swift source: Swift's <c>Int</c>
        /// is 64-bit, so <c>Int(truncatingIfNeeded: seed)</c> there is a
        /// full 64-bit bit-pattern reinterpretation, not a truncation.
        /// <see cref="WorldCellId.X"/> here is a 32-bit <c>int</c> (the
        /// natural width for a grid-cell coordinate), so this cast DOES
        /// truncate to the low 32 bits — a different (but still
        /// deterministic and still uniformly distributed) derived value
        /// than the Swift original would produce for the same inputs.
        /// This is a cosmetic prop-scattering utility, not a physics or
        /// gameplay-critical value, so exact cross-language numerical
        /// parity wasn't worth widening <see cref="WorldCellId"/> to
        /// <c>long</c> for. If something ever needs bit-for-bit parity
        /// with the Swift value, this is where to fix it.</summary>
        public static float Unit(ulong seed, int i)
        {
            var v = Value(new WorldCellId(unchecked((int)seed), i), 0xD6E8FEB86659FD93UL);
            return (v & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }

    /// <summary>Tracks which <see cref="WorldCellId"/>s should be active
    /// (loaded) around a position, and reports the delta from the
    /// previous update as explicit load/unload lists — the caller applies
    /// those to whatever scene-loading mechanism it uses (Addressables,
    /// direct instantiation, etc.), which this type deliberately knows
    /// nothing about.</summary>
    public sealed class WorldStreamingGrid
    {
        public float CellSize { get; }
        public int ActiveRadius { get; }
        private readonly HashSet<WorldCellId> _loaded = new();

        public WorldStreamingGrid(float cellSize = 160, int activeRadius = 2)
        {
            CellSize = cellSize;
            ActiveRadius = activeRadius;
        }

        public IReadOnlyCollection<WorldCellId> LoadedCells => _loaded;

        public WorldCellId CellAt(float x, float z) =>
            new WorldCellId((int)Math.Floor(x / CellSize), (int)Math.Floor(z / CellSize));

        public (IReadOnlyList<WorldCellId> ToLoad, IReadOnlyList<WorldCellId> ToUnload) Update(float playerX, float playerZ)
        {
            var center = CellAt(playerX, playerZ);
            var wanted = new HashSet<WorldCellId>();
            for (var dz = -ActiveRadius; dz <= ActiveRadius; dz++)
            {
                for (var dx = -ActiveRadius; dx <= ActiveRadius; dx++)
                {
                    wanted.Add(new WorldCellId(center.X + dx, center.Z + dz));
                }
            }

            var toUnload = new List<WorldCellId>();
            foreach (var id in _loaded)
            {
                if (!wanted.Contains(id)) toUnload.Add(id);
            }
            foreach (var id in toUnload) _loaded.Remove(id);

            var toLoad = new List<WorldCellId>();
            foreach (var id in wanted)
            {
                if (!_loaded.Contains(id))
                {
                    toLoad.Add(id);
                    _loaded.Add(id);
                }
            }

            return (toLoad, toUnload);
        }
    }
}
