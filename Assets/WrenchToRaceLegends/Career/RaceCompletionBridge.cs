using System.Collections.Generic;
using WTRL.Core;
using WTRL.Events;

namespace WTRL.Career
{
    /// <summary>
    /// Closes the actual remaining half of the "nothing calls
    /// ReputationState.RecordEvent/SafetyRatingState.RecordEvent/
    /// RivalBehaviorRuntime.RecordResult when a race completes" gap.
    /// <see cref="RaceOutcomeDetail"/> and <see cref="CareerTransaction"/>
    /// already existed and already handle every field correctly -- the
    /// piece that was missing was anything in the race-flow lifecycle
    /// ever constructing one and calling `CareerTransaction.Apply`.
    /// <see cref="RaceFlowController.Completed"/> now fires on race
    /// completion; this type subscribes to it and applies the outcome.
    ///
    /// HONEST LIMITATION, not fixed here: this project has no contact
    /// detection, no rival-position comparison, and no win/loss
    /// determination anywhere in the simulation yet -- `RaceRuntimeState`
    /// tracks lap/sector/penalty timing only. So this bridge can only
    /// honestly populate `RaceId`/`ClassifiedTimeSeconds`/`Format` from
    /// real data; every contact/rival/win field is left at its safe
    /// default (false/null). Concretely, that means: `RivalId` is
    /// deliberately left null even for races with a named rival in
    /// `RaceDefinition.RivalIds` -- setting it with `PlayerWon = false`
    /// would incorrectly record a LOSS against that rival on every single
    /// completion, which is worse than not recording anything. The one
    /// real effect every completion gets is `SafetyEvent
    /// .EventCompletedZeroIncidents` (accurate: no incident of any kind
    /// is or can be detected yet) plus best-classified-time tracking.
    /// Reputation/rival-memory effects stay dormant until a real contact/
    /// win-detection system exists to feed this honestly.
    /// </summary>
    public sealed class RaceCompletionBridge
    {
        private readonly CareerState _careerState;

        public RaceCompletionBridge(CareerState careerState)
        {
            _careerState = careerState;
        }

        public void AttachTo(RaceFlowController controller)
        {
            controller.Completed += OnRaceCompleted;
        }

        public void DetachFrom(RaceFlowController controller)
        {
            controller.Completed -= OnRaceCompleted;
        }

        private void OnRaceCompleted(RaceDefinition definition, RaceRuntimeState state)
        {
            var outcome = new RaceOutcomeDetail(
                raceId: definition.Id,
                classifiedTimeSeconds: state.ClassifiedTime,
                format: definition.Format);

            CareerTransaction.Apply(new[] { CareerCommand.RecordRaceOutcome(outcome) }, _careerState);

            // The first real call site for WTRL.Core.AnalyticsConsent --
            // proves the opt-in gate is actually wired to something, not
            // just infrastructure sitting unused. No-ops entirely unless
            // the player has opted in (default: opted out).
            AnalyticsConsent.Record("race_completed", new Dictionary<string, string>
            {
                ["race_id"] = definition.Id,
                ["format"] = definition.Format.ToString(),
                ["classified_time_seconds"] = state.ClassifiedTime.ToString("F2"),
            });
        }
    }
}
