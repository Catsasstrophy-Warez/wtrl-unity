using UnityEngine;
using UnityEngine.InputSystem;
using WTRL.Content;
using WTRL.Vehicle;
using WTRL.Runtime;

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
    /// UNVERIFIED, like the rest of Content/ and this file's asmdef change
    /// -- see UI/CONTRACT.md. Has never been compiled by Unity (the only
    /// Unity install available in this environment is unlicensed). Written
    /// carefully against the Input System 1.x API and WTRLRuntime's real
    /// signature, but the first licensed Editor open is what actually
    /// proves this compiles and runs.
    /// </summary>
    public sealed class VehicleRuntimeController : MonoBehaviour
    {
        [Header("Required content (no fallback -- all must be assigned)")]
        [SerializeField] private VehicleDefinitionAsset vehicle;

        [Header("Track context")]
        [SerializeField] private double bankingDegrees;

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

            var input = new VehicleInput(throttle, brake, steering);
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
