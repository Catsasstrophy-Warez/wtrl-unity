using NUnit.Framework;
using WTRL.Racing;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>Tests for the real contact/overtake detection primitives
    /// added to close "the concrete, narrow blocker for further Career
    /// progress" named in Career/CONTRACT.md. See ContactDetector.cs's
    /// own doc comment for the honest scope limit: detection only, not
    /// yet wired into RaceSessionController/RaceCompletionBridge.</summary>
    public class ContactDetectorTests
    {
        private static VehicleSimState At(double x, double z)
        {
            var state = VehicleSimState.Default();
            state.X = x;
            state.Z = z;
            return state;
        }

        [Test]
        public void DetectContactIsFalseWhenVehiclesAreFarApart()
        {
            var a = At(0, 0);
            var b = At(100, 0);
            var contact = ContactDetector.DetectContact(a, b, combinedRadiusM: 3, previousDistanceM: double.PositiveInfinity, out var distance);

            Assert.That(contact, Is.False);
            Assert.That(distance, Is.EqualTo(100).Within(0.001));
        }

        [Test]
        public void DetectContactIsTrueWhenWithinRadiusAndClosing()
        {
            var a = At(0, 0);
            var b = At(2, 0);
            var contact = ContactDetector.DetectContact(a, b, combinedRadiusM: 3, previousDistanceM: 2.5, out var distance);

            Assert.That(distance, Is.EqualTo(2).Within(0.001));
            Assert.That(contact, Is.True);
        }

        [Test]
        public void DetectContactIsFalseWhenWithinRadiusButSeparating()
        {
            // Two cars running side by side at a stable 2m gap that was
            // already 2m last frame -- not "closing," so not contact.
            // Guards against a car parked next to another one forever
            // registering as permanent contact.
            var a = At(0, 0);
            var b = At(2, 0);
            var contact = ContactDetector.DetectContact(a, b, combinedRadiusM: 3, previousDistanceM: 2.0, out _);

            Assert.That(contact, Is.False);
        }

        [Test]
        public void ArcLengthAtStartIsZero()
        {
            var line = SampleContent.FoundryRowCircuitLine();
            var firstNode = line.Nodes[0];
            var arcLength = TrackProgress.ArcLengthAt(line, firstNode.X, firstNode.Z);

            Assert.That(arcLength, Is.EqualTo(0).Within(0.01));
        }

        [Test]
        public void ArcLengthIncreasesMonotonicallyAlongEachRealSegment()
        {
            var line = SampleContent.FoundryRowCircuitLine();
            var a = line.Nodes[0];
            var b = line.Nodes[1];

            var midX = (a.X + b.X) / 2;
            var midZ = (a.Z + b.Z) / 2;

            var atStart = TrackProgress.ArcLengthAt(line, a.X, a.Z);
            var atMidpoint = TrackProgress.ArcLengthAt(line, midX, midZ);
            var atEnd = TrackProgress.ArcLengthAt(line, b.X, b.Z);

            Assert.That(atMidpoint, Is.GreaterThan(atStart));
            Assert.That(atEnd, Is.GreaterThan(atMidpoint));
        }

        [Test]
        public void OvertakeTrackerReportsNothingOnTheFirstUpdate()
        {
            var tracker = new OvertakeTracker();
            var result = tracker.Update(progressA: 10, progressB: 5, contactThisFrame: false);
            Assert.That(result, Is.False, "no prior order exists yet to compare against");
        }

        [Test]
        public void OvertakeTrackerReportsACleanOvertakeWhenOrderFlipsWithoutContact()
        {
            var tracker = new OvertakeTracker();
            tracker.Update(progressA: 10, progressB: 5, contactThisFrame: false); // A ahead
            var overtook = tracker.Update(progressA: 10, progressB: 15, contactThisFrame: false); // B now ahead

            Assert.That(overtook, Is.True);
        }

        [Test]
        public void OvertakeTrackerDoesNotReportAnOvertakeIfContactOccurredFirst()
        {
            var tracker = new OvertakeTracker();
            tracker.Update(progressA: 10, progressB: 5, contactThisFrame: false); // A ahead
            tracker.Update(progressA: 10, progressB: 9, contactThisFrame: true); // contact while still A ahead
            var overtook = tracker.Update(progressA: 10, progressB: 15, contactThisFrame: false); // B now ahead

            Assert.That(overtook, Is.False, "a real overtake shouldn't be reported clean if there was contact along the way");
        }

        [Test]
        public void OvertakeTrackerResetsItsContactFlagAfterReportingAnOvertake()
        {
            var tracker = new OvertakeTracker();
            tracker.Update(progressA: 10, progressB: 5, contactThisFrame: false);
            tracker.Update(progressA: 10, progressB: 15, contactThisFrame: false); // clean overtake, B ahead

            // A retakes the lead cleanly -- should independently report
            // a second clean overtake, not be suppressed by the first
            // overtake's now-stale contact bookkeeping.
            var secondOvertake = tracker.Update(progressA: 20, progressB: 15, contactThisFrame: false);
            Assert.That(secondOvertake, Is.True);
        }
    }
}
