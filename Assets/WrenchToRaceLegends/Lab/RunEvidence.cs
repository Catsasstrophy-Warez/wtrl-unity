using System;
using System.Collections.Generic;
using System.Linq;
using WTRL.Garage;
using WTRL.Vehicle;

namespace WTRL.Lab
{
    // Ported from SwiftRacer/Sources/WTRLCore/Runtime/RunEvidence.swift.

    /// <summary>Stable FNV-1a fingerprint of a vehicle configuration —
    /// deliberately excludes anything that doesn't affect behavior (part
    /// installation order doesn't matter; components are sorted by
    /// slot/id first), so two configurations that are mechanically
    /// identical produce the same fingerprint.</summary>
    public readonly struct VehicleConfigurationFingerprint : IEquatable<VehicleConfigurationFingerprint>
    {
        public readonly string Value;

        public VehicleConfigurationFingerprint(string vehicleId, TransmissionDefinition transmission,
            IReadOnlyList<InstalledComponent> components, VehicleTuning tuning)
        {
            var parts = string.Join("|", components
                .OrderBy(c => c.Slot).ThenBy(c => c.PartId)
                .Select(c => $"{c.Slot}={c.PartId}"));
            var raw = $"{vehicleId}|{transmission.Id}|{transmission.FinalDrive}|{parts}|{tuning.BrakeBiasFront}|{tuning.DifferentialLock}|{tuning.TirePressureGripFactor}";

            ulong hash = 1469598103934665603UL;
            foreach (var b in System.Text.Encoding.UTF8.GetBytes(raw))
            {
                hash ^= b;
                unchecked { hash *= 1099511628211UL; }
            }
            Value = hash.ToString("x16");
        }

        public bool Equals(VehicleConfigurationFingerprint other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is VehicleConfigurationFingerprint other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public enum EvidenceKind { Dyno, Drag, Circuit, RoadTest }

    public sealed class TestRunEvidence
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public EvidenceKind Kind { get; }
        public string VehicleId { get; }
        public string ConfigurationFingerprint { get; }
        public IReadOnlyDictionary<string, double> Metrics { get; }
        public IReadOnlyList<RuntimeTelemetrySample> Telemetry { get; init; } = Array.Empty<RuntimeTelemetrySample>();

        // Constructor-enforced required fields, not the C# 11 `required`
        // keyword -- see WTRL.RPG/BuildRecipeProgress.cs's identical note
        // for why.
        public TestRunEvidence(EvidenceKind kind, string vehicleId, string configurationFingerprint,
            IReadOnlyDictionary<string, double> metrics)
        {
            Kind = kind;
            VehicleId = vehicleId;
            ConfigurationFingerprint = configurationFingerprint;
            Metrics = metrics;
        }
    }

    public sealed class RunComparison
    {
        public string BaselineId { get; }
        public string CandidateId { get; }
        public IReadOnlyDictionary<string, double> MetricDelta { get; }

        public RunComparison(TestRunEvidence baseline, TestRunEvidence candidate)
        {
            BaselineId = baseline.Id;
            CandidateId = candidate.Id;
            var delta = new Dictionary<string, double>();
            foreach (var key in baseline.Metrics.Keys.Union(candidate.Metrics.Keys))
            {
                var baseValue = baseline.Metrics.TryGetValue(key, out var bv) ? bv : 0;
                var candidateValue = candidate.Metrics.TryGetValue(key, out var cv) ? cv : 0;
                delta[key] = candidateValue - baseValue;
            }
            MetricDelta = delta;
        }
    }
}
