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

    /// <summary>
    /// New type, not a Swift port -- districts/zones (Blackridge's
    /// Freight Docks/Foundry Row/River Bypass/Icehouse Industrial/
    /// Briar/Mountain Crest; San Triana's Crest/Foundry Row/Maritime
    /// Docks/River Interchange/Old Town) were real, named content in the
    /// research corpus (racinggame/DEEP-CONTENT-CATALOG.md) with real
    /// gameplay-affecting properties, but had no corresponding type
    /// anywhere in this Unity port until now -- only counties and
    /// facilities existed. Values below are placeholder scaffolding
    /// (0.5-1.0 ranges chosen to be plausible, not sourced/measured),
    /// same caveat as every other piece of authored-not-sourced content
    /// in this pass.
    /// </summary>
    public sealed class DistrictDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public double DryGripMultiplier { get; init; } = 1.0;
        public double WetGripMultiplier { get; init; } = 0.7;
        public double Roughness { get; init; } = 0.2;
        public double StandingWaterRisk { get; init; } = 0.1;
        public double TrafficDensity { get; init; } = 0.3;
        public double AcousticReflectivity { get; init; } = 0.4;

        public DistrictDefinition(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public sealed class CountyDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public IReadOnlyList<string> FacilityIds { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<string> DistrictIds { get; init; } = System.Array.Empty<string>();

        public CountyDefinition(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
