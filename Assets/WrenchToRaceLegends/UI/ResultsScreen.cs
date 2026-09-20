using UnityEngine;
using WTRL.Core;
using WTRL.Events;

namespace WTRL.UI
{
    /// <summary>
    /// A minimal, functional results screen -- closes "no results-
    /// display flow around the 8-state RaceFlowController" from the
    /// world-content gap audit. `RaceFlowController` already models
    /// `RaceFlowPhase.Results` as a real phase and (as of the
    /// Career.RaceCompletionBridge pass) already fires a real
    /// `Completed` event with real classified-time/lap data -- nothing
    /// ever rendered that phase or gave the player a way to leave it.
    /// This screen shows itself only while `Phase == Results`, reads the
    /// real `RaceRuntimeState` (classified time, lap count, best lap,
    /// penalty count), and its one button calls the controller's own
    /// `Complete()` -- it does not invent any new race-flow behavior.
    ///
    /// Same IMGUI caveat as `TelemetryHud`/`GarageScreen`/`DynoScreen`:
    /// functional placeholder, not a designed screen. This project has
    /// no screenshot/visual QA capability, so "functional" here means
    /// verified by driving a real `RaceFlowController` through its full
    /// phase sequence in a test and asserting the screen's own visible-
    /// state and summary-text logic, not by looking at it.
    /// </summary>
    public sealed class ResultsScreen : MonoBehaviour
    {
        [SerializeField] private bool visible = true;

        private RaceFlowController _controller;

        public void Bind(RaceFlowController controller)
        {
            _controller = controller;
        }

        /// <summary>True only while the bound controller is actually in
        /// the Results phase -- exposed so tests can assert visibility
        /// without needing OnGUI/IMGUI to run (IMGUI does not execute in
        /// batchmode test runs).</summary>
        public bool IsShowing => visible && _controller != null && _controller.Phase == RaceFlowPhase.Results;

        /// <summary>The real summary text this screen renders, built from
        /// the controller's actual `RaceRuntimeState` -- separated from
        /// `OnGUI` so it's testable in batchmode.</summary>
        public string BuildSummaryText()
        {
            if (_controller == null) return string.Empty;
            var state = _controller.State;
            var lapCount = state.LapTimes.Count;
            var bestLapText = state.BestLap.HasValue ? $"{state.BestLap.Value:F2}s" : "--";
            return $"{_controller.Definition.Name}\n" +
                   $"{LocalizationTable.Get("results.classifiedTime")}: {state.ClassifiedTime:F2}s\n" +
                   $"{LocalizationTable.Get("results.laps")}: {lapCount}\n" +
                   $"{LocalizationTable.Get("results.bestLap")}: {bestLapText}\n" +
                   $"{LocalizationTable.Get("results.penalties")}: {state.Penalties.Count}";
        }

        private void OnGUI()
        {
            if (!IsShowing) return;

            GUILayout.BeginArea(new Rect(Screen.width / 2f - 160, Screen.height / 2f - 120, 320, 240), GUI.skin.box);
            GUILayout.Label(LocalizationTable.Get("results.title"), HeaderStyle());
            GUILayout.Label(BuildSummaryText());
            GUILayout.Space(8);
            if (GUILayout.Button(LocalizationTable.Get("results.continue")))
            {
                _controller.Complete();
            }
            GUILayout.EndArea();
        }

        private static GUIStyle HeaderStyle() => new(GUI.skin.label) { fontStyle = FontStyle.Bold };
    }
}
