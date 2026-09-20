using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace WTRL.UI
{
    /// <summary>
    /// The simplest possible chase camera -- exists only so the vertical
    /// slice scene has something other than a static viewpoint. Not
    /// Cinemachine-based despite `com.unity.cinemachine` being installed:
    /// configuring a real Cinemachine rig well requires visual iteration
    /// this pass can't do blind, so a plain lerp-follow is the honest
    /// placeholder instead of a half-configured Cinemachine setup nobody
    /// has looked at.
    ///
    /// Now also a real mobile touch camera rig, closing "no mobile...
    /// camera" from the world-content gap audit -- one-finger drag
    /// orbits around the target (yaw/pitch), two-finger pinch zooms the
    /// follow distance, matching the same New Input System touch
    /// approach `VehicleRuntimeController`'s touch throttle/brake already
    /// uses (this project standardizes on the New Input System package,
    /// not the legacy `Input` class). The orbit/zoom math is split into
    /// plain, hardware-independent methods (`ApplyOrbitDelta`/
    /// `ApplyPinchZoomDelta`) specifically so it's unit-testable: an
    /// earlier PlayMode test pass (`VehicleRuntimeControllerTests.cs`)
    /// found that simulating held touch/keyboard hardware state via
    /// `InputSystem.QueueStateEvent` is unreliable under this project's
    /// `-nographics -batchmode` PlayMode runs (a queued press read back
    /// as active immediately, then reverted before the next
    /// FixedUpdate), so the actual `EnhancedTouch` reading in `Update`
    /// is deliberately kept as thin as possible around these testable
    /// methods rather than tested directly.
    /// </summary>
    public sealed class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0, 4, -8);
        [SerializeField] private float positionLerpSpeed = 5f;
        [SerializeField] private float rotationLerpSpeed = 5f;

        [Header("Mobile touch orbit/zoom")]
        [SerializeField] private float orbitDegreesPerPixel = 0.2f;
        [SerializeField] private float minPitchDegrees = -20f;
        [SerializeField] private float maxPitchDegrees = 70f;
        [SerializeField] private float zoomMetersPerPinchPixel = 0.02f;
        [SerializeField] private float minZoomDistanceM = 3f;
        [SerializeField] private float maxZoomDistanceM = 20f;

        private float _orbitYawDegrees;
        private float _orbitPitchDegrees;
        private float _zoomDistanceM;

        public float OrbitYawDegrees => _orbitYawDegrees;
        public float OrbitPitchDegrees => _orbitPitchDegrees;
        public float ZoomDistanceM => _zoomDistanceM;

        private void Awake()
        {
            _zoomDistanceM = offset.magnitude;
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();
        private void OnDisable() => EnhancedTouchSupport.Disable();

        /// <summary>Applies a one-finger drag (in screen pixels) to the
        /// orbit yaw/pitch, clamping pitch to avoid flipping over the top
        /// or under the ground plane. Pure and hardware-independent --
        /// this is what a real one-finger drag reads and calls, and what
        /// tests call directly instead of simulating touch hardware.</summary>
        public void ApplyOrbitDelta(Vector2 dragDeltaPixels)
        {
            _orbitYawDegrees += dragDeltaPixels.x * orbitDegreesPerPixel;
            _orbitPitchDegrees = Mathf.Clamp(
                _orbitPitchDegrees - dragDeltaPixels.y * orbitDegreesPerPixel,
                minPitchDegrees, maxPitchDegrees);
        }

        /// <summary>Applies a two-finger pinch delta (in screen pixels,
        /// positive = fingers moving apart = zoom in / closer) to the
        /// follow distance, clamped to [minZoomDistanceM, maxZoomDistanceM].</summary>
        public void ApplyPinchZoomDelta(float pinchDeltaPixels)
        {
            _zoomDistanceM = Mathf.Clamp(
                _zoomDistanceM - pinchDeltaPixels * zoomMetersPerPinchPixel,
                minZoomDistanceM, maxZoomDistanceM);
        }

        private void Update()
        {
            var touches = Touch.activeTouches;
            if (touches.Count == 1)
            {
                ApplyOrbitDelta(touches[0].delta);
            }
            else if (touches.Count >= 2)
            {
                var previousDistance = Vector2.Distance(
                    touches[0].screenPosition - touches[0].delta, touches[1].screenPosition - touches[1].delta);
                var currentDistance = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
                ApplyPinchZoomDelta(currentDistance - previousDistance);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            var orbitRotation = Quaternion.Euler(_orbitPitchDegrees, _orbitYawDegrees, 0);
            var orbitOffset = orbitRotation * new Vector3(0, offset.y, -_zoomDistanceM);
            var desiredPosition = target.position + target.rotation * orbitOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerpSpeed * Time.deltaTime);

            var lookRotation = Quaternion.LookRotation(target.position - transform.position + Vector3.up, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationLerpSpeed * Time.deltaTime);
        }
    }
}
