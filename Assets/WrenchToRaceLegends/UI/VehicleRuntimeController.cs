using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using WTRL.Content;
using WTRL.Vehicle;
using WTRL.Runtime;

[assembly: InternalsVisibleTo("WTRL.Tests.PlayMode")]

namespace WTRL.UI
{
    /// <summary>
    /// First real Unity-side integration point: wraps a <see
    /// cref="WTRLRuntime"/> in a MonoBehaviour, feeds it keyboard input via
    /// the new Input System, and copies its resulting <see
    /// cref="WTRLSnapshot.Vehicle"/> position/heading onto this
    /// GameObject's transform every frame. Deliberately minimal -- this is
    /// the smallest possible end-to-end vertical slice (data asset -&gt;
    /// WTRLRuntime.Advance -&gt; transform), not a real vehicle controller.
    /// No visuals, no camera, no wheel meshes.
    ///
    /// Compiles clean in a real, licensed Unity Editor (confirmed
    /// 2026-09-20) and is exercised end-to-end by
    /// `WTRL.Tests.PlayMode.VehicleRuntimeControllerTests` -- see
    /// UI/CONTRACT.md for the verification history.
    /// </summary>
    public sealed class VehicleRuntimeController : MonoBehaviour
    {
        // Public (not private [SerializeField]) so PlayMode tests and the
        // Editor content builder can assign it directly without reflection
        // or an Editor-only SerializedObject dependency, while still
        // serializing/showing in the Inspector exactly like a
        // [SerializeField] private field would.
        [Header("Required content (no fallback -- all must be assigned)")]
        public VehicleDefinitionAsset vehicle;

        [Header("Track context")]
        public double bankingDegrees;

        // Touch/tilt controls, additive with keyboard -- closes the
        // "wire touch + tilt controls" gap. PROJECT-MAP-UNITY-MOBILE.md's
        // vertical slice calls for "touch+tilt controls"; the Rev16.1
        // audit (Assignments/OUTPUT-Rev16.1-Audit.md) recommends porting
        // that project's real `MobileInputMath.cs` (deadzone, response
        // curve, speed-sensitivity scaling) near-verbatim. That file
        // wasn't available to read in this pass (Rev16.1 is an archived
        // zip, not something this session extracted), so the shape below
        // (deadzone + power-curve response) is a reasonable equivalent,
        // NOT a port -- replace with the real MobileInputMath logic if
        // that archive becomes available to read directly.
        [Header("Touch/tilt (mobile) -- additive with keyboard")]
        [SerializeField] private bool useTiltSteering = true;
        [SerializeField] private float tiltDeadzone = 0.05f;
        [SerializeField] private float tiltResponseCurve = 1.5f;
        [SerializeField] private bool useTouchThrottleBrake = true;

        private WTRLRuntime _runtime;
        private EngineDefinition _engine;
        private TransmissionDefinition _transmission;
        private SuspensionDefinition _suspensionDef;
        private TireDefinition _tire;
        private VehicleDefinition _vehicleDef;

        private void Awake()
        {
            if (vehicle == null || vehicle.engine == null || vehicle.transmission == null ||
                vehicle.suspension == null || vehicle.tire == null)
            {
                Debug.LogError($"{nameof(VehicleRuntimeController)} requires a fully-assigned " +
                    $"{nameof(VehicleDefinitionAsset)} (vehicle + engine + transmission + suspension + tire). " +
                    "This project deliberately has no catalog fallback -- see Content/CONTRACT.md.");
                enabled = false;
                return;
            }

            _vehicleDef = vehicle.ToDefinition();
            _engine = vehicle.engine.ToDefinition();
            _transmission = vehicle.transmission.ToDefinition();
            _suspensionDef = vehicle.suspension.ToDefinition();
            _tire = vehicle.tire.ToDefinition();

            _runtime = new WTRLRuntime(_vehicleDef.Id) { BankingDegrees = bankingDegrees };
        }

        private void FixedUpdate()
        {
            if (_runtime == null) return;

            var keyboard = Keyboard.current;
            var throttle = 0.0;
            var brake = 0.0;
            var steering = 0.0;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttle += 1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) brake += 1;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steering -= 1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steering += 1;
            }

            if (useTouchThrottleBrake && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                // Crude left-half-brake / right-half-throttle tap zones --
                // a real touch HUD (accelerator/brake pedals, or a single
                // combined slider) is future UI work, not this
                // controller's job.
                var touchX = Touchscreen.current.primaryTouch.position.ReadValue().x;
                if (touchX > Screen.width * 0.5f) throttle += 1; else brake += 1;
            }

            if (useTiltSteering && Accelerometer.current != null)
            {
                var tilt = Mathf.Clamp(Accelerometer.current.acceleration.ReadValue().x, -1f, 1f);
                var deadzoned = Mathf.Abs(tilt) < tiltDeadzone ? 0f : tilt;
                steering += Mathf.Sign(deadzoned) * Mathf.Pow(Mathf.Abs(deadzoned), tiltResponseCurve);
            }

            steering = Mathf.Clamp((float)steering, -1f, 1f);
            Tick(new VehicleInput(throttle, brake, steering));
        }

        /// <summary>The actual per-frame simulate-and-apply step, split
        /// out of <see cref="FixedUpdate"/> so tests can drive it with an
        /// explicit <see cref="VehicleInput"/> instead of a real keyboard.
        /// This exists because a real, reproduced limitation of this
        /// environment's `-nographics -batchmode` PlayMode test runs made
        /// simulating a held key via `InputSystem.QueueStateEvent` +
        /// `InputSystem.Update()` unreliable -- a queued key-down event
        /// read back as pressed immediately, but reverted to released by
        /// the time the next `FixedUpdate` ran, even when re-queued every
        /// frame (see `WTRL.Tests.PlayMode.VehicleRuntimeControllerTests`'
        /// own doc comment for the diagnostic trail). Exposed as
        /// `internal` via `InternalsVisibleTo("WTRL.Tests.PlayMode")`
        /// rather than `public`, since it's a test seam, not part of this
        /// component's real API.</summary>
        internal void Tick(VehicleInput input)
        {
            _runtime.Advance(Time.fixedDeltaTime, input, _vehicleDef, _engine, _transmission, _tire,
                _suspensionDef, surface: null);

            var sim = _runtime.Snapshot.Vehicle;
            transform.SetPositionAndRotation(
                new Vector3((float)sim.X, transform.position.y, (float)sim.Z),
                Quaternion.Euler(0, (float)(sim.HeadingRadians * Mathf.Rad2Deg), 0));
        }

        /// <summary>Exposed for a future HUD -- not wired to any UI yet.</summary>
        public WTRLSnapshot? CurrentSnapshot => _runtime?.Snapshot;
    }
}
