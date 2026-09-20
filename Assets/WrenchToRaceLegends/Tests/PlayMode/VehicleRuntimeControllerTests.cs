using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using WTRL.Content;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>
    /// The automated substitute for "a human presses Play and watches the
    /// box move" (UI/CONTRACT.md's outstanding manual step). Runs the
    /// real MonoBehaviour inside an actual Play session (not a mock, not
    /// the throwaway dotnet-test method every non-Unity assembly uses).
    ///
    /// IMPORTANT LIMITATION FOUND DURING THIS PASS: simulating a held
    /// keyboard key via `InputSystem.QueueStateEvent` + `InputSystem
    /// .Update()` proved unreliable under this project's `-nographics
    /// -batchmode` PlayMode test runs -- a queued key-down event read
    /// back as pressed immediately after queuing, but had already
    /// reverted to released by the very next `FixedUpdate`, even when
    /// the event was re-queued every single frame. This looks like a
    /// real constraint of headless/no-window Play sessions rather than a
    /// bug in `VehicleRuntimeController` itself (its keyboard-reading
    /// code in `FixedUpdate` was never the thing that failed -- the
    /// simulated hardware state was). Root cause not further diagnosed;
    /// flagged here rather than silently worked around.
    ///
    /// Because of that, the movement-chain test below drives
    /// `VehicleRuntimeController.Tick(VehicleInput)` directly -- an
    /// `internal` method `FixedUpdate` now delegates to, exposed via
    /// `[InternalsVisibleTo("WTRL.Tests.PlayMode")]` -- instead of
    /// simulating a keyboard. This still exercises the real end-to-end
    /// chain this test cares about (Content asset -&gt; WTRLRuntime
    /// .Advance -&gt; Transform), just with explicit input instead of
    /// hardware-simulated input. A real headed (non-`-nographics`) Play
    /// session is what's still needed to confirm actual keyboard input
    /// reaches `Keyboard.current` correctly end-to-end -- that remains
    /// an open manual verification step, not one this suite can close.
    ///
    /// Builds its own in-memory content assets rather than depending on
    /// HeroContentBuilder's generated .asset files, so this test doesn't
    /// silently start failing if someone edits the generated assets --
    /// it owns its own fixture data, same discipline as every EditMode
    /// test's Make*() helpers.
    /// </summary>
    public class VehicleRuntimeControllerTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
        }

        private static VehicleDefinitionAsset MakeVehicleAsset()
        {
            var engine = ScriptableObject.CreateInstance<EngineDefinitionAsset>();
            engine.id = "test-engine";
            engine.displayName = "Test Engine";
            engine.displacementLiters = 5.0;
            engine.peakPowerHp = 420;
            engine.peakTorqueLbFt = 390;
            engine.redlineRpm = 7000;
            engine.idleRpm = 800;

            var transmission = ScriptableObject.CreateInstance<TransmissionDefinitionAsset>();
            transmission.id = "test-gearbox";
            transmission.displayName = "Test Gearbox";
            transmission.ratios = new[] { 3.36, 2.07, 1.43, 1.00, 0.84 };
            transmission.finalDrive = 3.55;

            var suspension = ScriptableObject.CreateInstance<SuspensionDefinitionAsset>();
            suspension.id = "test-suspension";
            suspension.displayName = "Test Suspension";
            suspension.frontLayout = "double-wishbone";
            suspension.rearLayout = "multi-link";

            var tire = ScriptableObject.CreateInstance<TireDefinitionAsset>();
            tire.id = "test-tire";
            tire.displayName = "Test Tire";
            tire.longitudinalStiffness = 8.5;
            tire.corneringStiffness = 5.5;
            tire.peakSlipRatio = 0.12;
            tire.peakSlipAngleRadians = 0.11;

            var vehicle = ScriptableObject.CreateInstance<VehicleDefinitionAsset>();
            vehicle.id = "test-vehicle";
            vehicle.generation = "test";
            vehicle.displayName = "Test Vehicle";
            vehicle.massKg = 1450;
            vehicle.wheelbaseM = 2.6;
            vehicle.engine = engine;
            vehicle.transmission = transmission;
            vehicle.suspension = suspension;
            vehicle.tire = tire;
            return vehicle;
        }

        // GameObjects for these tests start inactive so AddComponent does
        // NOT invoke Awake() immediately (Unity calls Awake synchronously
        // the instant a component is added to an ACTIVE GameObject --
        // too early to have assigned `vehicle` yet). Awake only fires
        // once SetActive(true) runs, by which point `vehicle` is already
        // set. Missing this ordering was a real bug in an earlier version
        // of this test suite, caught by the tests themselves failing with
        // the exact "requires a fully-assigned" error this component
        // logs when unconfigured.
        private WTRL.UI.VehicleRuntimeController AddInactiveConfigured(VehicleDefinitionAsset asset)
        {
            _go = new GameObject("VehicleUnderTest");
            _go.SetActive(false);
            var controller = _go.AddComponent<WTRL.UI.VehicleRuntimeController>();
            controller.vehicle = asset;
            _go.SetActive(true);
            return controller;
        }

        [UnityTest]
        public IEnumerator HoldingThrottleMovesTheGameObjectForward()
        {
            var controller = AddInactiveConfigured(MakeVehicleAsset());
            var startPosition = _go.transform.position;

            // 60 fixed steps at the default 0.02s timestep is 1.2s of
            // simulated throttle -- comfortably enough for a 420hp car to
            // move measurably from rest.
            for (var i = 0; i < 60; i++)
            {
                controller.Tick(new VehicleInput(throttle: 1));
                yield return new WaitForFixedUpdate();
            }

            var moved = Vector3.Distance(startPosition, _go.transform.position);
            Assert.That(moved, Is.GreaterThan(0.5f),
                "VehicleRuntimeController.Tick did not move the GameObject under throttle -- " +
                "the end-to-end Content asset -> WTRLRuntime.Advance -> Transform chain is broken.");
        }

        [UnityTest]
        public IEnumerator NoInputLeavesTheGameObjectStationary()
        {
            var controller = AddInactiveConfigured(MakeVehicleAsset());
            var startPosition = _go.transform.position;

            for (var i = 0; i < 10; i++)
            {
                controller.Tick(default);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Vector3.Distance(startPosition, _go.transform.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator MissingSubAssetDisablesTheComponentWithoutThrowing()
        {
            // vehicle is null at the moment AddComponent synchronously
            // invokes Awake(), which is exactly the case this test means
            // to cover -- Awake's own LogError is expected, not a failure.
            LogAssert.Expect(LogType.Error, new Regex("requires a fully-assigned"));

            _go = new GameObject("VehicleUnderTest");
            var controller = _go.AddComponent<WTRL.UI.VehicleRuntimeController>();

            yield return null;

            Assert.That(controller.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator KeyboardInputSimulationIsUnreliableInHeadlessBatchmode()
        {
            // Documents the real, reproduced limitation described in this
            // class's doc comment, rather than silently deleting the
            // investigation. Asserts only what actually held true across
            // repeated attempts: a queued state event registers as
            // pressed immediately after `InputSystem.Update()`, but does
            // NOT reliably survive to the next FixedUpdate in this
            // environment. This is an environment constraint, not a
            // regression to fix -- if this test's second assertion ever
            // starts passing (a future Unity/Input System version fixing
            // headless simulation), replace `HoldingThrottleMovesThe
            // GameObjectForward`'s Tick()-based approach with real
            // simulated keyboard input, since that would be a strictly
            // more faithful test of the shipped FixedUpdate code path.
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                InputSystem.Update();
                Assert.That(Keyboard.current.wKey.isPressed, Is.True,
                    "a freshly-queued key state should read as pressed immediately");

                yield return new WaitForFixedUpdate();

                // Not asserted as a bug -- recorded as the known-unreliable
                // behavior. If this starts failing (state stayed true),
                // that's good news, not a broken test.
                Assert.That(Keyboard.current.wKey.isPressed, Is.False,
                    "documenting known behavior: simulated key state did not survive one FixedUpdate " +
                    "in headless batchmode -- see this file's class doc comment");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }
    }
}
