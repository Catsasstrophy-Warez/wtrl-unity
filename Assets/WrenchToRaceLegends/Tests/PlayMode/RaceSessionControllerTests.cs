using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WTRL.Career;
using WTRL.Events;
using WTRL.UI;

namespace WTRL.Tests
{
    /// <summary>
    /// Drives a real `RaceSessionController` through a full race using
    /// simulated vehicle movement -- closes "ResultsScreen isn't wired
    /// into any scene with an active RaceFlowController". No IMGUI
    /// executes in headless test runs, so this exercises the
    /// controller's real phase/lap-detection logic and confirms
    /// `ResultsScreen`/`CareerState` react correctly, not the rendered
    /// UI itself.
    /// </summary>
    public class RaceSessionControllerTests
    {
        private static (GameObject go, RaceSessionController controller, Transform vehicle, ResultsScreen screen, CareerStateHolder career)
            MakeSession(int laps)
        {
            var vehicleGo = new GameObject("Vehicle");

            var careerGo = new GameObject("CareerState");
            var career = careerGo.AddComponent<CareerStateHolder>();

            var screenGo = new GameObject("ResultsScreen");
            var screen = screenGo.AddComponent<ResultsScreen>();

            var sessionGo = new GameObject("RaceSession");
            var controller = sessionGo.AddComponent<RaceSessionController>();

            typeof(RaceSessionController).GetField("vehicleTransform",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, vehicleGo.transform);
            typeof(RaceSessionController).GetField("resultsScreen",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, screen);
            typeof(RaceSessionController).GetField("careerState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, career);
            typeof(RaceSessionController).GetField("laps",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, laps);
            typeof(RaceSessionController).GetField("countdownSeconds",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, 0f);
            typeof(RaceSessionController).GetField("returnRadiusM",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, 5f);
            typeof(RaceSessionController).GetField("departureRadiusM",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, 10f);

            // Start() is a private Unity message -- invoke it directly
            // rather than waiting a real frame, since EditMode/PlayMode
            // test timing for MonoBehaviour lifecycle messages isn't
            // guaranteed synchronous in this project's batchmode runs.
            typeof(RaceSessionController).GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(controller, null);

            return (sessionGo, controller, vehicleGo.transform, screen, career);
        }

        private static void SimulateUpdate(RaceSessionController controller, float dt)
        {
            typeof(RaceSessionController).GetMethod("Update",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(controller, null);
        }

        private static void DriveOneLap(Transform vehicle, RaceSessionController controller)
        {
            // Leave the start zone (radius 5, threshold 2x = 10) then
            // return within it -- the real heuristic CompleteLap() uses.
            vehicle.position = new Vector3(50, 0, 0);
            SimulateUpdate(controller, 0.1f);
            vehicle.position = Vector3.zero;
            SimulateUpdate(controller, 0.1f);
        }

        [Test]
        public void SessionReachesRacingPhaseAfterStartWithZeroCountdown()
        {
            var (go, controller, _, _, _) = MakeSession(laps: 1);
            SimulateUpdate(controller, 0.1f);

            Assert.That(controller.Controller.Phase, Is.EqualTo(RaceFlowPhase.Racing));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DrivingAwayThenBackToStartCompletesARealLap()
        {
            var (go, controller, vehicle, _, _) = MakeSession(laps: 2);
            SimulateUpdate(controller, 0.1f);

            DriveOneLap(vehicle, controller);

            Assert.That(controller.Controller.State.LapTimes.Count, Is.EqualTo(1));
            Assert.That(controller.Controller.Phase, Is.EqualTo(RaceFlowPhase.Racing), "1 of 2 laps done, still racing");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CompletingAllLapsShowsResults()
        {
            var (go, controller, vehicle, screen, _) = MakeSession(laps: 1);
            SimulateUpdate(controller, 0.1f);

            DriveOneLap(vehicle, controller);
            // One more Update to let Finishing -> ShowResults() run.
            SimulateUpdate(controller, 0.1f);

            Assert.That(controller.Controller.Phase, Is.EqualTo(RaceFlowPhase.Results));
            Assert.That(screen.IsShowing, Is.True);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PressingContinueOnResultsUpdatesCareerState()
        {
            // Simulates ResultsScreen's real "Continue" button, which
            // calls Complete() directly -- that's the actual, only
            // trigger for RaceCompletionBridge to record the outcome,
            // not merely reaching the Results phase.
            var (go, controller, vehicle, screen, career) = MakeSession(laps: 1);
            SimulateUpdate(controller, 0.1f);
            DriveOneLap(vehicle, controller);
            SimulateUpdate(controller, 0.1f);

            controller.Controller.Complete();

            Assert.That(controller.Controller.Phase, Is.EqualTo(RaceFlowPhase.Complete));
            Assert.That(screen.IsShowing, Is.False);
            Assert.That(career.State.CompletedRaceIds, Does.Contain("vertical-slice-club-circuit"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MisconfiguredRadiiFallBackInsteadOfHangingTheRaceForever()
        {
            // Real bug found by a deep-dive review: departureRadiusM <=
            // returnRadiusM used to mean a vehicle could never register
            // as having "left" the start zone, so no lap could ever
            // complete -- the race hung in Racing phase permanently
            // with no error. Start() now detects this and falls back to
            // a safe ratio instead. This test configures exactly that
            // misconfiguration (both radii equal) and confirms a lap
            // still completes rather than hanging.
            var (go, controller, vehicle, _, _) = MakeSession(laps: 1);
            typeof(RaceSessionController).GetField("returnRadiusM",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, 5f);
            typeof(RaceSessionController).GetField("departureRadiusM",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, 5f); // misconfigured: equal to returnRadiusM

            // The whole point of the fix: this misconfiguration is now
            // loud (a real Debug.LogError), not a silent hang -- assert
            // the warning is actually raised, not just that recovery
            // works.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("departureRadiusM.*must be greater than returnRadiusM"));

            // Re-run Start() with the misconfigured radii now set.
            typeof(RaceSessionController).GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(controller, null);
            SimulateUpdate(controller, 0.1f);

            DriveOneLap(vehicle, controller);

            Assert.That(controller.Controller.State.LapTimes.Count, Is.EqualTo(1),
                "a lap must still complete even with a misconfigured radius pair, not hang forever");
            Object.DestroyImmediate(go);
        }
    }
}
