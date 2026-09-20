using System.Collections.Generic;

namespace WTRL.World
{
    // Ported from SwiftRacer/Sources/WTRLCore/Content/Definitions.swift.
    // Static hub-world content: tracks, facilities (garage/parts/gas/
    // diner/motorsports complexes), and the counties that group them.

    public sealed class TrackDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public string Kind { get; init; }
        public double LengthM { get; init; }

        /// <summary>Corner banking in degrees, for oval kinds only (0 for
        /// road circuits and drag strips). Applied in
        /// <c>WTRL.Vehicle.VehicleSimulation.Step</c>'s
        /// <c>bankingDegrees</c> parameter to reduce lateral load transfer
        /// in a turn — this is real, physics-affecting content, not
        /// cosmetic metadata.</summary>
        public double BankingDegrees { get; init; }

        public TrackDefinition(string id, string name, string kind, double lengthM, double bankingDegrees = 0)
        {
            Id = id;
            Name = name;
            Kind = kind;
            LengthM = lengthM;
            BankingDegrees = bankingDegrees;
        }
    }

    public sealed class FacilityDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public string Kind { get; init; }
        public IReadOnlyList<string> TrackIds { get; init; } = System.Array.Empty<string>();

        public FacilityDefinition(string id, string name, string kind)
        {
            Id = id;
            Name = name;
            Kind = kind;
        }
    }

    public sealed class CountyDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public IReadOnlyList<string> FacilityIds { get; init; } = System.Array.Empty<string>();

        public CountyDefinition(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
