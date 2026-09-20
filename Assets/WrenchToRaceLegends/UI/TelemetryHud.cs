using UnityEngine;

namespace WTRL.UI
{
    /// <summary>
    /// A minimal telemetry HUD reading `VehicleRuntimeController
    /// .CurrentSnapshot` -- closes the "build a basic telemetry HUD" gap.
    /// Deliberately built with legacy IMGUI (`OnGUI`/`GUILayout`) rather
    /// than a Canvas/UGUI hierarchy: no scene, prefab, or font asset
    /// exists yet to build a real Canvas-based HUD against, and
    /// hand-authoring RectTransform layouts without being able to see
    /// the result would be worse than a plain, honest, functional
    /// IMGUI panel. Treat this as a functional placeholder to be
    /// replaced with real UI once someone can iterate on it visually --
    /// not a finished HUD design.
    /// </summary>
    public sealed class TelemetryHud : MonoBehaviour
    {
        [SerializeField] private VehicleRuntimeController vehicle;
        [SerializeField] private bool visible = true;

        private void Reset()
        {
            vehicle = GetComponent<VehicleRuntimeController>();
        }

        private void OnGUI()
        {
            if (!visible || vehicle == null) return;
            var snapshot = vehicle.CurrentSnapshot;
            if (snapshot == null) return;

            var sim = snapshot.Value.Vehicle;
            GUILayout.BeginArea(new Rect(16, 16, 260, 160), GUI.skin.box);
            GUILayout.Label($"Speed: {sim.SpeedMps * 3.6:F0} km/h");
            GUILayout.Label($"Engine: {sim.EngineRpm:F0} rpm");
            GUILayout.Label($"Gear: {sim.Gear}");
            GUILayout.Label($"Lat G: {sim.LateralAcceleration / 9.81:F2}");
            GUILayout.Label($"Long G: {sim.LongitudinalAcceleration / 9.81:F2}");

            if (snapshot.Value.Diagnostics is { Count: > 0 } diagnostics)
            {
                GUILayout.Space(6);
                foreach (var finding in diagnostics)
                {
                    GUILayout.Label($"[{finding.Severity}] {finding.Observation}");
                }
            }
            GUILayout.EndArea();
        }
    }
}
