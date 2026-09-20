using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WTRL.Career;
using WTRL.Garage;
using WTRL.Racing;

namespace WTRL.Persistence
{
    /// <summary>
    /// Ported from SwiftRacer/Sources/WTRLCore/Persistence/CareerSave.swift
    /// — the save DTO shape and its lenient-decode discipline (every field
    /// optional with a default, so any save shape round-trips through one
    /// code path) — plus CareerSaveCodec's encode/decode.
    ///
    /// DEVIATION: uses System.Text.Json (built into .NET/Unity's
    /// framework, no external package) instead of Swift's
    /// JSONEncoder/JSONDecoder. `IncludeFields = true` is required in
    /// `Options` below because `InstalledComponent`/`RivalMemory` are
    /// ported as structs with public FIELDS (matching their Swift
    /// originals' `var` properties, but C# structs here use fields, not
    /// auto-properties) — System.Text.Json ignores fields by default.
    ///
    /// DEVIATION: `runEvidence`/`dynoRuns`/`ghostReplays` from the Swift
    /// original are NOT in this DTO. `runEvidence`/`dynoRuns` need
    /// `WTRL.Lab` types (`TestRunEvidence`/`DynoRun`), and
    /// `ghostReplays` needs a `DeterministicReplay` type that hasn't been
    /// ported anywhere yet — `WTRL.Persistence` has no dependency on
    /// `WTRL.Lab`, matching `WTRL.Career`'s own deferral of
    /// `.recordEvidence` for the identical reason (see `Career/
    /// CONTRACT.md`). Flagged here, not silently dropped.
    /// </summary>
    public sealed class CareerSaveDto
    {
        public int SchemaVersion { get; set; } = 3;
        public int Money { get; set; } = 5000;
        public int Reputation { get; set; }
        public List<string> OwnedPartIds { get; set; } = new();
        public Dictionary<string, List<InstalledComponent>> InstalledComponents { get; set; } = new();
        public bool LicensePassed { get; set; }
        public string SelectedVehicleId { get; set; } = "hero-1965";
        public List<string> OwnedVehicleIds { get; set; } = new() { "hero-1965" };
        public List<string> CompletedRaceIds { get; set; } = new();
        public Dictionary<string, double> RaceRecords { get; set; } = new();
        public double DynoFinalDrive { get; set; } = 0.5;
        public double DynoTirePressure { get; set; } = 0.5;
        public double DynoNitrous { get; set; } = 0.3;
        public Dictionary<string, List<VehicleHistoryEvent>> VehicleHistory { get; set; } = new();
        public Dictionary<string, RivalMemory> RivalMemories { get; set; } = new();
    }

    public static class CareerSaveCodec
    {
        private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };

        public static string Encode(CareerState state)
        {
            var dto = new CareerSaveDto
            {
                SchemaVersion = 3,
                Money = state.Money,
                Reputation = state.Reputation,
                OwnedPartIds = state.OwnedPartIds.ToList(),
                InstalledComponents = state.InstalledComponents,
                LicensePassed = state.LicensePassed,
                SelectedVehicleId = state.SelectedVehicleId,
                OwnedVehicleIds = state.OwnedVehicleIds.ToList(),
                CompletedRaceIds = state.CompletedRaceIds.ToList(),
                RaceRecords = state.RaceRecords,
                DynoFinalDrive = state.DynoFinalDrive,
                DynoTirePressure = state.DynoTirePressure,
                DynoNitrous = state.DynoNitrous,
                VehicleHistory = state.VehicleHistory,
                RivalMemories = new Dictionary<string, RivalMemory>(state.RivalBehavior.Snapshot().Memories),
            };
            return JsonSerializer.Serialize(dto, Options);
        }

        /// <summary>Every field is read leniently (a missing/null value
        /// falls back to a default) so any save shape — old or new,
        /// missing keys entirely — decodes through this one path, matching
        /// the Swift original's `decodeIfPresent ?? default` discipline
        /// applied field-by-field rather than failing the whole
        /// decode.</summary>
        public static CareerState Decode(string json)
        {
            CareerSaveDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<CareerSaveDto>(json, Options);
            }
            catch (JsonException)
            {
                dto = null;
            }
            dto ??= new CareerSaveDto();

            // The Swift original's hero-gen1 -> hero-1965 rename — this
            // leniency means the migration must be applied here, on every
            // decode, rather than relying on a separate legacy-path
            // fallback the way an earlier Swift schema version did.
            var decodedVehicleId = dto.SelectedVehicleId;
            var selectedVehicleId = decodedVehicleId == "hero-gen1" ? "hero-1965" : (decodedVehicleId ?? "hero-1965");

            var state = new CareerState
            {
                Money = dto.Money,
                Reputation = dto.Reputation,
                LicensePassed = dto.LicensePassed,
                SelectedVehicleId = selectedVehicleId,
                DynoFinalDrive = dto.DynoFinalDrive,
                DynoTirePressure = dto.DynoTirePressure,
                DynoNitrous = dto.DynoNitrous,
            };

            foreach (var id in dto.OwnedPartIds ?? new List<string>()) state.OwnedPartIds.Add(id);
            foreach (var kv in dto.InstalledComponents ?? new Dictionary<string, List<InstalledComponent>>())
            {
                state.InstalledComponents[kv.Key] = kv.Value;
            }
            foreach (var id in dto.OwnedVehicleIds ?? new List<string> { "hero-1965" }) state.OwnedVehicleIds.Add(id);
            foreach (var id in dto.CompletedRaceIds ?? new List<string>()) state.CompletedRaceIds.Add(id);
            foreach (var kv in dto.RaceRecords ?? new Dictionary<string, double>()) state.RaceRecords[kv.Key] = kv.Value;
            foreach (var kv in dto.VehicleHistory ?? new Dictionary<string, List<VehicleHistoryEvent>>())
            {
                state.VehicleHistory[kv.Key] = kv.Value;
            }
            state.RivalBehavior.Restore(dto.RivalMemories ?? new Dictionary<string, RivalMemory>());

            // Matches the Swift original's own safety guarantee: whatever
            // car is selected must be in the owned set, even for
            // minimal/old save shapes that never explicitly owned it.
            state.OwnedVehicleIds.Add(state.SelectedVehicleId);

            return state;
        }
    }
}
