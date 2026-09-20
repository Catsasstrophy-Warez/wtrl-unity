using UnityEngine;
using WTRL.Career;
using WTRL.Events;

namespace WTRL.UI
{
    /// <summary>
    /// Closes "ResultsScreen isn't wired into VerticalSliceSceneBuilder
    /// (no active RaceFlowController there)" -- the real, previously
    /// honest gap named in `UI/CONTRACT.md`. Owns one `RaceFlowController`
    /// for a single vehicle in the scene, drives it through
    /// Loading->Staging->Countdown->Racing every `Update`, detects real
    /// lap completions from the vehicle's own transform, and shows
    /// results once the configured lap count is reached.
    ///
    /// HONEST SCOPE: lap detection is a simple "left the start zone,
    /// then came back within it" radius check on the vehicle's world
    /// position -- an arcade-standard technique, not a real finish-line
    /// plane/timing-loop crossing detector. It has no notion of
    /// direction (driving backward through the start zone counts the
    /// same as a real lap) and no penalty/false-start detection beyond
    /// what `RaceFlowController`/`RaceRules` already do internally.
    /// Good enough to exercise the full real race-flow/results/career
    /// pipeline end to end; not a finished lap-timing system.
    /// </summary>
    public sealed class RaceSessionController : MonoBehaviour
    {
        [SerializeField] private Transform vehicleTransform;
        [SerializeField] private ResultsScreen resultsScreen;
        [SerializeField] private CareerStateHolder careerState;
        [SerializeField] private string raceId = "vertical-slice-club-circuit";
        [SerializeField] private string raceName = "Foundry Row Club Circuit";
        [SerializeField] private string trackId = "foundry-row-circuit";
        [SerializeField] private int laps = 3;
        [SerializeField] private float lapDetectionRadiusM = 15f;
        [SerializeField] private float countdownSeconds = 3f;

        private RaceFlowController _controller;
        private RaceCompletionBridge _bridge;
        private Vector3 _startPosition;
        private bool _hasLeftStartZone;

        public RaceFlowController Controller => _controller;

        private void Start()
        {
            var race = new RaceDefinition(raceId, raceName, trackId, laps, reputationRequired: 0);
            _controller = new RaceFlowController(race);

            if (careerState != null && careerState.State != null)
            {
                _bridge = new RaceCompletionBridge(careerState.State);
                _bridge.AttachTo(_controller);
            }

            if (resultsScreen != null)
            {
                resultsScreen.Bind(_controller);
            }

            _startPosition = vehicleTransform != null ? vehicleTransform.position : Vector3.zero;

            _controller.BeginLoading();
            _controller.FinishLoading();
            _controller.BeginCountdown(countdownSeconds);
        }

        private void Update()
        {
            if (_controller == null) return;

            _controller.Advance(Time.deltaTime);

            if (_controller.Phase == RaceFlowPhase.Finishing)
            {
                _controller.ShowResults();
                return;
            }

            if (_controller.Phase != RaceFlowPhase.Racing || vehicleTransform == null) return;

            var distance = Vector3.Distance(vehicleTransform.position, _startPosition);
            if (distance > lapDetectionRadiusM * 2f)
            {
                _hasLeftStartZone = true;
            }
            else if (_hasLeftStartZone && distance < lapDetectionRadiusM)
            {
                _hasLeftStartZone = false;
                _controller.CompleteLap();
            }
        }

        private void OnDestroy()
        {
            if (_controller != null && _bridge != null)
            {
                _bridge.DetachFrom(_controller);
            }
        }
    }
}
