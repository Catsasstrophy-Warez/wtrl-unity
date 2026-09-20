using NUnit.Framework;
using WTRL.Core;

namespace WTRL.Tests
{
    /// <summary>Tests for the project's first analytics infrastructure --
    /// closes "no analytics opt-in" from the Milestone M8 gap audit. See
    /// AnalyticsConsent.cs's own doc comment for the honest scope limit:
    /// no real vendor SDK is wired, only the consent gate + in-memory
    /// log a real backend would eventually drain.</summary>
    public class AnalyticsConsentTests
    {
        [SetUp]
        public void ResetState()
        {
            AnalyticsConsent.OptedIn = false;
            AnalyticsConsent.ClearLog();
        }

        [TearDown]
        public void CleanUp()
        {
            AnalyticsConsent.OptedIn = false;
            AnalyticsConsent.ClearLog();
        }

        [Test]
        public void DefaultsToOptedOut()
        {
            Assert.That(AnalyticsConsent.OptedIn, Is.False);
        }

        [Test]
        public void RecordIsANoOpWhenNotOptedIn()
        {
            AnalyticsConsent.Record("some_event");
            Assert.That(AnalyticsConsent.RecordedEvents, Is.Empty);
        }

        [Test]
        public void RecordLogsTheEventOnceOptedIn()
        {
            AnalyticsConsent.OptedIn = true;
            AnalyticsConsent.Record("garage_opened");

            Assert.That(AnalyticsConsent.RecordedEvents.Count, Is.EqualTo(1));
            Assert.That(AnalyticsConsent.RecordedEvents[0].Name, Is.EqualTo("garage_opened"));
        }

        [Test]
        public void OptingOutAfterEventsWereLoggedDoesNotRetroactivelyClearThem()
        {
            AnalyticsConsent.OptedIn = true;
            AnalyticsConsent.Record("event_a");
            AnalyticsConsent.OptedIn = false;

            Assert.That(AnalyticsConsent.RecordedEvents.Count, Is.EqualTo(1));
            AnalyticsConsent.Record("event_b"); // now a no-op again
            Assert.That(AnalyticsConsent.RecordedEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void RaceCompletionBridgeRecordsARealAnalyticsEventWhenOptedIn()
        {
            AnalyticsConsent.OptedIn = true;

            var race = new WTRL.Events.RaceDefinition("analytics-test-01", "Analytics Test", "foundry-row-circuit",
                laps: 1, reputationRequired: 0);
            var careerState = new Career.CareerState();
            var bridge = new Career.RaceCompletionBridge(careerState);
            var controller = new WTRL.Events.RaceFlowController(race);
            bridge.AttachTo(controller);

            controller.BeginLoading();
            controller.FinishLoading();
            controller.BeginCountdown(0);
            controller.Advance(0.1);
            controller.CompleteLap();
            controller.ShowResults();
            controller.Complete();

            Assert.That(AnalyticsConsent.RecordedEvents.Count, Is.EqualTo(1));
            Assert.That(AnalyticsConsent.RecordedEvents[0].Name, Is.EqualTo("race_completed"));
            Assert.That(AnalyticsConsent.RecordedEvents[0].Parameters["race_id"], Is.EqualTo("analytics-test-01"));
        }
    }
}
