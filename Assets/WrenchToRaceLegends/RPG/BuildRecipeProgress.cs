namespace WTRL.RPG
{
    // New design from 48-RPG-SYSTEMS-SPEC.md Part 1. Resolves the "where
    // does BuildRecipe live" open question from OUTPUT-RPG-Design.md:
    // this type deliberately holds only the player-progression/reward
    // side (name, kind, completion, unlocked title/livery) and references
    // a target definition only by string id — the actual tuning-table
    // snapshot and the SatisfiesTarget threshold check live in
    // WTRL.Garage.BuildRecipeEvaluator, which needs Vehicle/Engine/
    // DifferentialKind types this assembly deliberately doesn't depend
    // on. See Garage/BuildRecipeEvaluator.cs's class doc for the other
    // half of this split.

    public enum RecipeKind { FreeForm, Target }

    /// <summary>A player's saved build recipe. <see cref="TargetRecipeDefinitionId"/>
    /// is only meaningful when <see cref="Kind"/> is <see cref="RecipeKind.Target"/>
    /// — it's the id of a <c>WTRL.Garage.BuildRecipeDefinition</c>, referenced by
    /// string rather than by type so this assembly stays dependency-free.
    /// Free-form recipes (48-RPG-SYSTEMS-SPEC.md S1.2) have no target,
    /// no reward, and exist purely so a player with more than one
    /// competitive setup can switch between them without re-tuning from
    /// scratch — <see cref="IsCompleted"/> is always true for them the
    /// moment they're saved.</summary>
    public sealed class SavedBuildRecipe
    {
        public string Name { get; }
        public RecipeKind Kind { get; }
        public string? TargetRecipeDefinitionId { get; init; }
        public string? UnlockedTitle { get; init; }
        public string? UnlockedLiveryId { get; init; }
        public bool IsCompleted { get; private set; }

        // Constructor-enforced required fields, not the C# 11 `required`
        // keyword: Unity's per-assembly `.rsp` language-version override
        // (tried first) did not take effect for this assembly in a real
        // Editor compile, for reasons not fully understood -- rather than
        // depend on an unverified compiler-plumbing workaround, this
        // matches the constructor-required-plus-init-optional pattern
        // WTRL.Vehicle's `Definitions.cs` already established project-wide.
        private SavedBuildRecipe(string name, RecipeKind kind)
        {
            Name = name;
            Kind = kind;
        }

        /// <summary>The caller (eventually WTRL.Career, which can see both
        /// WTRL.Garage's evaluator and this type) is responsible for
        /// calling WTRL.Garage.BuildRecipeEvaluator.SatisfiesTarget first
        /// and only calling this when it returns true — this type has no
        /// way to check that itself without the Vehicle/Engine types it
        /// deliberately doesn't reference.</summary>
        public void MarkCompleted()
        {
            IsCompleted = true;
        }

        public static SavedBuildRecipe CreateFreeForm(string name) =>
            new SavedBuildRecipe(name, RecipeKind.FreeForm) { IsCompleted = true };

        public static SavedBuildRecipe CreateTarget(string name, string targetRecipeDefinitionId,
            string? unlockedTitle = null, string? unlockedLiveryId = null) =>
            new SavedBuildRecipe(name, RecipeKind.Target)
            {
                TargetRecipeDefinitionId = targetRecipeDefinitionId,
                UnlockedTitle = unlockedTitle,
                UnlockedLiveryId = unlockedLiveryId,
            };
    }
}
