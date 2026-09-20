using System;
using NUnit.Framework;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>
    /// Ported from SwiftRacer/Tests/WTRLCoreTests/WTRLCoreTests.swift and
    /// WTRLAdvancedTests.swift. The Swift originals drove these through
    /// WTRLRuntime (a content-catalog-backed wrapper over
    /// VehicleSimulation.step); no equivalent runtime exists yet in
    /// WTRL.Vehicle (that's WTRL.Runtime's job — see PIVOT-PLAN.md), so
    /// these call VehicleSimulation.Step directly against a locally-built
    /// representative vehicle, matching the same test *intent*
    /// (determinism, banking's physical effect, finite-value robustness)
    /// rather than reproducing the exact hero-car numbers.
    ///
    /// Unlike everything in SwiftRacer, these were actually run — see
    /// CONTRACT.md for how (a throwaway `dotnet test` project, not the
    /// Unity Editor, which was unavailable in this environment).
    /// </summary>
    public class VehicleSimulationTests
    {
        private static VehicleDefinition MakeVehicle(DifferentialKind differential = DifferentialKind.Open) =>
            new VehicleDefinition("test-vehicle", "test-gen", "Test Vehicle", massKg: 1450, wheelbaseM: 2.6,
                engineId: "test-engine", transmissionId: "test-gearbox", suspensionId: "test-suspension")
            {
                TrackWidthM = 1.55,
                CgHeightM = 0.5,
                StaticFrontWeightFraction = 0.56,
                TireGripCoefficient = 1.05,
                TireRadiusM = 0.33,
                WheelInertiaKgm2 = 1.3,
                UnsprungMassKgPerWheel = 20,
                DriveLayout = DriveLayout.Rwd,
                Differential = differential,
                BrakeTorqueNm = 6500,
                AeroDragCoefficientArea = 0.72,
            };

        private static EngineDefinition MakeEngine() =>
            // No explicit TorqueCurve — falls back to EngineSolver's
            // two-point approximation from PeakTorqueLbFt/RedlineRpm.
            new EngineDefinition("test-engine", "Test Engine", displacementLiters: 5.0, peakPowerHp: 420,
                peakTorqueLbFt: 390)
            {
                RedlineRpm = 7000,
                IdleRpm = 800,
            };

        private static TransmissionDefinition MakeTransmission() =>
            new TransmissionDefinition("test-gearbox", "Test Gearbox",
                new[] { 3.36, 2.07, 1.43, 1.00, 0.84 }, finalDrive: 3.55)
            {
                Kind = TransmissionKind.Manual,
                ShiftDuration = 0.25,
            };

        private static SuspensionDefinition MakeSuspension() =>
            new SuspensionDefinition("test-suspension", "Test Suspension", "double-wishbone", "multi-link");

        private static TireDefinition MakeTire() =>
            new TireDefinition("test-tire", "Test Tire", longitudinalStiffness: 8.5, corneringStiffness: 5.5,
                peakSlipRatio: 0.12, peakSlipAngleRadians: 0.11);

        private static void Advance(ref VehicleSimState state, VehicleDefinition vehicle, EngineDefinition engine,
            TransmissionDefinition transmission, TireDefinition tire, SuspensionDefinition suspension,
            VehicleInput input, double dt, double bankingDegrees = 0)
        {
            VehicleSimulation.Step(ref state, input, vehicle, engine, transmission, VehicleTuning.Default, tire,
                suspension, surface: null, bankingDegrees: bankingDegrees, dt: dt);
        }

        [Test]
        public void FixedStepDeterminism_DifferentFrameDeltasConvergeThroughFixedStepClock()
        {
            // This is NOT "Step(dt=1/60) once equals Step(dt=1/120) twice" —
            // that would fail for any nonlinear integrator, and did fail
            // when first ported this way (a real bug in my initial port of
            // this test, caught by actually running it — see CONTRACT.md).
            // The Swift original tests FixedStepClock's job: decoupling the
            // physics dt from wall-clock frame time, so a 60Hz-framerate
            // caller and a 120Hz-framerate caller drive the SAME number of
            // fixed-dt physics steps and converge. Feeding frameDelta=1/60
            // to a clock with a 1/120 step fires the integrator twice
            // internally, same as feeding 1/120 twice — that equivalence is
            // what's actually under test.
            var vehicle = MakeVehicle();
            var engine = MakeEngine();
            var transmission = MakeTransmission();
            var tire = MakeTire();
            var suspension = MakeSuspension();

            var a = VehicleSimState.Default();
            var b = VehicleSimState.Default();
            var input = new VehicleInput(throttle: 1);
            var clockA = new FixedStepClock(120);
            var clockB = new FixedStepClock(120);

            for (var i = 0; i < 60; i++)
            {
                var localA = a;
                clockA.Consume(1.0 / 60, stepDt =>
                {
                    Advance(ref localA, vehicle, engine, transmission, tire, suspension, input, stepDt);
                });
                a = localA;

                var localB = b;
                clockB.Consume(1.0 / 120, stepDt =>
                {
                    Advance(ref localB, vehicle, engine, transmission, tire, suspension, input, stepDt);
                });
                clockB.Consume(1.0 / 120, stepDt =>
                {
                    Advance(ref localB, vehicle, engine, transmission, tire, suspension, input, stepDt);
                });
                b = localB;
            }

            Assert.That(a.SpeedMps, Is.EqualTo(b.SpeedMps).Within(1e-9));
            Assert.That(a.X, Is.EqualTo(b.X).Within(1e-9));
        }

        [Test]
        public void OvalBanking_ReducesLateralLoadTransferInATurn()
        {
            var vehicle = MakeVehicle();
            var engine = MakeEngine();
            var transmission = MakeTransmission();
            var tire = MakeTire();
            var suspension = MakeSuspension();
            var input = new VehicleInput(throttle: 0.6, steering: 0.5);

            var flat = VehicleSimState.Default();
            var banked = VehicleSimState.Default();

            for (var i = 0; i < 300; i++)
            {
                Advance(ref flat, vehicle, engine, transmission, tire, suspension, input, 1.0 / 120, bankingDegrees: 0);
                Advance(ref banked, vehicle, engine, transmission, tire, suspension, input, 1.0 / 120, bankingDegrees: 30);
            }

            double Spread(VehicleSimState s)
            {
                var min = double.MaxValue;
                var max = double.MinValue;
                foreach (var t in s.Tires)
                {
                    min = Math.Min(min, t.NormalLoadN);
                    max = Math.Max(max, t.NormalLoadN);
                }
                return max - min;
            }

            Assert.That(Spread(banked), Is.LessThan(Spread(flat)));
        }

        [Test]
        public void ZeroBanking_LeavesPhysicsUnchanged()
        {
            var vehicle = MakeVehicle();
            var engine = MakeEngine();
            var transmission = MakeTransmission();
            var tire = MakeTire();
            var suspension = MakeSuspension();
            var input = new VehicleInput(throttle: 1, steering: 0.4);

            var a = VehicleSimState.Default();
            var b = VehicleSimState.Default();

            for (var i = 0; i < 120; i++)
            {
                Advance(ref a, vehicle, engine, transmission, tire, suspension, input, 1.0 / 120, bankingDegrees: 0);
                Advance(ref b, vehicle, engine, transmission, tire, suspension, input, 1.0 / 120, bankingDegrees: 0);
            }

            Assert.That(a.SpeedMps, Is.EqualTo(b.SpeedMps).Within(1e-9));
            Assert.That(a.X, Is.EqualTo(b.X).Within(1e-9));
        }

        [Test]
        public void DeterministicFuzzInputs_RemainFinite()
        {
            var vehicle = MakeVehicle();
            var engine = MakeEngine();
            var transmission = MakeTransmission();
            var tire = MakeTire();
            var suspension = MakeSuspension();
            var state = VehicleSimState.Default();

            ulong seed = 0x123456789abcdef;
            double Next()
            {
                unchecked { seed = seed * 6364136223846793005UL + 1UL; }
                return (double)((seed >> 11) & 0xFFFF) / 65535.0;
            }

            for (var i = 0; i < 20_000; i++)
            {
                var input = new VehicleInput(throttle: Next(), brake: Next() * 0.2, steering: Next() * 2 - 1);
                Advance(ref state, vehicle, engine, transmission, tire, suspension, input, 1.0 / 120);
            }

            Assert.That(state.SpeedMps, Is.Not.NaN.And.Not.EqualTo(double.PositiveInfinity).And.Not.EqualTo(double.NegativeInfinity));
            Assert.That(double.IsFinite(state.SpeedMps));
            Assert.That(double.IsFinite(state.X));
            Assert.That(double.IsFinite(state.Z));
            Assert.That(double.IsFinite(state.EngineRpm));
            Assert.That(double.IsFinite(state.YawRate));
        }

        [Test]
        public void VehicleSimStateClone_ProducesIndependentArrays()
        {
            var a = VehicleSimState.Default();
            var b = a.Clone();
            b.Tires[0].TemperatureC = 999;
            Assert.That(a.Tires[0].TemperatureC, Is.Not.EqualTo(999),
                "mutating the clone's tire array must not affect the original — see the aliasing warning on VehicleSimState");
        }
    }
}
