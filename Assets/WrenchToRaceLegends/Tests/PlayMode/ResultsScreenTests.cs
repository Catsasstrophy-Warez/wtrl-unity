using NUnit.Framework;
using UnityEngine;
using WTRL.Events;
using WTRL.UI;

namespace WTRL.Tests
{
    /// <summary>
    /// Drives a real `ResultsScreen` MonoBehaviour bound to a real
    /// `RaceFlowController` through its full phase sequence, asserting
    /// the screen's own visibility/summary logic reacts correctly --
    /// this project has no screenshot/visual QA capability, so this is
    /// the closest available substitute for "look at the results
    /// screen": assert what it WOULD render and when it WOULD appear,
    /// via its testable (non-OnGUI) surface.
    /// </summary>
    public class ResultsScreenTests
    {
        private static RaceFlowController RunToPhase(RaceDefinition race, RaceFlowPhase targetPhase, int laps = 1)
        {
            var controller = new RaceFlowController(race);
            controller.BeginLoading();
            if (targetPhase == RaceFlowPhase.Loading) return controller;
            controller.FinishLoading();
            if (targetPhase == RaceFlowPhase.Staging) return controller;
            controller.BeginCountdown(0);
            controller.Advance(0.1);
            if (targetPhase == RaceFlowPhase.Racing) return controller;
            for (var i = 0; i < laps; i++)
            {
                controller.Advance(30.0); // real elapsed time per lap -- CompleteLap only records a lap time when Elapsed has actually advanced since the last lap start
                controller.CompleteLap();
            }
            if (targetPhase == RaceFlowPhase.Finishing) return controller;
            controller.ShowResults();
            if (targetPhase == RaceFlowPhase.Results) return controller;
            controller.Complete();
            return controller;
        }

        [Test]
        public void ResultsScreenIsNotShowingBeforeResultsPhase()
        {
            var race = new RaceDefinition("results-test-01", "Results Test", "foundry-row-circuit", laps: 1, reputationRequired: 0);
            var controller = RunToPhase(race, RaceFlowPhase.Racing);

            var screenGo = new GameObject("ResultsScreen");
            var screen = screenGo.AddComponent<ResultsScreen>();
            screen.Bind(controller);

            Assert.That(screen.IsShowing, Is.False);
            Object.DestroyImmediate(screenGo);
        }

        [Test]
        public void ResultsScreenShowsOnlyDuringResultsPhaseAndHidesAfterComplete()
        {
            var race = new RaceDefinition("results-test-02", "Results Test 2", "foundry-row-circuit", laps: 2, reputationRequired: 0);
            var controller = RunToPhase(race, RaceFlowPhase.Results, laps: 2);

            var screenGo = new GameObject("ResultsScreen");
            var screen = screenGo.AddComponent<ResultsScreen>();
            screen.Bind(controller);

            Assert.That(screen.IsShowing, Is.True);

            controller.Complete();
            Assert.That(screen.IsShowing, Is.False);
            Object.DestroyImmediate(screenGo);
        }

        [Test]
        public void ResultsScreenSummaryReflectsRealClassifiedTimeAndLapCount()
        {
            var race = new RaceDefinition("results-test-03", "Results Test 3", "foundry-row-circuit", laps: 3, reputationRequired: 0);
            var controller = RunToPhase(race, RaceFlowPhase.Results, laps: 3);

            var screenGo = new GameObject("ResultsScreen");
            var screen = screenGo.AddComponent<ResultsScreen>();
            screen.Bind(controller);

            var summary = screen.BuildSummaryText();
            Assert.That(summary, Does.Contain("Results Test 3"));
            Assert.That(summary, Does.Contain("Laps: 3"));
            Assert.That(summary, Does.Contain($"Classified time: {controller.State.ClassifiedTime:F2}s"));
            Object.DestroyImmediate(screenGo);
        }

        [Test]
        public void ContinueButtonEquivalentCallingCompleteAdvancesControllerToCompletePhase()
        {
            var race = new RaceDefinition("results-test-04", "Results Test 4", "foundry-row-circuit", laps: 1, reputationRequired: 0);
            var controller = RunToPhase(race, RaceFlowPhase.Results);

            var screenGo = new GameObject("ResultsScreen");
            var screen = screenGo.AddComponent<ResultsScreen>();
            screen.Bind(controller);

            // OnGUI's "Continue" button calls exactly this -- exercised
            // directly since IMGUI doesn't execute in headless test runs.
            controller.Complete();

            Assert.That(controller.Phase, Is.EqualTo(RaceFlowPhase.Complete));
            Object.DestroyImmediate(screenGo);
        }
    }
}
