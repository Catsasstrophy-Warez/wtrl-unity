using System.Collections.Generic;

namespace WTRL.Events
{
    // Ported from SwiftRacer/Sources/WTRLCore/Content/Definitions.swift.
    // Exactly 5 formats -- no "drag" case. Several external uploads this
    // project absorbed tried to reintroduce a RaceFormat.drag case from a
    // stale/regressed lineage; that lineage was rejected every time (see
    // SwiftRacer's own RaceContentIntegrityTests.swift header comment).
    // Drag races here use RaceDisciplineRuntime's separate
    // DragRaceRuntimeState/DragRaceRules, not this format enum.

    public enum RaceFormat { Circuit, Touge, Knockout, PursuitChase, PursuitEscape }

    public sealed class RaceDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public string TrackId { get; init; }
        public int Laps { get; init; }
        public int ReputationRequired { get; init; }
        public RaceFormat Format { get; init; } = RaceFormat.Circuit;
        public IReadOnlyList<string> RivalIds { get; init; } = System.Array.Empty<string>();
        public string WinConditionDescription { get; init; } = string.Empty;

        public RaceDefinition(string id, string name, string trackId, int laps, int reputationRequired)
        {
            Id = id;
            Name = name;
            TrackId = trackId;
            Laps = laps;
            ReputationRequired = reputationRequired;
        }
    }
}
