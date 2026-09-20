namespace WTRL.Garage
{
    /// <summary>
    /// The project's first fully authored, end-to-end-satisfiable
    /// `BuildRecipeDefinition` — closes the "author and satisfy one full
    /// build recipe" gap (0 of 35 spec'd recipes existed before this).
    /// Numbers are sized against the hero-1965 fixture values already
    /// used throughout `Tests/EditMode` (1450kg / 420hp = ~3.45 kg/hp),
    /// not sourced research-corpus data — see `HeroContentBuilder.cs`'s
    /// identical caveat.
    /// </summary>
    public static class SampleContent
    {
        /// <summary>The only 3 part ids `VehicleConfigurationResolver
        /// .Resolve` actually recognizes (see that class's own doc
        /// comment) — a real, closed catalog rather than parts that
        /// would silently no-op when installed. Prices/reputation gates
        /// are placeholder numbers, not sourced content.</summary>
        public static System.Collections.Generic.IReadOnlyList<PartDefinition> RecognizedParts { get; } = new[]
        {
            new PartDefinition("final-drive-373", "Differential", "3.73 Final Drive", price: 450,
                reputationRequired: 0, topSpeedDelta: -3, accelerationDelta: 0.15),
            new PartDefinition("sport-tire", "Tire", "Sport Tire", price: 600,
                reputationRequired: 0, topSpeedDelta: 0, accelerationDelta: 0.05),
            new PartDefinition("track-damper", "Suspension", "Track Damper", price: 800,
                reputationRequired: 20, topSpeedDelta: 0, accelerationDelta: 0),
        };

        public static BuildRecipeDefinition Hero1965TrackBuild() =>
            new("hero-1965-track-build", "hero-1965", "track", "Hero 1965 Track Build",
                targetWeightToPowerMinKgPerHp: 3.0, targetWeightToPowerMaxKgPerHp: 3.6,
                unlockedTitle: "Track Regular")
            {
                RequiredDifferentialType = "lsdRace",
                UnlockedLiveryId = "hero-1965-track-livery",
            };
    }
}
