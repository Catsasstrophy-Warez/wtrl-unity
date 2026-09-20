namespace WTRL.Events
{
    /// <summary>
    /// The 8-state wrapper the Rev16.1 audit recommended layering around
    /// this assembly's real, tested 4-phase <see cref="RaceRuntimeState"/>
    /// core (`Assignments/OUTPUT-Rev16.1-Audit.md`; also flagged in
    /// `RaceRuntime.cs`'s own header comment and every subsequent
    /// CONTRACT.md as "worth layering on top ... once scene-loading and a
    /// results-display flow actually exist"). Those two things still
    /// don't exist as real systems (no scene-loading, no results UI), so
    /// <see cref="Loading"/>/<see cref="Results"/> here are just states a
    /// caller transitions through explicitly — this controller doesn't
    /// itself load a scene or render results, it only tracks that those
    /// steps are where they belong in the flow.
    /// </summary>
    public enum RaceFlowPhase { Inactive, Loading, Staging, Countdown, Racing, Finishing, Results, Complete }

    public sealed class RaceFlowController
    {
        public RaceDefinition Definition { get; }
        public RaceRuntimeState State { get; }
        public RaceFlowPhase Phase { get; private set; } = RaceFlowPhase.Inactive;

        public RaceFlowController(RaceDefinition definition)
        {
            Definition = definition;
            State = new RaceRuntimeState(definition.Id);
        }

        public void BeginLoading()
        {
            if (Phase == RaceFlowPhase.Inactive) Phase = RaceFlowPhase.Loading;
        }

        public void FinishLoading()
        {
            if (Phase == RaceFlowPhase.Loading) Phase = RaceFlowPhase.Staging;
        }

        public void BeginCountdown(double seconds = 3)
        {
            if (Phase != RaceFlowPhase.Staging) return;
            RaceRules.BeginCountdown(State, seconds);
            Phase = RaceFlowPhase.Countdown;
        }

        /// <summary>Advances the underlying <see cref="RaceRuntimeState"/>
        /// and promotes this controller's own phase when the inner state
        /// machine crosses Countdown → Running. A no-op outside
        /// Countdown/Racing, matching every <see cref="RaceRules"/> method's
        /// own phase-guard discipline.</summary>
        public void Advance(double dt)
        {
            if (Phase != RaceFlowPhase.Countdown && Phase != RaceFlowPhase.Racing) return;
            RaceRules.Advance(State, dt);
            if (Phase == RaceFlowPhase.Countdown && State.Phase == RacePhase.Running)
            {
                Phase = RaceFlowPhase.Racing;
            }
        }

        public void CompleteSector()
        {
            if (Phase == RaceFlowPhase.Racing) RaceRules.CompleteSector(State);
        }

        public void CompleteLap()
        {
            if (Phase != RaceFlowPhase.Racing) return;
            RaceRules.CompleteLap(State, Definition);
            if (State.Phase == RacePhase.Finished) Phase = RaceFlowPhase.Finishing;
        }

        public void ShowResults()
        {
            if (Phase == RaceFlowPhase.Finishing) Phase = RaceFlowPhase.Results;
        }

        public void Complete()
        {
            if (Phase == RaceFlowPhase.Results) Phase = RaceFlowPhase.Complete;
        }
    }
}
