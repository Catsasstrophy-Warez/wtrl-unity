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

        // ---- Real contact/win detection wired into a live caller ----
        // These build a real AiVehicleController (bypassing its Awake(),
        // which needs a fully-configured ScriptableObject asset this
        // test doesn't need) and drive its underlying AiVehicleSession's
        // real, public State field directly -- the same "set position
        // directly, no physics stepping needed to prove the detection
        // logic" approach the rest of this file already uses for the
        // player's Transform.

        private static WTRL.UI.AiVehicleController MakeRival(string rivalId)
        {
            // Created inactive so AddComponent doesn't run Awake() yet
            // (Awake would otherwise fire immediately with `vehicle`
            // still null, log a real error, and disable the component
            // before this helper gets a chance to set it) -- Awake
            // still never needs to run for this test's purposes since
            // `_session` is set directly afterward anyway.
            var go = new GameObject("Rival");
            go.SetActive(false);
            var ai = go.AddComponent<WTRL.UI.AiVehicleController>();

            var vehicleAsset = ScriptableObject.CreateInstance<WTRL.Content.VehicleDefinitionAsset>();
            vehicleAsset.id = rivalId;
            typeof(WTRL.UI.AiVehicleController).GetField("vehicle")
                .SetValue(ai, vehicleAsset);

            var vehicleDef = new WTRL.Vehicle.VehicleDefinition(rivalId, "test-gen", "Test Rival", massKg: 1400,
                wheelbaseM: 2.5, engineId: "test-engine", transmissionId: "test-gearbox", suspensionId: "test-suspension");
            var engineDef = new WTRL.Vehicle.EngineDefinition("test-engine", "Test Engine", displacementLiters: 3.0,
                peakPowerHp: 250, peakTorqueLbFt: 220);
            var transmissionDef = new WTRL.Vehicle.TransmissionDefinition("test-gearbox", "Test 5-Speed",
                new[] { 3.2, 2.1, 1.5, 1.1, 0.9 }, finalDrive: 4.0);
            var suspensionDef = new WTRL.Vehicle.SuspensionDefinition("test-suspension", "Test Suspension",
                "double-wishbone", "double-wishbone");
            var tireDef = new WTRL.Vehicle.TireDefinition("test-tire", "Test Tire", longitudinalStiffness: 9.0,
                corneringStiffness: 6.0, peakSlipRatio: 0.11, peakSlipAngleRadians: 0.10);

            var session = new WTRL.Racing.AiVehicleSession(new WTRL.Racing.DriverModel
            {
                Aggression = 0.5, Consistency = 0.8, BrakingConfidence = 0.7, ThrottleDiscipline = 0.8,
                WetSkill = 0.6, TireConservation = 0.6, MechanicalSympathy = 0.6, MistakeProbability = 0,
            }, WTRL.Racing.SampleContent.FoundryRowCircuitLine(), vehicleDef, engineDef, transmissionDef, tireDef, suspensionDef);

            typeof(WTRL.UI.AiVehicleController).GetField("_session",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ai, session);

            return ai;
        }

        private static void SetRivalPosition(WTRL.UI.AiVehicleController rival, double x, double z)
        {
            var session = (WTRL.Racing.AiVehicleSession)typeof(WTRL.UI.AiVehicleController).GetField("_session",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(rival);
            var state = session.State;
            state.X = x;
            state.Z = z;
            session.State = state;
        }

        private static (GameObject go, RaceSessionController controller, Transform vehicle, WTRL.UI.AiVehicleController rival, CareerStateHolder career)
            MakeSessionWithRival(int laps)
        {
            var (go, controller, vehicle, _, career) = MakeSession(laps);
            var rival = MakeRival("test-rival");

            typeof(RaceSessionController).GetField("rival",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(controller, rival);

            // Re-run Start() now that `rival` is assigned, so the
            // rival-aware branch (EnrichOutcome, progress trackers) is
            // actually initialized -- MakeSession() already ran Start()
            // once without a rival.
            typeof(RaceSessionController).GetMethod("Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(controller, null);

            return (go, controller, vehicle, rival, career);
        }

        // The 3 tests below exercise the real, private
        // `EnrichOutcomeWithContactDetection` method directly via
        // reflection, injecting known `LapProgressTracker`/overtake
        // state rather than choreographing exact vehicle movement
        // through the full Update() loop. Found necessary while writing
        // this: the arcade lap-detection heuristic (return-to-start-
        // radius) means a completed lap always ends with the player
        // physically near the start line -- i.e. near-zero current
        // track arc length -- which made a full-geometry simulation of
        // "player finishes farther ahead" fight the very heuristic
        // that's supposed to detect the lap in the first place. Testing
        // the enrichment method's real logic directly against known
        // inputs is more robust AND more precisely targeted at the
        // actual new code, not an artifact of unrelated lap-detection
        // geometry.

        private static WTRL.Racing.LapProgressTracker MakeProgressAt(double totalDistanceM)
        {
            var tracker = new WTRL.Racing.LapProgressTracker(trackLengthM: 1_000_000); // large enough that this single value never wraps
            tracker.Update(totalDistanceM);
            return tracker;
        }

        private static RaceOutcomeDetail InvokeEnrich(RaceSessionController controller, WTRL.Racing.LapProgressTracker playerProgress,
            WTRL.Racing.LapProgressTracker rivalProgress, bool cleanOvertakeOccurred)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(RaceSessionController).GetField("_playerProgress", flags).SetValue(controller, playerProgress);
            typeof(RaceSessionController).GetField("_rivalProgress", flags).SetValue(controller, rivalProgress);
            typeof(RaceSessionController).GetField("_playerMadeACleanOvertake", flags).SetValue(controller, cleanOvertakeOccurred);

            var baseOutcome = new RaceOutcomeDetail("test-race", 42.0, RaceFormat.Circuit);
            var method = typeof(RaceSessionController).GetMethod("EnrichOutcomeWithContactDetection", flags);
            return (RaceOutcomeDetail)method.Invoke(controller, new object[] { baseOutcome });
        }

        [Test]
        public void PlayerAheadInRealTrackProgressIsEnrichedAsAWin()
        {
            var (go, controller, _, _, _) = MakeSessionWithRival(laps: 1);

            var outcome = InvokeEnrich(controller, MakeProgressAt(500), MakeProgressAt(200), cleanOvertakeOccurred: false);

            Assert.That(outcome.RivalId, Is.EqualTo("test-rival"));
            Assert.That(outcome.PlayerWon, Is.True);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerBehindInRealTrackProgressIsEnrichedAsALoss()
        {
            var (go, controller, _, _, _) = MakeSessionWithRival(laps: 1);

            var outcome = InvokeEnrich(controller, MakeProgressAt(100), MakeProgressAt(400), cleanOvertakeOccurred: false);

            Assert.That(outcome.RivalId, Is.EqualTo("test-rival"));
            Assert.That(outcome.PlayerWon, Is.False);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CleanOvertakeFlagFlowsThroughToTheEnrichedOutcome()
        {
            var (go, controller, _, _, _) = MakeSessionWithRival(laps: 1);

            var outcome = InvokeEnrich(controller, MakeProgressAt(100), MakeProgressAt(400), cleanOvertakeOccurred: true);

            Assert.That(outcome.CleanOvertakeOccurred, Is.True);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EnrichedOutcomeNeverFabricatesContactOrOffTrackFields()
        {
            // This project has no fault-attribution or off-track
            // detection -- only "contact occurred" with no fault. Even
            // with a rival present and a win/overtake detected, these
            // specific fields must stay false; setting them would be
            // fabrication this project's discipline exists to prevent.
            var (go, controller, _, _, _) = MakeSessionWithRival(laps: 1);

            var outcome = InvokeEnrich(controller, MakeProgressAt(500), MakeProgressAt(200), cleanOvertakeOccurred: true);

            Assert.That(outcome.PlayerCausedContact, Is.False);
            Assert.That(outcome.CausedRivalSpinOrRetire, Is.False);
            Assert.That(outcome.OffTrackCutForAdvantage, Is.False);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RealRaceCompletionWithARivalPresentActuallyUpdatesRivalMemory()
        {
            // End-to-end sanity check (not the geometry-precise scenario
            // above): drives a real race through RaceSessionController's
            // actual Update loop with a rival present, and confirms
            // SOME real RivalMemory/reputation effect occurs on
            // Complete() -- proving the wiring (EnrichOutcome actually
            // getting called with a real rival attached) works, without
            // depending on exact arc-length arithmetic.
            var (go, controller, vehicle, rival, career) = MakeSessionWithRival(laps: 1);
            SetRivalPosition(rival, 300, 0); // give the rival *some* real, nonzero track progress throughout
            SimulateUpdate(controller, 0.1f);

            DriveOneLap(vehicle, controller);
            SimulateUpdate(controller, 0.1f);
            controller.Controller.Complete();

            var memory = career.State.RivalBehavior.Memory("test-rival");
            Assert.That(memory.Encounters, Is.EqualTo(1), "a real rival result should have been recorded exactly once");
            Object.DestroyImmediate(go);
        }
    }
}
