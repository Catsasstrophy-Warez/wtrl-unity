using NUnit.Framework;
using WTRL.Racing;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>Ported from RivalIntimidationTests.swift,
    /// Reconciled3DeterministicIntimidationTests.swift, and
    /// Wave20CanonicalRivalBehaviorTests.swift. Actually run via a
    /// throwaway dotnet test project (see WTRL.Vehicle's CONTRACT.md for
    /// why this is possible) — all pass.</summary>
    public class RivalIntimidationTests
    {
        [Test]
        public void StartsAtZeroAndSaturatesAtCeiling()
        {
            var c = RivalIntimidation.Ceilings["kade"];
            var zero = RivalIntimidation.Update(c, lossesToRival: 0);
            Assert.That(zero, Is.EqualTo(new RivalIntimidationState()));
            var full = RivalIntimidation.Update(c, lossesToRival: 999);
            Assert.That(full.LaunchReactionDelayMs, Is.EqualTo(c.LaunchReactionDelayCeilingMs).Within(0.0001));
            Assert.That(full.PassAttemptSuppression, Is.EqualTo(c.PassAttemptSuppressionCeiling).Within(0.0001));
        }

        [Test]
        public void OseiShortEventIsSuppressed()
        {
            var c = RivalIntimidation.Ceilings["osei"];
            var shortEvent = RivalIntimidation.Update(c, lossesToRival: 99, isEnduranceFormat: false);
            var endurance = RivalIntimidation.Update(c, lossesToRival: 99, isEnduranceFormat: true);
            Assert.That(shortEvent.BrakePointBiasM, Is.LessThan(endurance.BrakePointBiasM));
        }

        [Test]
        public void VogelDefensiveErrorRampsFaster()
        {
            var c = RivalIntimidation.Ceilings["vogel"];
            var x = RivalIntimidation.Update(c, lossesToRival: 4);
            Assert.That(x.DefensivePositionErrorM, Is.EqualTo(c.DefensivePositionErrorCeilingM).Within(0.0001));
            Assert.That(x.BrakePointBiasM, Is.LessThan(c.BrakePointBiasCeilingM));
        }

        private static DriverModel MakeModel(double aggression, double consistency) => new DriverModel
        {
            Aggression = aggression,
            Consistency = consistency,
            BrakingConfidence = 0.8,
            ThrottleDiscipline = 0.8,
            WetSkill = 0.7,
            TireConservation = 0.7,
            MechanicalSympathy = 0.7,
            MistakeProbability = 0,
        };

        [Test]
        public void IdenticalSamplesProduceIdenticalInputs()
        {
            var model = MakeModel(0.5, 0.9);
            var p = new DriverPerception { NodeIndex = 1, DistanceToNodeM = 10, HeadingErrorRadians = 0.1, TargetSpeedMps = 30 };
            var i = new RivalIntimidationState { BrakePointBiasM = 8, DefensivePositionErrorM = 0.2, PassAttemptSuppression = 0.5 };
            var proximity = new TrackAiDriver.PlayerProximity(isAlongside: true, rivalIsBehindPlayer: true);

            var a = TrackAiDriver.Input(model, p, 32, i, proximity, defensiveSample: 0.25, suppressionSample01: 0.2);
            var b = TrackAiDriver.Input(model, p, 32, i, proximity, defensiveSample: 0.25, suppressionSample01: 0.2);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void SuppressionUsesInjectedSampleNotRandomness()
        {
            var model = MakeModel(0.8, 0.9);
            var p = new DriverPerception { NodeIndex = 1, DistanceToNodeM = 20, HeadingErrorRadians = 0, TargetSpeedMps = 40 };
            var i = new RivalIntimidationState { PassAttemptSuppression = 0.5 };
            var proximity = new TrackAiDriver.PlayerProximity(isAlongside: false, rivalIsBehindPlayer: true);

            var suppressed = TrackAiDriver.Input(model, p, 20, i, proximity, defensiveSample: 0, suppressionSample01: 0.1);
            var notSuppressed = TrackAiDriver.Input(model, p, 20, i, proximity, defensiveSample: 0, suppressionSample01: 0.9);
            Assert.That(suppressed.Throttle, Is.LessThan(notSuppressed.Throttle));
        }

        [Test]
        public void MemoryDerivesIntimidationWithoutSecondTruth()
        {
            var r = new RivalBehaviorRuntime();
            Assert.That(r.Intimidation("kade"), Is.EqualTo(new RivalIntimidationState()));
            r.RecordResult("kade", playerWon: true);
            Assert.That(r.Memory("kade").LossesToPlayer, Is.EqualTo(1));
            Assert.That(r.Intimidation("kade").LaunchReactionDelayMs, Is.GreaterThan(0));
        }

        [Test]
        public void RestoreRecomputesDerivedState()
        {
            var m = new RivalMemory();
            m.Record(playerWon: true);
            m.Record(playerWon: true);
            var r = new RivalBehaviorRuntime();
            r.Restore(new System.Collections.Generic.Dictionary<string, RivalMemory> { ["vogel"] = m });
            var a = r.Intimidation("vogel");
            r.Restore(new System.Collections.Generic.Dictionary<string, RivalMemory> { ["vogel"] = m });
            Assert.That(a, Is.EqualTo(r.Intimidation("vogel")));
        }

        [Test]
        public void DeterministicSamplesStableAndBounded()
        {
            var a = RivalDeterministicSample.Unit("reyes", 42, "suppression");
            Assert.That(a, Is.EqualTo(RivalDeterministicSample.Unit("reyes", 42, "suppression")));
            Assert.That(a, Is.InRange(0, 1));
            var s = RivalDeterministicSample.Signed("reyes", 42, "defense");
            Assert.That(s, Is.InRange(-1, 1));
        }
    }
}
