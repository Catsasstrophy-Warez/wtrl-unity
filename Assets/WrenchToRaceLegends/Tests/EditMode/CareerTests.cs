using System.Collections.Generic;
using NUnit.Framework;
using WTRL.Career;
using WTRL.Garage;

namespace WTRL.Tests
{
    /// <summary>Ported from WTRLRecommendationTests.swift's
    /// testCareerTransactionIsAtomicOnFailure, plus new tests for each
    /// individual command (no direct Swift-test equivalent existed for
    /// most of these — the Swift test suite only exercised the atomicity
    /// property, not each command handler individually). Run via the
    /// same throwaway dotnet test project as every other assembly — all
    /// pass.</summary>
    public class CareerTests
    {
        [Test]
        public void CareerTransactionIsAtomicOnFailure()
        {
            var s = new CareerState { Money = 100 };
            var ok = CareerTransaction.Apply(new[] { CareerCommand.Spend(50), CareerCommand.Spend(1000) }, s);
            Assert.That(ok, Is.False);
            Assert.That(s.Money, Is.EqualTo(100));
        }

        [Test]
        public void SuccessfulBatchAppliesEveryCommand()
        {
            var s = new CareerState { Money = 100 };
            var ok = CareerTransaction.Apply(new[] { CareerCommand.Spend(30), CareerCommand.Earn(0, 5) }, s);
            Assert.That(ok, Is.True);
            Assert.That(s.Money, Is.EqualTo(70));
            Assert.That(s.Reputation, Is.EqualTo(5));
        }

        [Test]
        public void EarnAddsMoneyAndReputation()
        {
            var s = new CareerState { Money = 0, Reputation = 0 };
            CareerTransaction.Apply(new[] { CareerCommand.Earn(300, 3) }, s);
            Assert.That(s.Money, Is.EqualTo(300));
            Assert.That(s.Reputation, Is.EqualTo(3));
        }

        [Test]
        public void SpendingNegativeAmountFails()
        {
            var s = new CareerState { Money = 100 };
            var ok = CareerTransaction.Apply(new[] { CareerCommand.Spend(-10) }, s);
            Assert.That(ok, Is.False);
            Assert.That(s.Money, Is.EqualTo(100));
        }

        [Test]
        public void InstallRequiresPartToBeOwnedFirst()
        {
            var s = new CareerState();
            var component = new InstalledComponent { PartId = "sport-tire", Slot = "tires" };
            var okBeforeAcquire = CareerTransaction.Apply(new[] { CareerCommand.Install("hero-1965", component) }, s);
            Assert.That(okBeforeAcquire, Is.False);

            var okAfterAcquire = CareerTransaction.Apply(new[]
            {
                CareerCommand.AcquirePart("sport-tire"),
                CareerCommand.Install("hero-1965", component),
            }, s);
            Assert.That(okAfterAcquire, Is.True);
            Assert.That(s.InstalledComponents["hero-1965"], Has.Count.EqualTo(1));
        }

        [Test]
        public void InstallingIntoSameSlotReplacesThePreviousPart()
        {
            var s = new CareerState();
            CareerTransaction.Apply(new[]
            {
                CareerCommand.AcquirePart("street-tire"),
                CareerCommand.AcquirePart("sport-tire"),
                CareerCommand.Install("hero-1965", new InstalledComponent { PartId = "street-tire", Slot = "tires" }),
                CareerCommand.Install("hero-1965", new InstalledComponent { PartId = "sport-tire", Slot = "tires" }),
            }, s);

            var installed = s.InstalledComponents["hero-1965"];
            Assert.That(installed, Has.Count.EqualTo(1));
            Assert.That(installed[0].PartId, Is.EqualTo("sport-tire"));
        }

        [Test]
        public void CompleteRaceKeepsTheBestTime()
        {
            var s = new CareerState();
            CareerTransaction.Apply(new[] { CareerCommand.CompleteRace("club-circuit-01", 90.0) }, s);
            CareerTransaction.Apply(new[] { CareerCommand.CompleteRace("club-circuit-01", 85.0) }, s);
            CareerTransaction.Apply(new[] { CareerCommand.CompleteRace("club-circuit-01", 95.0) }, s);

            Assert.That(s.CompletedRaceIds, Does.Contain("club-circuit-01"));
            Assert.That(s.RaceRecords["club-circuit-01"], Is.EqualTo(85.0));
        }

        [Test]
        public void RecordHistoryAppendsToThatVehiclesLedger()
        {
            var s = new CareerState();
            var evt = new VehicleHistoryEvent("dyno", "Baseline pull");
            CareerTransaction.Apply(new[] { CareerCommand.RecordHistory("hero-1965", evt) }, s);

            Assert.That(s.VehicleHistory["hero-1965"], Has.Count.EqualTo(1));
            Assert.That(s.VehicleHistory["hero-1965"][0].Summary, Is.EqualTo("Baseline pull"));
        }

        [Test]
        public void CloneProducesIndependentCollections()
        {
            var s = new CareerState();
            s.OwnedPartIds.Add("sport-tire");
            var clone = s.Clone();
            clone.OwnedPartIds.Add("track-damper");
            Assert.That(s.OwnedPartIds, Does.Not.Contain("track-damper"));
        }
    }
}
