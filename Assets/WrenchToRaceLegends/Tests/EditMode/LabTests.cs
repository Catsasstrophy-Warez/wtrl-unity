using System.Collections.Generic;
using NUnit.Framework;
using WTRL.Garage;
using WTRL.Lab;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>Ported from WTRLAdvancedTests.swift's testDynoIsDeterministic
    /// and WTRLRecommendationTests.swift's
    /// testConfigurationFingerprintChangesWithPart, plus new tests for
    /// RuntimeTelemetryRing (no equivalent existed to port — the Swift
    /// tests exercised it only indirectly through WTRLRuntime, which isn't
    /// ported yet). Run via the same throwaway dotnet test project as
    /// every other assembly so far — all pass.</summary>
    public class LabTests
    {
        private static VehicleDefinition MakeVehicle() =>
            new VehicleDefinition("test-vehicle", "test-gen", "Test Vehicle", massKg: 1450, wheelbaseM: 2.6,
                engineId: "test-engine", transmissionId: "test-gearbox", suspensionId: "test-suspension");

        private static EngineDefinition MakeEngine() =>
            new EngineDefinition("test-engine", "Test Engine", displacementLiters: 5.0, peakPowerHp: 420, peakTorqueLbFt: 390)
            {
                RedlineRpm = 7000,
                IdleRpm = 800,
            };

        private static TransmissionDefinition MakeTransmission() =>
            new TransmissionDefinition("test-gearbox", "Test Gearbox", new[] { 3.36, 2.07, 1.43, 1.00, 0.84 }, finalDrive: 3.55);

        [Test]
        public void DynoIsDeterministic()
        {
            var config = new DynoConfiguration("test-vehicle");
            var a = DynoSimulation.Run(MakeVehicle(), MakeTransmission(), null, MakeEngine(), config);
            var b = DynoSimulation.Run(MakeVehicle(), MakeTransmission(), null, MakeEngine(), config);

            Assert.That(a.PeakPowerKw, Is.EqualTo(b.PeakPowerKw));
            Assert.That(a.PeakTorqueNm, Is.EqualTo(b.PeakTorqueNm));
            Assert.That(a.Samples.Count, Is.EqualTo(b.Samples.Count));
            for (var i = 0; i < a.Samples.Count; i++)
            {
                Assert.That(a.Samples[i].Rpm, Is.EqualTo(b.Samples[i].Rpm));
                Assert.That(a.Samples[i].WheelTorqueNm, Is.EqualTo(b.Samples[i].WheelTorqueNm));
            }
        }

        [Test]
        public void ConfigurationFingerprintChangesWithPart()
        {
            var transmission = MakeTransmission();
            var a = new VehicleConfigurationFingerprint("test-vehicle", transmission, System.Array.Empty<InstalledComponent>(), VehicleTuning.Default);
            var b = new VehicleConfigurationFingerprint("test-vehicle", transmission,
                new List<InstalledComponent> { new InstalledComponent { PartId = "sport-tire", Slot = "tire" } }, VehicleTuning.Default);

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void ConfigurationFingerprintIsOrderIndependent()
        {
            var transmission = MakeTransmission();
            var a = new VehicleConfigurationFingerprint("test-vehicle", transmission,
                new List<InstalledComponent>
                {
                    new InstalledComponent { PartId = "sport-tire", Slot = "tire" },
                    new InstalledComponent { PartId = "final-drive-373", Slot = "finalDrive" },
                }, VehicleTuning.Default);
            var b = new VehicleConfigurationFingerprint("test-vehicle", transmission,
                new List<InstalledComponent>
                {
                    new InstalledComponent { PartId = "final-drive-373", Slot = "finalDrive" },
                    new InstalledComponent { PartId = "sport-tire", Slot = "tire" },
                }, VehicleTuning.Default);

            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void TelemetryRingReturnsSamplesInChronologicalOrderAfterWraparound()
        {
            var ring = new RuntimeTelemetryRing(capacity: 3);
            for (var i = 0; i < 5; i++)
            {
                ring.Append(new RuntimeTelemetrySample { SimulationSeconds = i });
            }
            var samples = ring.Samples;
            Assert.That(samples.Count, Is.EqualTo(3));
            Assert.That(samples[0].SimulationSeconds, Is.EqualTo(2));
            Assert.That(samples[1].SimulationSeconds, Is.EqualTo(3));
            Assert.That(samples[2].SimulationSeconds, Is.EqualTo(4));
        }

        [Test]
        public void TelemetryRingBeforeFullReturnsOnlyWhatWasAppended()
        {
            var ring = new RuntimeTelemetryRing(capacity: 10);
            ring.Append(new RuntimeTelemetrySample { SimulationSeconds = 1 });
            ring.Append(new RuntimeTelemetrySample { SimulationSeconds = 2 });
            Assert.That(ring.Samples.Count, Is.EqualTo(2));
        }

        [Test]
        public void RunComparisonComputesMetricDeltaAcrossUnionOfKeys()
        {
            var baseline = new TestRunEvidence
            {
                Kind = EvidenceKind.Dyno,
                VehicleId = "test-vehicle",
                ConfigurationFingerprint = "abc",
                Metrics = new Dictionary<string, double> { ["power"] = 100 },
            };
            var candidate = new TestRunEvidence
            {
                Kind = EvidenceKind.Dyno,
                VehicleId = "test-vehicle",
                ConfigurationFingerprint = "def",
                Metrics = new Dictionary<string, double> { ["power"] = 120, ["torque"] = 50 },
            };
            var comparison = new RunComparison(baseline, candidate);

            Assert.That(comparison.MetricDelta["power"], Is.EqualTo(20));
            Assert.That(comparison.MetricDelta["torque"], Is.EqualTo(50));
        }
    }
}
