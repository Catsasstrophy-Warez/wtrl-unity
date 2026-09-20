using System;
using WTRL.Events;

namespace WTRL.Career
{
    /// <summary>
    /// New orchestration type, not a Swift port: wires WTRL.Events'
    /// RaceRules/RaceRuntimeState state machine to a CareerCommand at the
    /// point a race finishes. This is what steps "wire RaceRules into a
    /// session" and "wire race completion into Career/RPG" (long-flagged
    /// open items across Events/Career/RPG's CONTRACT.md files) actually
    /// look like as code, kept deliberately thin: it owns phase/lap
    /// progression bookkeeping, nothing about scene loading, rendering,
    /// or checkpoint/collision detection (those don't exist as systems
    /// yet -- see the honesty note on <see cref="BuildOutcomeCommand"/>).
    /// </summary>
    public sealed class RaceSession
    {
        public RaceDefinition Definition { get; }
        public RaceRuntimeState State { get; }

        public RaceSession(RaceDefinition definition)
        {
            Definition = definition;
            State = new RaceRuntimeState(definition.Id);
        }

        public void BeginCountdown(double seconds = 3) => RaceRules.BeginCountdown(State, seconds);
        public void Advance(double dt) => RaceRules.Advance(State, dt);
        public void RegisterFalseStart() => RaceRules.RegisterFalseStart(State);
        public void CompleteSector() => RaceRules.CompleteSector(State);
        public void CompleteLap() => RaceRules.CompleteLap(State, Definition);

        public bool IsFinished => State.Phase == RacePhase.Finished;

        /// <summary>
        /// Builds the <see cref="CareerCommand.RecordRaceOutcome"/> command
        /// for this session's finished state. The conduct flags
        /// (<paramref name="playerCausedContact"/> etc.) must be supplied
        /// by the caller -- this session has no collision/track-limit
        /// detection of its own; nothing in this project produces that
        /// signal yet (it needs real scene colliders, which don't exist).
        /// Passing all-default (clean) flags is honest only when the
        /// caller actually has no way to know otherwise; a caller that
        /// DOES have collision data must pass it through, not default it
        /// away for convenience.
        /// </summary>
        public CareerCommand BuildOutcomeCommand(bool playerWon, string? rivalId = null,
            bool playerCausedContact = false, bool causedRivalSpinOrRetire = false,
            bool cleanOvertakeOccurred = false, bool offTrackCutForAdvantage = false,
            bool defensiveHoldNoContact = false)
        {
            if (!IsFinished)
            {
                throw new InvalidOperationException(
                    "Cannot build a race outcome before the race has finished -- check IsFinished first.");
            }

            var outcome = new RaceOutcomeDetail(Definition.Id, State.ClassifiedTime, Definition.Format,
                rivalId, playerWon, playerCausedContact, causedRivalSpinOrRetire, cleanOvertakeOccurred,
                offTrackCutForAdvantage, defensiveHoldNoContact);
            return CareerCommand.RecordRaceOutcome(outcome);
        }
    }
}
