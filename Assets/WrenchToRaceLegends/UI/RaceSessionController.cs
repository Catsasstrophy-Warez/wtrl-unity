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
    ///
    /// REAL BUG FIXED (2026-09-20, found by a deep-dive review, not by
    /// symptom): the "left zone" and "returned to zone" thresholds used
    /// to be a single `lapDetectionRadiusM` field with a hardcoded 2x
    /// multiplier baked into the away-check -- a vehicle had to travel
    /// more than `lapDetectionRadiusM * 2` (30m at the shipped default)
    /// from the start position before a lap could register AT ALL. On
    /// any track whose loop is smaller than that (a tight autocross
    /// layout, a short oval, or simply a smaller `lapDetectionRadiusM`
    /// tuned for a tighter track), `_hasLeftStartZone` could never
    /// become true and the race would hang in `Racing` phase forever --
    /// a real correctness bug, not exercised by Foundry Row's current
    /// scale but latent for any future track/config. Fixed by exposing
    /// the two thresholds as independent fields
    /// (`returnRadiusM`/`departureRadiusM`) with a loud
    /// `Debug.LogError` in `Start()` if `departureRadiusM` isn't
    /// meaningfully larger than `returnRadiusM`, instead of a silent
    /// hang.
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
        [SerializeField] private float returnRadiusM = 15f;
        [SerializeField] private float departureRadiusM = 30f;
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

            if (departureRadiusM <= returnRadiusM)
            {
                // Loud, not silent: with the old hardcoded-multiplier
                // design this misconfiguration would have hung the race
                // in Racing phase forever with no error at all. A
                // vehicle can never register "departed" if the
                // departure threshold isn't meaningfully farther out
                // than the return threshold.
                Debug.LogError($"RaceSessionController: departureRadiusM ({departureRadiusM}) must be greater " +
                    $"than returnRadiusM ({returnRadiusM}), or laps can never be detected. Using a safe fallback " +
                    "(2x returnRadiusM) for this session.");
                departureRadiusM = returnRadiusM * 2f;
            }

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
            if (distance > departureRadiusM)
            {
                _hasLeftStartZone = true;
            }
            else if (_hasLeftStartZone && distance < returnRadiusM)
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
