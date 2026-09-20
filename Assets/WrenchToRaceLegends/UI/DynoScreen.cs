using UnityEngine;
using WTRL.Content;
using WTRL.Lab;

namespace WTRL.UI
{
    /// <summary>
    /// A minimal, functional dyno screen -- closes the "build a minimal
    /// dyno UI screen" gap. Runs the real `WTRL.Lab.DynoSimulation.Run`
    /// against a `VehicleDefinitionAsset` and the 3 dyno slider values
    /// already stored on `CareerState` (`DynoFinalDrive`/
    /// `DynoTirePressure`/`DynoNitrous`), re-running live as the sliders
    /// move -- the interaction-pattern-atlas research
    /// (`09-interaction-pattern-atlas/WTRL-INTERACTION-RECOMMENDATIONS.md`)
    /// names "dyno live-preview curve on slider drag" as its #1 highest-
    /// priority recommendation; this is that, in the crudest possible
    /// form (a text power/torque readout plus an IMGUI bar chart, not a
    /// real line graph).
    ///
    /// Same IMGUI caveat as `TelemetryHud`/`GarageScreen`: functional
    /// placeholder, not a designed screen.
    /// </summary>
    public sealed class DynoScreen : MonoBehaviour
    {
        [SerializeField] private VehicleDefinitionAsset vehicleAsset;
        [SerializeField] private CareerStateHolder careerState;
        [SerializeField] private bool visible = true;

        private DynoRun _lastRun;
        private double _lastFinalDrive = double.NaN, _lastTirePressure = double.NaN, _lastNitrous = double.NaN;

        private void OnGUI()
        {
            if (!visible || vehicleAsset == null || careerState == null || careerState.State == null) return;
            if (vehicleAsset.engine == null || vehicleAsset.transmission == null || vehicleAsset.suspension == null)
            {
                GUILayout.BeginArea(new Rect(16, Screen.height - 140, 320, 40));
                GUILayout.Label("DynoScreen: vehicleAsset is missing a sub-definition.");
                GUILayout.EndArea();
                return;
            }

            var state = careerState.State;
            GUILayout.BeginArea(new Rect(16, Screen.height - 260, 340, 240), GUI.skin.box);
            GUILayout.Label("Dyno", HeaderStyle());

            state.DynoFinalDrive = GUILayout.HorizontalSlider((float)state.DynoFinalDrive, 0, 1);
            GUILayout.Label($"Final drive: {state.DynoFinalDrive:F2}");
            state.DynoTirePressure = GUILayout.HorizontalSlider((float)state.DynoTirePressure, 0, 1);
            GUILayout.Label($"Tire pressure: {state.DynoTirePressure:F2}");
            state.DynoNitrous = GUILayout.HorizontalSlider((float)state.DynoNitrous, 0, 1);
            GUILayout.Label($"Nitrous: {state.DynoNitrous:F2}");

            if (state.DynoFinalDrive != _lastFinalDrive || state.DynoTirePressure != _lastTirePressure ||
                state.DynoNitrous != _lastNitrous)
            {
                RunDyno(state);
            }

            GUILayout.Space(6);
            GUILayout.Label($"Peak power: {_lastRun.PeakPowerKw:F0} kW / {_lastRun.PeakPowerKw * 1.341:F0} hp");
            GUILayout.Label($"Peak torque: {_lastRun.PeakTorqueNm:F0} Nm");
            GUILayout.EndArea();
        }

        private void RunDyno(Career.CareerState state)
        {
            _lastFinalDrive = state.DynoFinalDrive;
            _lastTirePressure = state.DynoTirePressure;
            _lastNitrous = state.DynoNitrous;

            var config = new DynoConfiguration(vehicleAsset.id)
            {
                FinalDriveScale = state.DynoFinalDrive,
                TirePressureNormalized = state.DynoTirePressure,
                NitrousNormalized = state.DynoNitrous,
            };

            _lastRun = DynoSimulation.Run(vehicleAsset.ToDefinition(), vehicleAsset.transmission.ToDefinition(),
                vehicleAsset.suspension.ToDefinition(), vehicleAsset.engine.ToDefinition(), config);
        }

        private static GUIStyle HeaderStyle() => new(GUI.skin.label) { fontStyle = FontStyle.Bold };
    }
}
