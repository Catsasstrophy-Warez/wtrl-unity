using System;
using WTRL.Vehicle;

namespace WTRL.Racing
{
    // Ported from SwiftRacer/Sources/WTRLCore/Simulation/TrackAI.swift.

    public struct DriverPerception
    {
        public int NodeIndex;
        public double DistanceToNodeM;
        public double HeadingErrorRadians;
        public double TargetSpeedMps;
    }

    public static class TrackAiDriver
    {
        public static DriverPerception? Perceive(VehicleSimState state, TrackLineDefinition line, int lastNode = 0)
        {
            if (line.Nodes.Count == 0) return null;

            var nearest = lastNode;
            var bestDistance = double.MaxValue;
            for (var i = 0; i < line.Nodes.Count; i++)
            {
                var d = Math.Sqrt(Math.Pow(line.Nodes[i].X - state.X, 2) + Math.Pow(line.Nodes[i].Z - state.Z, 2));
                if (d < bestDistance)
                {
                    bestDistance = d;
                    nearest = i;
                }
            }

            var targetIndex = (nearest + 1) % line.Nodes.Count;
            var n = line.Nodes[targetIndex];
            var dx = n.X - state.X;
            var dz = n.Z - state.Z;
            var desired = Math.Atan2(-dx, -dz);
            var error = desired - state.HeadingRadians;
            while (error > Math.PI) error -= 2 * Math.PI;
            while (error < -Math.PI) error += 2 * Math.PI;

            return new DriverPerception
            {
                NodeIndex = targetIndex,
                DistanceToNodeM = Math.Sqrt(dx * dx + dz * dz),
                HeadingErrorRadians = error,
                TargetSpeedMps = n.TargetSpeedMps,
            };
        }

        public static VehicleInput Input(DriverModel model, DriverPerception perception, double currentSpeed)
        {
            var steering = Math.Max(-1, Math.Min(1, perception.HeadingErrorRadians / 0.55));
            return DriverController.Input(model, currentSpeed, perception.TargetSpeedMps, steering);
        }

        /// <summary>Real-time application of a rival's earned
        /// <see cref="RivalIntimidationState"/>. Deliberately minimal — no
        /// per-rival "attempting a pass" decision system exists, so
        /// intimidation is approximated from simple alongside/ahead/behind
        /// facts about the player rather than a real overtake state
        /// machine.</summary>
        public readonly struct PlayerProximity
        {
            /// <summary>True when the player is close enough (longitudinally
            /// and laterally) that "alongside" intimidation effects apply.</summary>
            public readonly bool IsAlongside;
            /// <summary>True when the rival is immediately behind the
            /// player — the context in which <c>PassAttemptSuppression</c>
            /// (abandoning a pass it would otherwise make) applies.</summary>
            public readonly bool RivalIsBehindPlayer;

            public PlayerProximity(bool isAlongside, bool rivalIsBehindPlayer)
            {
                IsAlongside = isAlongside;
                RivalIsBehindPlayer = rivalIsBehindPlayer;
            }
        }

        /// <summary>Both samples are required, explicit parameters — this
        /// mirrors a real determinism fix made in the Swift source, where
        /// this overload used to default one sample to a live RNG call
        /// (see WTRL.Vehicle's own determinism discipline for why that's a
        /// real bug class in a fixed-step sim). Callers must supply
        /// deterministic values, e.g. from <see cref="RivalDeterministicSample"/>,
        /// never from a live RNG.</summary>
        public static VehicleInput Input(DriverModel model, DriverPerception perception, double currentSpeed,
            RivalIntimidationState intimidation, PlayerProximity proximity, double defensiveSample, double suppressionSample01)
        {
            var targetSpeed = perception.TargetSpeedMps;
            // brakePointBias: "metres earlier a rival begins braking when the
            // player is alongside." Approximated as an early speed reduction
            // ahead of the next node, scaled by proximity to it.
            if (proximity.IsAlongside && intimidation.BrakePointBiasM > 0)
            {
                var proximityToCorner = Math.Max(0, 1 - perception.DistanceToNodeM / 40);
                var speedCut = intimidation.BrakePointBiasM / RivalIntimidation.MaxBrakePointBiasM;
                targetSpeed *= 1 - 0.35 * speedCut * proximityToCorner;
            }

            var steeringError = perception.HeadingErrorRadians / 0.55;
            var steering = Math.Max(-1, Math.Min(1, steeringError));
            // defensivePositionError: lateral jitter on the blocking line,
            // approximated as noise on the steering demand itself.
            if (intimidation.DefensivePositionErrorM > 0)
            {
                var jitterFraction = intimidation.DefensivePositionErrorM / RivalIntimidation.MaxDefensivePositionErrorM;
                var boundedSample = Math.Max(-1, Math.Min(1, defensiveSample));
                steering = Math.Max(-1, Math.Min(1, steering + 0.3 * boundedSample * jitterFraction));
            }

            var input = DriverController.Input(model, currentSpeed, targetSpeed, steering);

            // passAttemptSuppression: probability the rival abandons a pass
            // attempt it would otherwise make — only meaningful when the
            // rival is trailing the player.
            if (proximity.RivalIsBehindPlayer && intimidation.PassAttemptSuppression > 0 &&
                Math.Max(0, Math.Min(1, suppressionSample01)) < intimidation.PassAttemptSuppression)
            {
                input.Throttle *= 0.55;
            }

            return input;
        }
    }
}
