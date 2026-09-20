using NUnit.Framework;
using WTRL.RPG;

namespace WTRL.Tests
{
    /// <summary>New tests for WTRL.RPG — no Swift/Prototype~ source to port
    /// tests from (this is new design, not a port). Each test asserts a
    /// specific claim from 48-RPG-SYSTEMS-SPEC.md, cited in the test name
    /// or a comment, so a spec change surfaces as a specific failing
    /// test rather than a vague regression. Run via the same throwaway
    /// dotnet test project as every other assembly — all pass.</summary>
    public class RpgTests
    {
        [Test]
        public void ReputationStartsUnknown()
        {
            var r = new ReputationState();
            Assert.That(r.Tier, Is.EqualTo(ReputationTier.Unknown));
        }

        [Test]
        public void ReputationTiersAdvanceAtThresholds()
        {
            var r = new ReputationState();
            r.RecordEvent(ReputationEvent.ShopDriverJobCompleted, r.KnownThreshold);
            Assert.That(r.Tier, Is.EqualTo(ReputationTier.Known));
        }

        [Test]
        public void RepeatWinsAgainstSameRivalDiminish()
        {
            // 48-RPG-SYSTEMS-SPEC.md S2.3: "beating the same rival
            // repeatedly diminishes in reputation value."
            var r = new ReputationState();
            r.RecordNamedRivalWin("marsh", baseValue: 10);
            var afterFirst = r.Points;
            r.RecordNamedRivalWin("marsh", baseValue: 10);
            var gainFromSecond = r.Points - afterFirst;
            Assert.That(gainFromSecond, Is.LessThan(afterFirst));
        }

        [Test]
        public void RepeatWinNeverDropsToZeroValue()
        {
            var r = new ReputationState();
            for (var i = 0; i < 50; i++) r.RecordNamedRivalWin("marsh", baseValue: 10);
            var before = r.Points;
            r.RecordNamedRivalWin("marsh", baseValue: 10);
            // Still worth SOMETHING, per the design doc's floor -- "a win
            // is still a win."
            Assert.That(r.Points, Is.GreaterThan(before));
        }

        [Test]
        public void NamedRivalWinThroughGenericRecordEventThrows()
        {
            var r = new ReputationState();
            Assert.Throws<System.InvalidOperationException>(() => r.RecordEvent(ReputationEvent.NamedRivalWin));
        }

        [Test]
        public void SafetyRatingStartsClean()
        {
            var s = new SafetyRatingState();
            Assert.That(s.Rating, Is.EqualTo(100));
            Assert.That(s.WasResultClean(), Is.True);
        }

        [Test]
        public void SafetyRatingNeverExceedsBounds()
        {
            // 48-RPG-SYSTEMS-SPEC.md S3.2: bounded [0,100], not accumulating.
            var s = new SafetyRatingState();
            for (var i = 0; i < 50; i++) s.RecordEvent(SafetyEvent.EventCompletedZeroIncidents);
            Assert.That(s.Rating, Is.LessThanOrEqualTo(100));

            var s2 = new SafetyRatingState();
            for (var i = 0; i < 50; i++) s2.RecordEvent(SafetyEvent.CausedRivalSpinOrRetire);
            Assert.That(s2.Rating, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void CausingRivalSpinDropsRatingMoreThanContact()
        {
            // S3.2's table: "Causing a rival to spin or retire: Decreases
            // sharply" -- explicitly the worst single event.
            var a = new SafetyRatingState();
            a.RecordEvent(SafetyEvent.PlayerCausedContact);
            var b = new SafetyRatingState();
            b.RecordEvent(SafetyEvent.CausedRivalSpinOrRetire);
            Assert.That(b.Rating, Is.LessThan(a.Rating));
        }

        [Test]
        public void ProvisionalLicensePermitsKnockoutEntryRegardlessOfSafety()
        {
            // S3.3: the minimum-safety-rating gate only applies "from the
            // professional licence onward" -- street tier has no lockout.
            var license = new DriverLicenseState();
            var safety = new SafetyRatingState();
            for (var i = 0; i < 20; i++) safety.RecordEvent(SafetyEvent.CausedRivalSpinOrRetire);
            Assert.That(license.PermitsKnockoutEntry(safety), Is.True);
        }

        [Test]
        public void ContenderLicenseBlocksKnockoutEntryBelowMinimumSafety()
        {
            var license = new DriverLicenseState();
            license.Promote(DriverLicenseGrade.Contender);
            var safety = new SafetyRatingState();
            for (var i = 0; i < 5; i++) safety.RecordEvent(SafetyEvent.CausedRivalSpinOrRetire);
            Assert.That(safety.Rating, Is.LessThan(license.MinimumSafetyRatingForKnockoutEntry));
            Assert.That(license.PermitsKnockoutEntry(safety), Is.False);
        }

        [Test]
        public void ClassBracketQualificationIsACeilingNotAnExactMatch()
        {
            // A Street-bracket build (few points) should qualify for a
            // Club-bracket event (higher ceiling), but a maxed Pro build
            // should not qualify for a Street event.
            Assert.That(ClassBracket.QualifiesForBracket(5, ClassBracketTier.Club), Is.True);
            Assert.That(ClassBracket.QualifiesForBracket(100, ClassBracketTier.Street), Is.False);
        }

        [Test]
        public void ClassBracketForPointsMatchesCeilings()
        {
            Assert.That(ClassBracket.BracketForPoints(10), Is.EqualTo(ClassBracketTier.Street));
            Assert.That(ClassBracket.BracketForPoints(ClassBracket.StreetCeiling + 1), Is.EqualTo(ClassBracketTier.Club));
            Assert.That(ClassBracket.BracketForPoints(ClassBracket.SemiProCeiling + 1), Is.EqualTo(ClassBracketTier.Pro));
        }
    }
}
