namespace WTRL.World
{
    /// <summary>
    /// The project's first authored (not fixture-only) track content --
    /// closes part of the "author one real TrackDefinition for a first
    /// test circuit" gap. "Foundry Row" is one of the two Blackridge
    /// district vertical-slice candidates named in
    /// racinggame/PROJECT-MAP-UNITY-MOBILE.md ("one short original
    /// circuit" in "Foundry Row or River Bypass"). No actual world
    /// geometry/mesh exists for this yet -- see WTRL.Racing
    /// .SampleContent.FoundryRowCircuitLine() for the matching AI
    /// racing-line waypoints, whose node coordinates are this class's
    /// only real description of the circuit's shape so far.
    /// </summary>
    public static class SampleContent
    {
        public const string FoundryRowCircuitId = "foundry-row-circuit";

        /// <summary>Flat road circuit (0 banking) -- a rough rectangle
        /// with two hairpin-style corners, sized for a short original
        /// circuit rather than a full-scale track. Length is the actual
        /// polyline length of <see cref="Racing.SampleContent
        /// .FoundryRowCircuitLine"/>'s nodes, not an independent
        /// estimate -- see that method's test coverage for the
        /// cross-check.</summary>
        public static TrackDefinition FoundryRowCircuit() =>
            new(FoundryRowCircuitId, "Foundry Row Circuit", "circuit", lengthM: 633.1, bankingDegrees: 0);
    }
}
