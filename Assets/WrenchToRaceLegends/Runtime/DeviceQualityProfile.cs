using UnityEngine;

namespace WTRL.Runtime
{
    /// <summary>
    /// The project's first device quality profile system -- closes "no
    /// device quality profiles" from the Milestone M8 gap audit.
    ///
    /// HONEST SCOPE: there is no real device benchmarking or automatic
    /// tier detection here (that requires running on real hardware,
    /// which this environment cannot do -- see PIVOT-PLAN.md's standing
    /// note that no device testing of any kind has occurred). This is
    /// three real, concrete, applyable quality tiers a caller picks
    /// explicitly (e.g. from a settings menu, or a build-time default),
    /// each of which actually changes real Unity render/quality
    /// settings -- not a settings-menu enum that does nothing.
    /// </summary>
    public enum DeviceQualityTier { Low, Medium, High }

    public static class DeviceQualityProfile
    {
        public static DeviceQualityTier Current { get; private set; } = DeviceQualityTier.Medium;

        /// <summary>Applies the given tier's real settings to
        /// `QualitySettings`/`Application.targetFrameRate`. Idempotent --
        /// calling it again with the same tier re-applies the same
        /// values rather than erroring.</summary>
        public static void Apply(DeviceQualityTier tier)
        {
            Current = tier;
            switch (tier)
            {
                case DeviceQualityTier.Low:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = 30;
                    break;
                case DeviceQualityTier.Medium:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = 60;
                    break;
                case DeviceQualityTier.High:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.vSyncCount = 1;
                    Application.targetFrameRate = 60;
                    break;
            }
        }
    }
}
