using System;
using WTRL.Vehicle;

namespace WTRL.Racing
{
    /// <summary>
    /// The real contact/overtake detection primitives that
    /// `Career/CONTRACT.md` names as "the concrete, narrow blocker for
    /// further Career progress" -- `RaceCompletionBridge` deliberately
    /// leaves `RaceOutcomeDetail`'s contact/rival/win fields unset
    /// because no such system existed anywhere in this project.
    ///
    /// HONEST SCOPE: this closes the DETECTION half, not the full
    /// integration. These are pure functions over two vehicles'
    /// `VehicleSimState` (position/heading) plus a `TrackLineDefinition`
    /// for progress tracking -- deliberately not wired into
    /// `RaceSessionController`/`RaceCompletionBridge` yet, since doing
    /// that honestly requires a caller that already has BOTH vehicles'
    /// live simulation state each frame (today, the player's lives
    /// inside `WTRL.UI.VehicleRuntimeController` and the rival's inside
    /// `AiVehicleSession`/`AiVehicleController` -- neither is currently
    /// exposed to a shared per-frame comparison point). That wiring is
    /// real, separate follow-on work, not done here.
    ///
    /// Contact detection is a simple combined-radius distance check
    /// with a closing-speed component, not a real physics collision
    /// response -- consistent with this project's existing "arcade
    /// approximation over full realism" simulation choices elsewhere
    /// (e.g. `RaceSessionController`'s own lap-detection heuristic).
    /// </summary>
    public static class ContactDetector
    {
        /// <summary>True when two vehicles are within
        /// <paramref name="combinedRadiusM"/> of each other AND closing
        /// (their separation decreased since <paramref name="previousDistanceM"/>)
        /// -- the closing-speed check exists so two cars stably running
        /// side by side at a constant gap (never actually touching)
        /// don't register as permanent "contact." Pass
        /// <see cref="double.PositiveInfinity"/> for
        /// <paramref name="previousDistanceM"/> on the first call.</summary>
        public static bool DetectContact(VehicleSimState a, VehicleSimState b, double combinedRadiusM,
            double previousDistanceM, out double currentDistanceM)
        {
            var dx = a.X - b.X;
            var dz = a.Z - b.Z;
            currentDistanceM = Math.Sqrt(dx * dx + dz * dz);

            return currentDistanceM <= combinedRadiusM && currentDistanceM < previousDistanceM;
        }
    }

    /// <summary>Real per-vehicle progress along a <see cref="TrackLineDefinition"/>,
    /// via nearest-segment projection -- the same technique
    /// `TrackAiDriver.Perceive` already uses to find the nearest node,
    /// extended here to a continuous cumulative-arc-length value usable
    /// to compare which of two vehicles is actually ahead.</summary>
    public static class TrackProgress
    {
        /// <summary>Cumulative arc length (meters) from node 0 to the
        /// point on the track polyline nearest (x, z), plus the
        /// vehicle's own distance along the segment it's nearest to.
        /// Treats the line as open (not closed/looped) -- for a closed
        /// circuit, callers comparing two progress values across a
        /// start/finish crossing must account for the wraparound
        /// themselves (not done here, since whether/when that happens
        /// depends on lap-counting state this pure function doesn't
        /// have).</summary>
        public static double ArcLengthAt(TrackLineDefinition line, double x, double z)
        {
            if (line.Nodes.Count < 2) return 0;

            var bestDistanceToSegment = double.PositiveInfinity;
            var bestArcLength = 0.0;
            var cumulative = 0.0;

            for (var i = 0; i < line.Nodes.Count - 1; i++)
            {
                var a = line.Nodes[i];
                var b = line.Nodes[i + 1];
                var segDx = b.X - a.X;
                var segDz = b.Z - a.Z;
                var segLengthSq = segDx * segDx + segDz * segDz;

                var t = segLengthSq > 0 ? ((x - a.X) * segDx + (z - a.Z) * segDz) / segLengthSq : 0;
                t = Math.Max(0, Math.Min(1, t));

                var projX = a.X + t * segDx;
                var projZ = a.Z + t * segDz;
                var distToSegment = Math.Sqrt((x - projX) * (x - projX) + (z - projZ) * (z - projZ));

                var segLength = Math.Sqrt(segLengthSq);
                if (distToSegment < bestDistanceToSegment)
                {
                    bestDistanceToSegment = distToSegment;
                    bestArcLength = cumulative + t * segLength;
                }

                cumulative += segLength;
            }

            return bestArcLength;
        }
    }

