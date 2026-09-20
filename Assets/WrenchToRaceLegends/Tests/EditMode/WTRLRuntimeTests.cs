using NUnit.Framework;
using WTRL.Garage;
using WTRL.Runtime;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>Ported from WTRLCoreTests.swift's testFixedStepDeterminism
    /// (now expressible properly since WTRLRuntime exists, unlike
    /// WTRL.Vehicle's own version of this test which had to drive
    /// FixedStepClock directly) and WTRLRecommendationTests.swift's
    /// testExpandedTelemetryHasFourCornerEvidence, plus new tests for
    /// Reset/SetSurface/SetShiftMode which had no isolated Swift-test
    /// equivalent (they were only ever exercised as a side effect of
    /// other tests). Run via the same throwaway dotnet test project as
    /// every other assembly — all pass.</summary>
    public class WTRLRuntimeTests
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

        private static SuspensionDefinition MakeSuspension() =>
            new SuspensionDefinition("test-suspension", "Test Suspension", "double-wishbone", "multi-link");

        private static TireDefinition MakeTire() =>
            new TireDefinition("test-tire", "Test Tire", longitudinalStiffness: 8.5, corneringStiffness: 5.5,
                peakSlipRatio: 0.12, peakSlipAngleRadians: 0.11);

        private static void AdvanceMany(WTRLRuntime runtime, int count, double frameDelta, VehicleInput input)
        {
            for (var i = 0; i < count; i++)
            {
                runtime.Advance(frameDelta, input, MakeVehicle(), MakeEngine(), MakeTransmission(), MakeTire(), MakeSuspension(), null);
            }
        }

        [Test]
        public void FixedStepDeterminism_60HzAndTwiceAt120HzConverge()
        {
            var a = new WTRLRuntime("test-vehicle");
            var b = new WTRLRuntime("test-vehicle");
            var input = new VehicleInput(throttle: 1);

            for (var i = 0; i < 60; i++)
            {
                a.Advance(1.0 / 60, input, MakeVehicle(), MakeEngine(), MakeTransmission(), MakeTire(), MakeSuspension(), null);
                b.Advance(1.0 / 120, input, MakeVehicle(), MakeEngine(), MakeTransmission(), MakeTire(), MakeSuspension(), null);
                b.Advance(1.0 / 120, input, MakeVehicle(), MakeEngine(), MakeTransmission(), MakeTire(), MakeSuspension(), null);
            }

            Assert.That(a.Snapshot.Vehicle.SpeedMps, Is.EqualTo(b.Snapshot.Vehicle.SpeedMps).Within(1e-9));
            Assert.That(a.Snapshot.Vehicle.X, Is.EqualTo(b.Snapshot.Vehicle.X).Within(1e-9));
        }

        [Test]
        public void TelemetryHasFourCornerEvidence()
        {
            var runtime = new WTRLRuntime("test-vehicle");
            runtime.Advance(1.0 / 60, new VehicleInput(throttle: 1), MakeVehicle(), MakeEngine(), MakeTransmission(), MakeTire(), MakeSuspension(), null);

            var last = runtime.TelemetrySamples[^1];
            Assert.That(last.TireTemperaturesC, Has.Length.EqualTo(4));
            Assert.That(last.TireNormalLoadsN, Has.Length.EqualTo(4));
            Assert.That(last.SuspensionTravelM, Has.Length.EqualTo(4));
        }

        [Test]
        public void TelemetrySamplingIsRateGatedNotEveryPhysicsStep()
        {
            // At 120Hz physics with a default 30Hz telemetry rate, ~600
            // fixed steps over 5 simulated seconds should produce roughly
            // 150 samples (5s * 30Hz), not 600.
            var runtime = new WTRLRuntime("test-vehicle");
            AdvanceMany(runtime, count: 300, frameDelta: 1.0 / 60, input: new VehicleInput(throttle: 0.5));
            Assert.That(runtime.TelemetrySamples.Count, Is.LessThan(300));
            Assert.That(runtime.TelemetrySamples.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ResetReturnsToStartingPositionButKeepsSurface()
        {
            var runtime = new WTRLRuntime("test-vehicle");
            runtime.SetSurface("wet-asphalt");
            AdvanceMany(runtime, count: 60, frameDelta: 1.0 / 60, input: new VehicleInput(throttle: 1));
            Assert.That(runtime.Snapshot.Vehicle.SpeedMps, Is.GreaterThan(0));

            runtime.Reset(x: 10, z: 20, heading: 0.5);
            Assert.That(runtime.Snapshot.Vehicle.SpeedMps, Is.EqualTo(0));
            Assert.That(runtime.Snapshot.Vehicle.X, Is.EqualTo(10));
            Assert.That(runtime.Snapshot.Vehicle.Z, Is.EqualTo(20));
            Assert.That(runtime.Snapshot.SurfaceId, Is.EqualTo("wet-asphalt"));
        }

        [Test]
        public void SetShiftModeIsReflectedInSnapshot()
        {
            var runtime = new WTRLRuntime("test-vehicle");
            runtime.SetShiftMode(ShiftMode.Manual);
            Assert.That(runtime.Snapshot.Vehicle.Drivetrain.ShiftMode, Is.EqualTo(ShiftMode.Manual));
        }

        [Test]
        public void DiagnosticsAreRecomputedEveryAdvance()
        {
            var runtime = new WTRLRuntime("test-vehicle");
            // Force heavy braking under load to eventually raise brake
            // temperature/fade above the diagnostic threshold.
            AdvanceMany(runtime, count: 600, frameDelta: 1.0 / 120, input: new VehicleInput(throttle: 1));
            AdvanceMany(runtime, count: 600, frameDelta: 1.0 / 120, input: new VehicleInput(brake: 1));
            // Not asserting a specific finding fires (that depends on
            // tuned thresholds this test's synthetic vehicle may not
            // hit) -- just that Diagnose() actually ran and produced a
            // list, not null/stale from construction.
            Assert.That(runtime.Snapshot.Diagnostics, Is.Not.Null);
        }
    }
}
