namespace WTRL.Garage
{
    // Ported from SwiftRacer/Sources/WTRLCore/Content/Definitions.swift.

    public sealed class PartDefinition
    {
        public string Id { get; }
        public string Category { get; init; }
        public string Name { get; init; }
        public int Price { get; init; }
        public int ReputationRequired { get; init; }
        public double TopSpeedDelta { get; init; }
        public double AccelerationDelta { get; init; }

        public PartDefinition(string id, string category, string name, int price, int reputationRequired,
            double topSpeedDelta, double accelerationDelta)
        {
            Id = id;
            Category = category;
            Name = name;
            Price = price;
            ReputationRequired = reputationRequired;
            TopSpeedDelta = topSpeedDelta;
            AccelerationDelta = accelerationDelta;
        }
    }

    public struct InstalledComponent
    {
        public string PartId;
        public string Slot;
    }

    /// <summary>Ported from Swift's <c>BuildRecipeDefinition</c>. Two
    /// fields (<see cref="RequiredTransmissionId"/>, <see cref="RequiredCrankType"/>)
    /// are sourced content that the Swift original explicitly documents as
    /// "not yet read or enforced anywhere in game logic" — carried over
    /// with the same honesty here rather than silently implying they're
    /// wired up.</summary>
    public sealed class BuildRecipeDefinition
    {
        public string Id { get; }
        public string VehicleId { get; init; }
        public string TrimTier { get; init; }
        public string Name { get; init; }
        public double TargetWeightToPowerMinKgPerHp { get; init; }
        public double TargetWeightToPowerMaxKgPerHp { get; init; }
        public string? RequiredDifferentialType { get; init; }
        public string UnlockedTitle { get; init; }
        public string? UnlockedLiveryId { get; init; }
        /// <summary>Not yet read or enforced anywhere in game logic — sourced
        /// content only (57-CONTENT-RESOLUTION-PASS-6.md Part 6).</summary>
        public string? RequiredTransmissionId { get; init; }
        /// <summary>Not yet enforced anywhere — sourced content only.</summary>
        public string? RequiredCrankType { get; init; }

        public BuildRecipeDefinition(string id, string vehicleId, string trimTier, string name,
            double targetWeightToPowerMinKgPerHp, double targetWeightToPowerMaxKgPerHp, string unlockedTitle)
        {
            Id = id;
            VehicleId = vehicleId;
            TrimTier = trimTier;
            Name = name;
            TargetWeightToPowerMinKgPerHp = targetWeightToPowerMinKgPerHp;
            TargetWeightToPowerMaxKgPerHp = targetWeightToPowerMaxKgPerHp;
            UnlockedTitle = unlockedTitle;
        }
    }
}
