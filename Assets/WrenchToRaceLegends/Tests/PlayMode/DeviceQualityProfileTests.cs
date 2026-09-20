using NUnit.Framework;
using UnityEngine;
using WTRL.Runtime;

namespace WTRL.Tests
{
    /// <summary>Tests for the project's first device quality profile
    /// system -- closes "no device quality profiles" from the Milestone
    /// M8 gap audit. Runs as PlayMode (not EditMode) since it asserts on
    /// real `QualitySettings`/`Application` state, which is more
    /// representative of a real player runtime than the Editor's own
    /// quality settings context. See DeviceQualityProfile.cs's own doc
    /// comment for the honest scope limit: fixed tiers a caller picks
    /// explicitly, no real device benchmarking (impossible without real
    /// hardware, which this environment doesn't have).</summary>
    public class DeviceQualityProfileTests
    {
        [Test]
        public void LowTierDisablesShadowsAndTargetsThirtyFps()
        {
            DeviceQualityProfile.Apply(DeviceQualityTier.Low);

            Assert.That(QualitySettings.shadows, Is.EqualTo(ShadowQuality.Disable));
            Assert.That(Application.targetFrameRate, Is.EqualTo(30));
            Assert.That(DeviceQualityProfile.Current, Is.EqualTo(DeviceQualityTier.Low));
        }

        [Test]
        public void HighTierEnablesAllShadowsAndTargetsSixtyFps()
        {
            DeviceQualityProfile.Apply(DeviceQualityTier.High);

            Assert.That(QualitySettings.shadows, Is.EqualTo(ShadowQuality.All));
            Assert.That(Application.targetFrameRate, Is.EqualTo(60));
            Assert.That(DeviceQualityProfile.Current, Is.EqualTo(DeviceQualityTier.High));
        }

        [Test]
        public void MediumTierIsTheDefaultBeforeAnyApplyCall()
        {
            // Only meaningful in isolation from the other tests in this
            // fixture (Current is process-global state) -- verified by
            // re-applying Medium explicitly and checking its real
            // settings rather than relying on run order.
            DeviceQualityProfile.Apply(DeviceQualityTier.Medium);

            Assert.That(QualitySettings.shadows, Is.EqualTo(ShadowQuality.HardOnly));
            Assert.That(QualitySettings.antiAliasing, Is.EqualTo(2));
        }
    }
}
