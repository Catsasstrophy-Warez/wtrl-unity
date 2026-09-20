using NUnit.Framework;
using WTRL.Career;
using WTRL.Garage;
using WTRL.Persistence;

namespace WTRL.Tests
{
    /// <summary>New tests for WTRL.Persistence — no direct Swift-test
    /// equivalent to port (SwiftRacer's CareerSave/CareerSaveCodec tests
    /// exercised real hero-1965-catalog scenarios this port doesn't have
    /// a catalog for; these instead assert the same behavioral
    /// guarantees the Swift source's own comments describe: lenient
    /// decoding, the hero-gen1 rename, and the owned-vehicle safety
    /// invariant). Run via the same throwaway dotnet test project as
    /// every other assembly — all pass.</summary>
    public class PersistenceTests
    {
        [Test]
        public void RoundTripPreservesCoreFields()
        {
            var state = new CareerState { Money = 1234, Reputation = 56, LicensePassed = true };
            state.OwnedPartIds.Add("sport-tire");
            state.CompletedRaceIds.Add("club-circuit-01");
            state.RaceRecords["club-circuit-01"] = 88.5;

            var json = CareerSaveCodec.Encode(state);
            var decoded = CareerSaveCodec.Decode(json);

            Assert.That(decoded.Money, Is.EqualTo(1234));
            Assert.That(decoded.Reputation, Is.EqualTo(56));
            Assert.That(decoded.LicensePassed, Is.True);
            Assert.That(decoded.OwnedPartIds, Does.Contain("sport-tire"));
            Assert.That(decoded.CompletedRaceIds, Does.Contain("club-circuit-01"));
            Assert.That(decoded.RaceRecords["club-circuit-01"], Is.EqualTo(88.5));
        }

        [Test]
        public void RoundTripPreservesInstalledComponentsAndVehicleHistory()
        {
            var state = new CareerState();
            state.InstalledComponents["hero-1965"] = new()
            {
                new InstalledComponent { PartId = "sport-tire", Slot = "tires" },
            };
            state.VehicleHistory["hero-1965"] = new()
            {
                new VehicleHistoryEvent { Kind = "dyno", Summary = "Baseline pull" },
            };

            var decoded = CareerSaveCodec.Decode(CareerSaveCodec.Encode(state));

            Assert.That(decoded.InstalledComponents["hero-1965"][0].PartId, Is.EqualTo("sport-tire"));
            Assert.That(decoded.VehicleHistory["hero-1965"][0].Summary, Is.EqualTo("Baseline pull"));
        }

        [Test]
        public void RoundTripPreservesRivalMemoryThroughRivalBehaviorRuntime()
        {
            var state = new CareerState();
            state.RivalBehavior.RecordResult("kade", playerWon: true);
            state.RivalBehavior.RecordResult("kade", playerWon: true);

            var decoded = CareerSaveCodec.Decode(CareerSaveCodec.Encode(state));

            Assert.That(decoded.RivalBehavior.Memory("kade").LossesToPlayer, Is.EqualTo(2));
            // The derived intimidation state must also come back correctly
            // -- it's recomputed from memory, never persisted separately.
            Assert.That(decoded.RivalBehavior.Intimidation("kade").LaunchReactionDelayMs, Is.GreaterThan(0));
        }

        [Test]
        public void LegacyHeroGen1VehicleIdIsRenamedOnDecode()
        {
            var json = """{"SelectedVehicleId":"hero-gen1"}""";
            var decoded = CareerSaveCodec.Decode(json);
            Assert.That(decoded.SelectedVehicleId, Is.EqualTo("hero-1965"));
        }

        [Test]
        public void SelectedVehicleIsAlwaysInOwnedSetEvenForMinimalSaveShapes()
        {
            // An empty JSON object -- the leniency contract's whole point:
            // every field falls back to a sensible default.
            var decoded = CareerSaveCodec.Decode("{}");
            Assert.That(decoded.OwnedVehicleIds, Does.Contain(decoded.SelectedVehicleId));
        }

        [Test]
        public void DecodingGarbageJsonFallsBackToDefaultsRatherThanThrowing()
        {
            var decoded = CareerSaveCodec.Decode("not valid json at all");
            Assert.That(decoded.Money, Is.EqualTo(5000));
            Assert.That(decoded.SelectedVehicleId, Is.EqualTo("hero-1965"));
        }

        [Test]
        public void DynoSliderValuesRoundTrip()
        {
            var state = new CareerState { DynoFinalDrive = 0.8, DynoTirePressure = 0.2, DynoNitrous = 0.9 };
            var decoded = CareerSaveCodec.Decode(CareerSaveCodec.Encode(state));
            Assert.That(decoded.DynoFinalDrive, Is.EqualTo(0.8));
            Assert.That(decoded.DynoTirePressure, Is.EqualTo(0.2));
            Assert.That(decoded.DynoNitrous, Is.EqualTo(0.9));
        }
    }
}
