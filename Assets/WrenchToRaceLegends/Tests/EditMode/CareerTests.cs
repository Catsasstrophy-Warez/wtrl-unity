using System.Collections.Generic;
using NUnit.Framework;
using WTRL.Career;
using WTRL.Events;
using WTRL.Garage;
using WTRL.RPG;

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

        [Test]
        public void CloneDeepCopiesRivalAndRpgState()
        {
            // Regression test for the exact bug class CareerState.Clone()'s
            // own doc comment warns about: RivalBehavior/ReputationState/
            // SafetyRating/DriverLicense used to be reference-copied, which
            // was safe only because no command mutated them. Now that
            // RecordRaceOutcome does, a clone must be a REAL independent
            // copy, not the same object under a different variable name.
            var s = new CareerState();
            s.RivalBehavior.RecordResult("marsh", playerWon: true);
            s.ReputationState.RecordNamedRivalWin("marsh");
            s.SafetyRating.RecordEvent(SafetyEvent.PlayerCausedContact);

            var clone = s.Clone();
            clone.RivalBehavior.RecordResult("marsh", playerWon: true);
            clone.ReputationState.RecordNamedRivalWin("marsh");
            clone.SafetyRating.RecordEvent(SafetyEvent.PlayerCausedContact);

            Assert.That(s.RivalBehavior.Memory("marsh").LossesToPlayer, Is.EqualTo(1),
                "mutating the clone's RivalBehavior must not affect the original");
            Assert.That(clone.RivalBehavior.Memory("marsh").LossesToPlayer, Is.EqualTo(2));
            Assert.That(s.ReputationState.Points, Is.LessThan(clone.ReputationState.Points));
            Assert.That(s.SafetyRating.Rating, Is.GreaterThan(clone.SafetyRating.Rating));
        }

        [Test]
        public void RecordRaceOutcomeWithNamedRivalWinUpdatesReputationAndRivalMemory()
        {
            var s = new CareerState();
            var outcome = new RaceOutcomeDetail("club-circuit-01", 88.5, RaceFormat.Circuit,
                rivalId: "marsh", playerWon: true);

            var ok = CareerTransaction.Apply(new[] { CareerCommand.RecordRaceOutcome(outcome) }, s);

            Assert.That(ok, Is.True);
            Assert.That(s.CompletedRaceIds, Does.Contain("club-circuit-01"));
            Assert.That(s.RaceRecords["club-circuit-01"], Is.EqualTo(88.5));
            Assert.That(s.RivalBehavior.Memory("marsh").LossesToPlayer, Is.EqualTo(1));
            Assert.That(s.ReputationState.Points, Is.GreaterThan(0));
        }

        [Test]
        public void RecordRaceOutcomeWithContactDamagesSafetyRating()
        {
            var s = new CareerState();
            var outcome = new RaceOutcomeDetail("club-circuit-01", 90, RaceFormat.Circuit,
                playerCausedContact: true);

            CareerTransaction.Apply(new[] { CareerCommand.RecordRaceOutcome(outcome) }, s);

            Assert.That(s.SafetyRating.Rating, Is.EqualTo(92)); // 100 - 8
        }

        [Test]
        public void RecordRaceOutcomeWithNoIncidentsCreditsCleanRun()
        {
            // Starting rating is already 100 (max) -- EventCompletedZeroIncidents's
            // +3 has nothing to add, so this asserts the credit was applied
            // and clamped, not skipped. See the next test for a case where
            // the credit is visible.
            var s = new CareerState();
            var outcome = new RaceOutcomeDetail("club-circuit-01", 90, RaceFormat.Circuit);

            CareerTransaction.Apply(new[] { CareerCommand.RecordRaceOutcome(outcome) }, s);

            Assert.That(s.SafetyRating.Rating, Is.EqualTo(100));
        }

        [Test]
        public void RecordRaceOutcomeWithNoIncidentsRecoversRatingFromEarlierContact()
        {
            var s = new CareerState();
            s.SafetyRating.RecordEvent(SafetyEvent.PlayerCausedContact); // 100 -> 92
            var outcome = new RaceOutcomeDetail("club-circuit-01", 90, RaceFormat.Circuit);

            CareerTransaction.Apply(new[] { CareerCommand.RecordRaceOutcome(outcome) }, s);

            Assert.That(s.SafetyRating.Rating, Is.EqualTo(95)); // 92 + 3
        }

        [Test]
        public void RecordRaceOutcomeIsAtomicOnFailureAlongsideOtherCommands()
        {
            // The real point of CloneDeepCopiesRivalAndRpgState above: a
            // failing Spend in the same batch must leave RivalBehavior/
            // ReputationState/SafetyRating completely untouched too, not
            // just Money.
            var s = new CareerState { Money = 10 };
            var outcome = new RaceOutcomeDetail("club-circuit-01", 90, RaceFormat.Circuit,
                rivalId: "marsh", playerWon: true, playerCausedContact: true);

            var ok = CareerTransaction.Apply(new[]
            {
                CareerCommand.RecordRaceOutcome(outcome),
                CareerCommand.Spend(999), // fails: insufficient funds
            }, s);

            Assert.That(ok, Is.False);
            Assert.That(s.CompletedRaceIds, Does.Not.Contain("club-circuit-01"));
            Assert.That(s.RivalBehavior.Memory("marsh").LossesToPlayer, Is.EqualTo(0));
            Assert.That(s.ReputationState.Points, Is.EqualTo(0));
            Assert.That(s.SafetyRating.Rating, Is.EqualTo(100));
        }

        [Test]
        public void RaceSessionCountdownThroughFinishBuildsAValidOutcomeCommand()
        {
            var race = new RaceDefinition("club-circuit-01", "Club Circuit", "foundry-row-circuit", laps: 2,
                reputationRequired: 0) { Format = RaceFormat.Circuit };
            var session = new RaceSession(race);

            session.BeginCountdown(3);
            session.Advance(3); // exhausts countdown, auto-starts
            Assert.That(session.State.Phase, Is.EqualTo(RacePhase.Running));

            session.Advance(45);
            session.CompleteLap();
            Assert.That(session.IsFinished, Is.False);

            session.Advance(44);
            session.CompleteLap();
            Assert.That(session.IsFinished, Is.True);

            var command = session.BuildOutcomeCommand(playerWon: true, rivalId: "marsh");
            var s = new CareerState();
            var ok = CareerTransaction.Apply(new[] { command }, s);

            Assert.That(ok, Is.True);
            Assert.That(s.CompletedRaceIds, Does.Contain("club-circuit-01"));
            Assert.That(s.RaceRecords["club-circuit-01"], Is.EqualTo(session.State.ClassifiedTime));
        }

        [Test]
        public void RaceSessionThrowsIfOutcomeRequestedBeforeFinish()
        {
            var race = new RaceDefinition("club-circuit-01", "Club Circuit", "foundry-row-circuit", laps: 2,
                reputationRequired: 0);
            var session = new RaceSession(race);

            Assert.Throws<System.InvalidOperationException>(() => session.BuildOutcomeCommand(playerWon: true));
        }
    }
}
