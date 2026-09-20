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
        public string Kind { get; }
        public string Summary { get; }
        public string? ConfigurationFingerprint { get; init; }

        // Constructor-enforced required fields, not the C# 11 `required`
        // keyword -- see WTRL.RPG/BuildRecipeProgress.cs's identical note
        // for why.
        public VehicleHistoryEvent(string kind, string summary)
        {
            Kind = kind;
            Summary = summary;
        }
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

        // Plain save fields with no CareerCommand of their own — ported
        // from SwiftRacer's CareerSave, which has these as ordinary
        // mutable values outside its own transaction system too (its
        // CareerCommand enum has no case for any of these either).
        public bool LicensePassed { get; set; }
        public string SelectedVehicleId { get; set; } = "hero-1965";
        public HashSet<string> OwnedVehicleIds { get; private set; } = new() { "hero-1965" };
        public double DynoFinalDrive { get; set; } = 0.5;
        public double DynoTirePressure { get; set; } = 0.5;
        public double DynoNitrous { get; set; } = 0.3;

        public RivalBehaviorRuntime RivalBehavior { get; private set; } = new();
        public ReputationState ReputationState { get; private set; } = new();
        public SafetyRatingState SafetyRating { get; private set; } = new();
        public DriverLicenseState DriverLicense { get; private set; } = new();
        public List<SavedBuildRecipe> SavedRecipes { get; private set; } = new();

        /// <summary>Deep copy of every field — used by
        /// <see cref="CareerTransaction.Apply"/> to build a candidate
        /// state that mutations apply to, so a failed command in the
        /// middle of a batch never leaves the real state partially
        /// mutated. See that method's doc for why this matters (it's the
        /// whole point of the original Swift design).
        ///
        /// IMPORTANT for future edits: this deliberately copies EVERY
        /// field, not just the ones <see cref="CareerTransaction"/>'s
        /// commands currently touch. An earlier version of this method
        /// only copied the transaction-relevant fields, on the reasoning
        /// that nothing else needed atomicity — but <see cref="Clone"/>'s
        /// object-initializer syntax means any field NOT explicitly
        /// listed silently resets to its default rather than copying from
        /// <c>this</c>. That would have meant every single
        /// <see cref="CareerTransaction.Apply"/> call reset
        /// <see cref="SelectedVehicleId"/>/<see cref="OwnedVehicleIds"/>/
        /// dyno slider values back to their defaults — caught before it
        /// shipped, not after. If you add a new field to
        /// <see cref="CareerState"/>, add it here and to
        /// <see cref="CopyFrom"/> in the SAME edit, or it WILL silently
        /// reset on every transaction.
        /// <see cref="RivalBehavior"/>/<see cref="ReputationState"/>/
        /// <see cref="SafetyRating"/>/<see cref="DriverLicense"/> are now
        /// deep-cloned via their own <c>Clone()</c> methods (added
        /// alongside <c>CareerCommand.RecordRaceOutcome</c>, the first
        /// command to actually mutate them mid-transaction — see that
        /// command's handler in <see cref="CareerTransaction"/> and each
        /// type's own <c>Clone()</c> doc comment). <see cref="SavedRecipes"/>
        /// remains reference-copied (a shallow list-of-reference-types
        /// copy would still alias the same <c>SavedBuildRecipe</c>
        /// instances) since no command mutates it yet — if one starts to,
        /// give it the same treatment.</summary>
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
                LicensePassed = LicensePassed,
                SelectedVehicleId = SelectedVehicleId,
                OwnedVehicleIds = new HashSet<string>(OwnedVehicleIds),
                DynoFinalDrive = DynoFinalDrive,
                DynoTirePressure = DynoTirePressure,
                DynoNitrous = DynoNitrous,
                RivalBehavior = RivalBehavior.Clone(),
                ReputationState = ReputationState.Clone(),
                SafetyRating = SafetyRating.Clone(),
                DriverLicense = DriverLicense.Clone(),
                SavedRecipes = SavedRecipes,
            };
            return copy;
        }

        /// <summary>Copies every field <see cref="Clone"/> copies back from
        /// <paramref name="other"/> into this instance — the "commit" half
        /// of the clone-mutate-commit pattern. Same warning as
        /// <see cref="Clone"/>: keep this in sync with it and with the
        /// property list above.</summary>
        public void CopyFrom(CareerState other)
        {
            Money = other.Money;
            Reputation = other.Reputation;
            OwnedPartIds = other.OwnedPartIds;
            InstalledComponents = other.InstalledComponents;
            CompletedRaceIds = other.CompletedRaceIds;
            RaceRecords = other.RaceRecords;
            VehicleHistory = other.VehicleHistory;
            LicensePassed = other.LicensePassed;
            SelectedVehicleId = other.SelectedVehicleId;
            OwnedVehicleIds = other.OwnedVehicleIds;
            DynoFinalDrive = other.DynoFinalDrive;
            DynoTirePressure = other.DynoTirePressure;
            DynoNitrous = other.DynoNitrous;
            RivalBehavior = other.RivalBehavior;
            ReputationState = other.ReputationState;
            SafetyRating = other.SafetyRating;
            DriverLicense = other.DriverLicense;
        }
    }
}
