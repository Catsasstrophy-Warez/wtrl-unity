using WTRL.Events;

namespace WTRL.Career
{
    /// <summary>
    /// Structured race-result data, closing a gap every prior
    /// `CareerTransaction`/`CareerState` CONTRACT.md note flagged as open:
    /// "nothing calls `ReputationState.RecordEvent`/`SafetyRatingState
    /// .RecordEvent`/`RivalBehaviorRuntime.RecordResult` when a race
    /// completes -- needs structured race-result detail (contact, clean
    /// overtakes) that doesn't exist as data anywhere yet." This is that
    /// data. The caller (whatever eventually owns race-session
    /// orchestration -- see `WTRL.Events.RaceRuntimeState`/`RaceRules`,
    /// which this assembly already depends on) is responsible for
    /// populating it honestly from what actually happened during the
    /// race; this type does no detection of its own.
    /// </summary>
    public readonly struct RaceOutcomeDetail
    {
        public readonly string RaceId;
        public readonly double ClassifiedTimeSeconds;
        public readonly RaceFormat Format;

        /// <summary>Null for a solo time-attack/no-rival event. When set,
        /// drives <see cref="RPG.ReputationState.RecordNamedRivalWin"/>
        /// and <see cref="Racing.RivalBehaviorRuntime.RecordResult"/> --
        /// a named-rival win always uses the diminishing-returns path,
        /// regardless of <see cref="Format"/>.</summary>
        public readonly string? RivalId;

        public readonly bool PlayerWon;
        public readonly bool PlayerCausedContact;
        public readonly bool CausedRivalSpinOrRetire;
        public readonly bool CleanOvertakeOccurred;
        public readonly bool OffTrackCutForAdvantage;
        public readonly bool DefensiveHoldNoContact;

        public RaceOutcomeDetail(string raceId, double classifiedTimeSeconds, RaceFormat format,
            string? rivalId = null, bool playerWon = false, bool playerCausedContact = false,
            bool causedRivalSpinOrRetire = false, bool cleanOvertakeOccurred = false,
            bool offTrackCutForAdvantage = false, bool defensiveHoldNoContact = false)
        {
            RaceId = raceId;
            ClassifiedTimeSeconds = classifiedTimeSeconds;
            Format = format;
            RivalId = rivalId;
            PlayerWon = playerWon;
            PlayerCausedContact = playerCausedContact;
            CausedRivalSpinOrRetire = causedRivalSpinOrRetire;
            CleanOvertakeOccurred = cleanOvertakeOccurred;
            OffTrackCutForAdvantage = offTrackCutForAdvantage;
            DefensiveHoldNoContact = defensiveHoldNoContact;
        }
    }
}
