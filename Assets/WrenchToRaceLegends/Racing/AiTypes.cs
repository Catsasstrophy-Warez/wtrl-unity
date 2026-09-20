using System.Collections.Generic;
using WTRL.Vehicle;

namespace WTRL.Racing
{
    // Ported from SwiftRacer/Sources/WTRLCore/Content/Definitions.swift
    // (DriverModel) and AdvancedDefinitions.swift (TrackNode,
    // TrackLineDefinition), plus DriverController.swift.

    public struct DriverModel
    {
        public double Aggression;
        public double Consistency;
        public double BrakingConfidence;
        public double ThrottleDiscipline;
        public double WetSkill;
        public double TireConservation;
        public double MechanicalSympathy;
        public double MistakeProbability;
    }

    public readonly struct TrackNode
    {
        public readonly double X;
        public readonly double Z;
        public readonly double TargetSpeedMps;
        public readonly double BrakingWeight;
        public readonly double PassingWidthM;

        public TrackNode(double x, double z, double targetSpeedMps, double brakingWeight = 0, double passingWidthM = 6)
        {
            X = x;
            Z = z;
            TargetSpeedMps = targetSpeedMps;
            BrakingWeight = brakingWeight;
            PassingWidthM = passingWidthM;
        }
    }

    public sealed class TrackLineDefinition
    {
        public string Id { get; }
        public string TrackId { get; init; }
        public IReadOnlyList<TrackNode> Nodes { get; init; }

        public TrackLineDefinition(string id, string trackId, IReadOnlyList<TrackNode> nodes)
        {
            Id = id;
            TrackId = trackId;
            Nodes = nodes;
        }
    }

    public static class DriverController
    {
        public static VehicleInput Input(DriverModel model, double currentSpeed, double targetSpeed, double steeringDemand = 0)
        {
            var error = targetSpeed - currentSpeed;
            var throttle = System.Math.Max(0, System.Math.Min(1, error / 8)) * model.ThrottleDiscipline
                           + System.Math.Max(0, error / 30) * model.Aggression;
            var brake = System.Math.Max(0, System.Math.Min(1, -error / 7)) * model.BrakingConfidence;
            return new VehicleInput(
                throttle: System.Math.Min(1, throttle),
                brake: brake,
                steering: System.Math.Max(-1, System.Math.Min(1, steeringDemand * model.Consistency)));
        }
    }
}
