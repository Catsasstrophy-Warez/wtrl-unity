using NUnit.Framework;
using UnityEngine;
using WTRL.UI;

namespace WTRL.Tests
{
    /// <summary>
    /// Tests the mobile touch orbit/zoom logic added to
    /// `SimpleFollowCamera` via its plain, hardware-independent
    /// `ApplyOrbitDelta`/`ApplyPinchZoomDelta` methods -- NOT via
    /// simulated touch hardware, since an earlier PlayMode test pass
    /// (`VehicleRuntimeControllerTests.cs`) found `InputSystem
    /// .QueueStateEvent`-simulated hardware state unreliable under this
    /// project's headless PlayMode runs. This exercises the actual
    /// math a real one-finger drag / two-finger pinch would call.
    /// </summary>
    public class SimpleFollowCameraTests
    {
        private static SimpleFollowCamera MakeCamera(out GameObject go)
        {
            go = new GameObject("TestCamera");
            return go.AddComponent<SimpleFollowCamera>();
        }

        [Test]
        public void OrbitDragIncreasesYawAndInvertsPitchFromVerticalDrag()
        {
            var camera = MakeCamera(out var go);

            camera.ApplyOrbitDelta(new Vector2(50, 0));
            Assert.That(camera.OrbitYawDegrees, Is.GreaterThan(0f));
            Assert.That(camera.OrbitPitchDegrees, Is.EqualTo(0f).Within(0.001f));

            camera.ApplyOrbitDelta(new Vector2(0, 50));
            // Dragging up (positive screen Y delta) should pitch the
            // camera DOWN toward the target, not up over its roof.
            Assert.That(camera.OrbitPitchDegrees, Is.LessThan(0f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void OrbitPitchClampsWithinConfiguredRange()
        {
            var camera = MakeCamera(out var go);

            camera.ApplyOrbitDelta(new Vector2(0, -100000));
            Assert.That(camera.OrbitPitchDegrees, Is.LessThanOrEqualTo(70f));

            camera.ApplyOrbitDelta(new Vector2(0, 100000));
            Assert.That(camera.OrbitPitchDegrees, Is.GreaterThanOrEqualTo(-20f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PinchZoomInMovesCameraCloserAndClampsAtMinimumDistance()
        {
            var camera = MakeCamera(out var go);
            var startDistance = camera.ZoomDistanceM;

            camera.ApplyPinchZoomDelta(200f); // fingers moving apart -> zoom in (closer)
            Assert.That(camera.ZoomDistanceM, Is.LessThan(startDistance));

            camera.ApplyPinchZoomDelta(-100000f);
            Assert.That(camera.ZoomDistanceM, Is.GreaterThanOrEqualTo(3f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PinchZoomOutMovesCameraFartherAndClampsAtMaximumDistance()
        {
            var camera = MakeCamera(out var go);

            var startDistance = camera.ZoomDistanceM;
            camera.ApplyPinchZoomDelta(-50f); // fingers moving together -> zoom out (farther)
            Assert.That(camera.ZoomDistanceM, Is.GreaterThan(startDistance));

            camera.ApplyPinchZoomDelta(-100000f); // keep zooming out, should clamp at the maximum
            Assert.That(camera.ZoomDistanceM, Is.LessThanOrEqualTo(20f));

            Object.DestroyImmediate(go);
        }
    }
}
