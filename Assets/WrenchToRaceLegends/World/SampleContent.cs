using System.Collections.Generic;

namespace WTRL.World
{
    /// <summary>
    /// The project's first authored (not fixture-only) world content --
    /// closes the "build the whole documented world" gap. Every named
    /// location below is a REAL name from the research corpus
    /// (racinggame/DEEP-CONTENT-CATALOG.md's "Complete named locations"
    /// section) -- this is not invented place-naming. What IS invented:
    /// which specific track lives at which named facility, and every
    /// district's grip/traffic/acoustic numbers (the corpus names the
    /// properties a district should carry, not their actual values).
    ///
    /// The corpus explicitly avoids replicating real-world track
    /// layouts, describing 3 "original road-course principles" instead
    /// (forest elevation, coastal signature-drop, technical tight) with
    /// no proper names given. The 3 names below (Whisperwood Forest
    /// Circuit, Cliffside Coastal Circuit, Ironclad Technical Circuit)
    /// are this pass's own naming, not sourced -- flagged the same way
    /// the rival-generation names were.
    ///
    /// See WTRL.Racing.SampleContent for the matching AI racing-line
    /// waypoints every track here needs to actually be driven.
    /// </summary>
    public static class SampleContent
    {
        public const string FoundryRowCircuitId = "foundry-row-circuit";
        public const string RedlineRacewayId = "redline-raceway";
        public const string CutbackTriOvalId = "cutback-tri-oval";
        public const string LongbowSpeedwayId = "longbow-speedway";
        public const string HighbankSuperspeedwayId = "highbank-superspeedway";
        public const string WhisperwoodForestCircuitId = "whisperwood-forest-circuit";
        public const string CliffsideCoastalCircuitId = "cliffside-coastal-circuit";
        public const string IroncladTechnicalCircuitId = "ironclad-technical-circuit";

        // ---- Tracks ----

        /// <summary>Flat road circuit (0 banking) -- a rough rectangle
        /// with two hairpin-style corners, sized for a short original
        /// circuit rather than a full-scale track. Length is the actual
        /// polyline length of <see cref="Racing.SampleContent
        /// .FoundryRowCircuitLine"/>'s nodes, not an independent
        /// estimate -- see that method's test coverage for the
        /// cross-check.</summary>
        public static TrackDefinition FoundryRowCircuit() =>
            new(FoundryRowCircuitId, "Foundry Row Circuit", "circuit", lengthM: 633.1, bankingDegrees: 0);

        /// <summary>Quarter-mile drag strip -- the corpus's own
        /// "period-correct for the 1965-era setting" distance, and the
        /// distance <see cref="Events.DragRaceRules"/> already finishes
        /// a run at.</summary>
        public static TrackDefinition RedlineRaceway() =>
            new(RedlineRacewayId, "Redline Raceway", "drag", lengthM: 402.336, bankingDegrees: 0);

        /// <summary>¾-mile short-track oval, corpus's "steep banking
        /// envelope" class.</summary>
        public static TrackDefinition CutbackTriOval() =>
            new(CutbackTriOvalId, "Cutback Tri-Oval", "oval", lengthM: 1201.17, bankingDegrees: 20);

        /// <summary>1.5-mile intermediate oval class.</summary>
        public static TrackDefinition LongbowSpeedway() =>
            new(LongbowSpeedwayId, "Longbow Speedway", "oval", lengthM: 2400.33, bankingDegrees: 24);

        /// <summary>2.5-mile superspeedway class -- per the corpus's own
        /// note, banking is deliberately not a free grip multiplier at
        /// this scale; power-to-drag is the limiting factor, which is
        /// exactly how `VehicleSimulation.Step`'s aero drag term already
        /// behaves regardless of `bankingDegrees`.</summary>
        public static TrackDefinition HighbankSuperspeedway() =>
            new(HighbankSuperspeedwayId, "Highbank Superspeedway", "oval", lengthM: 4001.5, bankingDegrees: 28);

        /// <summary>The corpus's "long forest elevation circuit"
        /// principle (blind crests, dense trees, large elevation change,
        /// low sightlines) -- elevation itself isn't modeled by anything
        /// in this project yet (no terrain/heightmap system exists), so
        /// this is a flat-plane placeholder for the shape only.</summary>
        public static TrackDefinition WhisperwoodForestCircuit() =>
            new(WhisperwoodForestCircuitId, "Whisperwood Forest Circuit", "circuit", lengthM: 2059.1, bankingDegrees: 0);

        /// <summary>The corpus's "coastal signature-drop circuit"
        /// principle (right-left sequence, steep single drop, cliff
        /// environment). Same elevation caveat as Whisperwood.</summary>
        public static TrackDefinition CliffsideCoastalCircuit() =>
            new(CliffsideCoastalCircuitId, "Cliffside Coastal Circuit", "circuit", lengthM: 1561.2, bankingDegrees: 0);

        /// <summary>The corpus's "technical tight circuit" principle
        /// (fast uphill compression, crossing/figure-eight pattern,
        /// tight barrier-close infield).</summary>
        public static TrackDefinition IroncladTechnicalCircuit() =>
            new(IroncladTechnicalCircuitId, "Ironclad Technical Circuit", "circuit", lengthM: 842.8, bankingDegrees: 0);

        public static IReadOnlyList<TrackDefinition> AllTracks() => new[]
        {
            FoundryRowCircuit(), RedlineRaceway(), CutbackTriOval(), LongbowSpeedway(), HighbankSuperspeedway(),
            WhisperwoodForestCircuit(), CliffsideCoastalCircuit(), IroncladTechnicalCircuit(),
        };

