using System.Collections.Generic;
using NUnit.Framework;
using WTRL.Garage;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>Ported from WTRLCoreTests.swift's
    /// testInstalledFinalDriveChangesConfiguration/testSportTireChangesPhysicalGrip
    /// and WTRLAdvancedTests.swift's testInstalledDamperChangesSpringRates —
    /// against a locally-built representative vehicle rather than the real
    /// "hero-1965"/"hero-mid90s" catalog entries, since no content catalog
    /// exists in this assembly yet (same reasoning as WTRL.Vehicle's own
    /// ported tests). Run via the same throwaway dotnet test project as
    /// WTRL.Vehicle/WTRL.Racing — all pass.</summary>
    public class VehicleConfigurationResolverTests
    {
        private static VehicleDefinition MakeVehicle() =>
            new VehicleDefinition("test-vehicle", "test-gen", "Test Vehicle", massKg: 1450, wheelbaseM: 2.6,
                engineId: "test-engine", transmissionId: "test-gearbox", suspensionId: "test-suspension")
            {
                TireGripCoefficient = 1.0,
            };

        private static TransmissionDefinition MakeTransmission() =>
            new TransmissionDefinition("test-gearbox", "Test Gearbox", new[] { 3.36, 2.07, 1.43, 1.00, 0.84 },
                finalDrive: 3.08);

        private static SuspensionDefinition MakeSuspension() =>
            new SuspensionDefinition("test-suspension", "Test Suspension", "double-wishbone", "multi-link")
            {
                FrontSpringRate = 35_000,
                RearSpringRate = 32_000,
            };

        [Test]
        public void NoComponentsLeavesBaseConfigurationUnchanged()
        {
            var resolved = VehicleConfigurationResolver.Resolve(MakeVehicle(), MakeTransmission(), MakeSuspension(),
                new List<InstalledComponent>());
            Assert.That(resolved.Vehicle.TireGripCoefficient, Is.EqualTo(1.0));
            Assert.That(resolved.Transmission.FinalDrive, Is.EqualTo(3.08));
        }

        [Test]
        public void InstalledFinalDriveChangesConfiguration()
        {
            var baseConfig = VehicleConfigurationResolver.Resolve(MakeVehicle(), MakeTransmission(), MakeSuspension(),
                new List<InstalledComponent>());
            var modified = VehicleConfigurationResolver.Resolve(MakeVehicle(), MakeTransmission(), MakeSuspension(),
                new List<InstalledComponent> { new InstalledComponent { PartId = "final-drive-373", Slot = "finalDrive" } });

            Assert.That(modified.Transmission.FinalDrive, Is.Not.EqualTo(baseConfig.Transmission.FinalDrive));
            Assert.That(modified.Transmission.FinalDrive, Is.EqualTo(3.73));
        }

        [Test]
        public void SportTireChangesPhysicalGrip()
        {
            var baseConfig = VehicleConfigurationResolver.Resolve(MakeVehicle(), MakeTransmission(), MakeSuspension(),
                new List<InstalledComponent>());
            var modified = VehicleConfigurationResolver.Resolve(MakeVehicle(), MakeTransmission(), MakeSuspension(),
                new List<InstalledComponent> { new InstalledComponent { PartId = "sport-tire", Slot = "tires" } });

            Assert.That(modified.Vehicle.TireGripCoefficient, Is.GreaterThan(baseConfig.Vehicle.TireGripCoefficient));
        }

        [Test]
        public void InstalledDamperChangesSpringRates()
        {
            var a = VehicleConfigurationResolver.Resolve(MakeVehicle(), MakeTransmission(), MakeSuspension(),
                new List<InstalledComponent>());
            var b = VehicleConfigurationResolver.Resolve(MakeVehicle(), MakeTransmission(), MakeSuspension(),
                new List<InstalledComponent> { new InstalledComponent { PartId = "track-damper", Slot = "suspension" } });

            Assert.That(b.Suspension!.FrontSpringRate, Is.GreaterThan(a.Suspension!.FrontSpringRate));
        }

        [Test]
        public void ResolvingDoesNotMutateTheOriginalDefinitions()
        {
            // Records + `with` should mean the base VehicleDefinition/
            // TransmissionDefinition instances passed in are never mutated
            // in place — a real risk this test guards against, since a
            // mutable-class version of this resolver would corrupt
            // whatever else holds a reference to the same canonical
            // definition object.
            var vehicle = MakeVehicle();
            var transmission = MakeTransmission();
            VehicleConfigurationResolver.Resolve(vehicle, transmission, MakeSuspension(),
                new List<InstalledComponent> { new InstalledComponent { PartId = "sport-tire", Slot = "tires" } });

            Assert.That(vehicle.TireGripCoefficient, Is.EqualTo(1.0));
            Assert.That(transmission.FinalDrive, Is.EqualTo(3.08));
        }
    }
}
