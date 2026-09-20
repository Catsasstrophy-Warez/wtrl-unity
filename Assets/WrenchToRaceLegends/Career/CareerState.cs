using System;
using System.Collections.Generic;
using System.Linq;
using WTRL.Garage;
using WTRL.Racing;
using WTRL.RPG;

namespace WTRL.Career
{
    // New design tying together WTRL.Garage/WTRL.Racing/WTRL.Events/
    // WTRL.RPG per PIVOT-PLAN.md's description of this assembly's job.
    // VehicleHistoryEvent is ported from SwiftRacer/Sources/WTRLCore/
    // Runtime/DeveloperLab.swift (a Runtime-layer file there, but the
    // type itself is plain data with no dependency on anything
    // Runtime-specific, so it's ported here where its one real consumer
    // -- CareerTransaction's RecordHistory command -- actually lives).

    public sealed class VehicleHistoryEvent
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public double SimulationSeconds { get; init; }
        public required string Kind { get; init; }
        public required string Summary { get; init; }
        public string? ConfigurationFingerprint { get; init; }
    }

    /// <summary>
    /// The runtime career model — money, reputation, owned parts,
    /// installed components per vehicle, completed races/records, vehicle
    /// history, and the RPG/Racing state that lives alongside it
    /// (<see cref="RivalBehaviorRuntime"/>, <see cref="ReputationState"/>,
    /// <see cref="SafetyRatingState"/>, <see cref="DriverLicenseState"/>,
    /// saved build recipes). This is the in-memory model — serialization
    /// to/from a save file is <c>WTRL.Persistence</c>'s job, matching the
    /// same layering SwiftRacer used (<c>CareerSave</c> as the DTO,
    /// separate from the runtime <c>GameState</c> that mutates it).
    /// </summary>
    public sealed class CareerState
    {
        public int Money { get; set; } = 5000;
        public int Reputation { get; set; }
        public HashSet<string> OwnedPartIds { get; private set; } = new();
        public Dictionary<string, List<InstalledComponent>> InstalledComponents { get; private set; } = new();
        public HashSet<string> CompletedRaceIds { get; private set; } = new();
        public Dictionary<string, double> RaceRecords { get; private set; } = new();
        public Dictionary<string, List<VehicleHistoryEvent>> VehicleHistory { get; private set; } = new();

        public RivalBehaviorRuntime RivalBehavior { get; private set; } = new();
        public ReputationState ReputationState { get; private set; } = new();
        public SafetyRatingState SafetyRating { get; private set; } = new();
        public DriverLicenseState DriverLicense { get; private set; } = new();
        public List<SavedBuildRecipe> SavedRecipes { get; private set; } = new();

        /// <summary>Deep copy — used by <see cref="CareerTransaction.Apply"/>
        /// to build a candidate state that mutations apply to, so a
        /// failed command in the middle of a batch never leaves the real
        /// state partially mutated. See that method's doc for why this
        /// matters (it's the whole point of the original Swift design).
        /// Note: <see cref="RivalBehavior"/>/<see cref="ReputationState"/>/
        /// etc. are NOT deep-cloned here (those types don't expose the
        /// internals a clone would need, and no <see cref="CareerCommand"/>
        /// currently mutates them) — only the fields
        /// <see cref="CareerTransaction"/> actually touches are cloned.
        /// If a future command needs to mutate one of those atomically
        /// too, this method needs extending first.</summary>
        public CareerState Clone()
        {
            var copy = new CareerState
            {
                Money = Money,
                Reputation = Reputation,
                OwnedPartIds = new HashSet<string>(OwnedPartIds),
                InstalledComponents = InstalledComponents.ToDictionary(kv => kv.Key, kv => new List<InstalledComponent>(kv.Value)),
                CompletedRaceIds = new HashSet<string>(CompletedRaceIds),
                RaceRecords = new Dictionary<string, double>(RaceRecords),
                VehicleHistory = VehicleHistory.ToDictionary(kv => kv.Key, kv => new List<VehicleHistoryEvent>(kv.Value)),
                RivalBehavior = RivalBehavior,
                ReputationState = ReputationState,
                SafetyRating = SafetyRating,
                DriverLicense = DriverLicense,
                SavedRecipes = SavedRecipes,
            };
            return copy;
        }

        /// <summary>Copies every field <see cref="Clone"/> copies back from
        /// <paramref name="other"/> into this instance — the "commit" half
        /// of the clone-mutate-commit pattern.</summary>
        public void CopyFrom(CareerState other)
        {
            Money = other.Money;
            Reputation = other.Reputation;
            OwnedPartIds = other.OwnedPartIds;
            InstalledComponents = other.InstalledComponents;
            CompletedRaceIds = other.CompletedRaceIds;
            RaceRecords = other.RaceRecords;
            VehicleHistory = other.VehicleHistory;
        }
    }
}