        // ---- Facilities (real names, from DEEP-CONTENT-CATALOG.md's
        // "Named race facilities" list, plus 2 generic service facilities
        // needed for the vertical slice) ----

        public static FacilityDefinition RedlineRacewayFacility() =>
            new("redline-raceway-facility", "Redline Raceway", "dragstrip") { TrackIds = new[] { RedlineRacewayId } };

        public static FacilityDefinition CutbackTriOvalFacility() =>
            new("cutback-tri-oval-facility", "Cutback Tri-Oval", "oval") { TrackIds = new[] { CutbackTriOvalId } };

        public static FacilityDefinition LongbowSpeedwayFacility() =>
            new("longbow-speedway-facility", "Longbow Speedway", "oval") { TrackIds = new[] { LongbowSpeedwayId } };

        public static FacilityDefinition HighbankSuperspeedwayFacility() =>
            new("highbank-superspeedway-facility", "Highbank Superspeedway", "oval") { TrackIds = new[] { HighbankSuperspeedwayId } };

        /// <summary>Groups the 4 original road-course circuits under one
        /// facility -- the corpus doesn't name a specific road-course
        /// park, so this facility name is this pass's own invention,
        /// unlike the 4 named facilities above.</summary>
        public static FacilityDefinition BlackridgeRoadCoursePark() =>
            new("blackridge-road-course-park", "Blackridge Road Course Park", "roadcourse")
            {
                TrackIds = new[]
                {
                    FoundryRowCircuitId, WhisperwoodForestCircuitId, CliffsideCoastalCircuitId, IroncladTechnicalCircuitId,
                },
            };

        public static FacilityDefinition DowntownGarage() => new("downtown-garage", "Downtown Garage", "garage");

        public static FacilityDefinition FoundryRowGasStation() =>
            new("foundry-row-gas-station", "Foundry Row Gas Station", "gas-station");

        public static IReadOnlyList<FacilityDefinition> BlackridgeFacilities() => new[]
        {
            RedlineRacewayFacility(), CutbackTriOvalFacility(), LongbowSpeedwayFacility(), HighbankSuperspeedwayFacility(),
            BlackridgeRoadCoursePark(), DowntownGarage(), FoundryRowGasStation(),
        };

        // ---- Districts (real names, from DEEP-CONTENT-CATALOG.md's
        // "Blackridge County districts" / "San Triana County zones"
        // lists; numeric properties are placeholder scaffolding -- see
        // DistrictDefinition's own doc comment) ----

        public static IReadOnlyList<DistrictDefinition> BlackridgeDistricts() => new[]
        {
            new DistrictDefinition("blackridge-freight-docks", "Freight Docks") { TrafficDensity = 0.6, Roughness = 0.4 },
            new DistrictDefinition("blackridge-foundry-row", "Foundry Row") { Roughness = 0.5, AcousticReflectivity = 0.6 },
            new DistrictDefinition("blackridge-river-bypass", "River Bypass") { StandingWaterRisk = 0.4, DryGripMultiplier = 0.95 },
            new DistrictDefinition("blackridge-icehouse-industrial", "Icehouse Industrial") { TrafficDensity = 0.5, Roughness = 0.45 },
            new DistrictDefinition("blackridge-briar", "Briar") { TrafficDensity = 0.15, DryGripMultiplier = 1.0 },
            new DistrictDefinition("blackridge-mountain-crest", "Mountain Crest") { WetGripMultiplier = 0.5, StandingWaterRisk = 0.3 },
        };

        public static IReadOnlyList<DistrictDefinition> SanTrianaDistricts() => new[]
        {
            new DistrictDefinition("santriana-crest", "Crest") { WetGripMultiplier = 0.55 },
            new DistrictDefinition("santriana-foundry-row", "Foundry Row") { Roughness = 0.5, AcousticReflectivity = 0.6 },
            new DistrictDefinition("santriana-maritime-docks", "Maritime Docks") { StandingWaterRisk = 0.5, AcousticReflectivity = 0.5 },
            new DistrictDefinition("santriana-river-interchange", "River Interchange") { TrafficDensity = 0.7 },
            new DistrictDefinition("santriana-old-town", "Old Town") { Roughness = 0.55, TrafficDensity = 0.5 },
        };

        // ---- Counties ----

        /// <summary>The primary setting -- also the county
        /// PROJECT-MAP-UNITY-MOBILE.md's vertical slice targets.</summary>
        public static CountyDefinition BlackridgeCounty()
        {
            var facilityIds = new List<string>();
            foreach (var f in BlackridgeFacilities()) facilityIds.Add(f.Id);
            var districtIds = new List<string>();
            foreach (var d in BlackridgeDistricts()) districtIds.Add(d.Id);
            return new CountyDefinition("blackridge-county", "Blackridge County")
            {
                FacilityIds = facilityIds,
                DistrictIds = districtIds,
            };
        }

        /// <summary>A second county named in the research corpus. No
        /// facilities are named against San Triana anywhere in the
        /// corpus (only its districts/zones are), so
        /// <see cref="CountyDefinition.FacilityIds"/> is honestly empty
        /// here rather than guessed.</summary>
        public static CountyDefinition SanTrianaCounty()
        {
            var districtIds = new List<string>();
            foreach (var d in SanTrianaDistricts()) districtIds.Add(d.Id);
            return new CountyDefinition("san-triana-county", "San Triana County") { DistrictIds = districtIds };
        }
    }
}
