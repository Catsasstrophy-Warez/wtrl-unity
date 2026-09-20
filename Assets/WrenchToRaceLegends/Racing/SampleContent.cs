namespace WTRL.Racing
{
    /// <summary>
    /// The project's first authored AI racing-line content, paired with
    /// WTRL.World.SampleContent.FoundryRowCircuit(). WTRL.Racing has no
    /// dependency on WTRL.World (and shouldn't gain one just for this --
    /// confirmed the hard way: an earlier version of this file referenced
    /// `World.SampleContent.FoundryRowCircuitId` directly and failed to
    /// compile in the real Unity Editor with CS0103, since Racing's
    /// asmdef has no World reference; the throwaway dotnet-test method
    /// missed this because it flattens every assembly into one flat
    /// folder, hiding real cross-assembly reference boundaries). Fixed by
    /// duplicating the id as a literal string instead -- the same
    /// explicit-reference-by-id discipline `WTRL.RPG.BuildRecipeProgress`
    /// already uses to avoid a `WTRL.Garage` dependency.
    ///
    /// Node coordinates form a rough rectangle with two corners -- a
    /// deliberately simple placeholder shape sized for a short original
    /// circuit, not derived from any real-world track survey. Target
    /// speeds are round numbers chosen to be plausible for a
    /// straight/corner split, not tuned against real telemetry.
    /// </summary>
    public static class SampleContent
    {
        /// <summary>Must match WTRL.World.SampleContent.FoundryRowCircuitId
        /// exactly -- kept as a duplicated literal, not a shared
        /// reference, to avoid a WTRL.Racing -&gt; WTRL.World dependency.
        /// SampleContentTests.TrackDefinitionAndTrackLineShareTheSameId
        /// guards against these drifting apart.</summary>
        public const string FoundryRowCircuitTrackId = "foundry-row-circuit";

        public static TrackLineDefinition FoundryRowCircuitLine() => new(
            "foundry-row-circuit-line", FoundryRowCircuitTrackId, new[]
            {
                new TrackNode(0, 0, targetSpeedMps: 45),
                new TrackNode(200, 0, targetSpeedMps: 45),
                new TrackNode(220, 20, targetSpeedMps: 18, brakingWeight: 0.8),
                new TrackNode(220, 80, targetSpeedMps: 22),
                new TrackNode(200, 100, targetSpeedMps: 40),
                new TrackNode(0, 100, targetSpeedMps: 40),
                new TrackNode(-20, 80, targetSpeedMps: 18, brakingWeight: 0.8),
                new TrackNode(-20, 20, targetSpeedMps: 22),
            });
    }
}