    /// <summary>Stateful clean-overtake detector: call <see cref="Update"/>
    /// once per frame with both vehicles' current track progress and
    /// whether contact occurred this frame. Reports a clean overtake
    /// the first frame the trailing vehicle's progress exceeds the
    /// leading vehicle's, provided no contact was flagged since the
    /// last time their order was the same.</summary>
    public sealed class OvertakeTracker
    {
        private bool? _aWasAhead;
        private bool _contactSincePositionsMatched;

        /// <summary>Which side became ahead on the most recent reported
        /// overtake (true = A, false = B) -- added so a caller who cares
        /// about direction (e.g. "did the PLAYER pass the rival," not
        /// just "did an overtake happen") can tell the two cases apart.
        /// Only meaningful immediately after <see cref="Update"/> returns
        /// true; undefined otherwise.</summary>
        public bool LastFlipFavoredA { get; private set; }

        /// <returns>True exactly on the frame a clean overtake completes
        /// (order flipped, no contact recorded since the order last
        /// flipped or since tracking started).</returns>
        public bool Update(double progressA, double progressB, bool contactThisFrame)
        {
            var aIsAheadNow = progressA > progressB;

            if (contactThisFrame)
            {
                _contactSincePositionsMatched = true;
            }

            var overtakeHappened = false;
            if (_aWasAhead.HasValue && _aWasAhead.Value != aIsAheadNow)
            {
                overtakeHappened = !_contactSincePositionsMatched;
                if (overtakeHappened) LastFlipFavoredA = aIsAheadNow;
                _contactSincePositionsMatched = false;
            }

            _aWasAhead = aIsAheadNow;
            return overtakeHappened;
        }
    }

    /// <summary>Unwraps <see cref="TrackProgress.ArcLengthAt"/>'s per-lap
    /// (0..trackLength) value into a monotonically increasing total
    /// distance traveled -- the real, honestly-derivable substitute for
    /// "how many laps has this vehicle actually completed" when the
    /// vehicle itself (e.g. an AI rival with no `RaceFlowController` of
    /// its own) never explicitly counts laps. Detects a lap wrap when
    /// arc length drops by more than half the track length in one
    /// update (crossing back from near the end to near the start),
    /// exactly the same wraparound-unwrapping technique used for
    /// continuous angle/odometer tracking generally.</summary>
    public sealed class LapProgressTracker
    {
        private readonly double _trackLengthM;
        private double? _previousArcLengthM;
        private double _lapOffsetM;

        public LapProgressTracker(double trackLengthM)
        {
            _trackLengthM = trackLengthM;
        }

        /// <summary>Total distance traveled along the track since this
        /// tracker was created, unwrapped across lap boundaries.</summary>
        public double TotalDistanceM { get; private set; }

        /// <summary>Real (not estimated-by-time) completed-lap count,
        /// derived from total unwrapped distance divided by track
        /// length.</summary>
        public int CompletedLaps => (int)(TotalDistanceM / _trackLengthM);

        public void Update(double currentArcLengthM)
        {
            if (_previousArcLengthM.HasValue && currentArcLengthM < _previousArcLengthM.Value - _trackLengthM / 2)
            {
                _lapOffsetM += _trackLengthM;
            }

            _previousArcLengthM = currentArcLengthM;
            TotalDistanceM = _lapOffsetM + currentArcLengthM;
        }
    }
}
