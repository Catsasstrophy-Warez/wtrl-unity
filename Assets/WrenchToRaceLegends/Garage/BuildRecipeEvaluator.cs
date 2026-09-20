using WTRL.Vehicle;

namespace WTRL.Garage
{
    /// <summary>
    /// New design, not a port — no `SatisfiesTarget`-equivalent exists
    /// anywhere in <c>SwiftRacer</c>/<c>WTRLCore</c>. `BuildRecipeDefinition
    /// .RequiredTransmissionId`/`RequiredCrankType` are themselves documented
    /// there as "not yet read or enforced anywhere in game logic," and
    /// `RequiredDifferentialType` had no consumer either — this file is
    /// that consumer, for the differential check only (the other two
    /// remain unenforced; see the class doc below).
    ///
    /// This is the resolution of the "where does BuildRecipe live" open
    /// question from `RPG/CONTRACT.md`/`Assignments/OUTPUT-RPG-Design.md`:
    /// option 3 from that document — the recipe SATISFACTION check lives
    /// here in `WTRL.Garage`, since it needs `VehicleDefinition`/
    /// `EngineDefinition`/`DifferentialKind`, all Garage/Vehicle-layer
    /// types. `WTRL.RPG`'s `SavedBuildRecipe` only holds the reward/
    /// progression state (name, completion flag, unlocked title/livery)
    /// and never needs to reference these types directly — it stores the
    /// target `BuildRecipeDefinition`'s id as a plain string. This keeps
    /// `WTRL.RPG`'s asmdef dependency-free (still just `WTRL.Core`)
    /// instead of adding a `WTRL.Garage` reference for one feature.
    /// </summary>
    public static class BuildRecipeEvaluator
    {
        /// <summary>All 35 real build-recipe entries in the research corpus
        /// (<c>CanonicalContent.swift</c>'s <c>buildRecipes</c>) only ever
        /// use <c>"lsd"</c> or <c>"lsdRace"</c> for
        /// <see cref="BuildRecipeDefinition.RequiredDifferentialType"/> —
        /// the content never distinguishes a race-spec LSD from a street
        /// one at the physics-enum level, so both map to the same
        /// <see cref="DifferentialKind.ClutchLsd"/>/<see cref="DifferentialKind.TorqueBiasing"/>
        /// check. An unrecognized string fails the check rather than
        /// silently passing — this project's standing discipline is to
        /// leave a requirement honestly unsatisfied rather than guess.</summary>
        private static bool DifferentialSatisfies(string requiredType, DifferentialKind actual)
        {
            return requiredType switch
            {
                "lsd" or "lsdRace" => actual == DifferentialKind.ClutchLsd || actual == DifferentialKind.TorqueBiasing,
                _ => false,
            };
        }

        /// <summary>Checks a resolved vehicle/engine/differential
        /// configuration against a target recipe's thresholds. Only
        /// checks weight-to-power ratio and required differential type —
        /// <see cref="BuildRecipeDefinition.RequiredTransmissionId"/> and
        /// <see cref="BuildRecipeDefinition.RequiredCrankType"/> are left
        /// unenforced, exactly as documented on those two properties
        /// (sourced content the original Swift project never wired up
        /// either — this port didn't invent enforcement for them).</summary>
        public static bool SatisfiesTarget(BuildRecipeDefinition recipe, VehicleDefinition vehicle, EngineDefinition engine)
        {
            if (engine.PeakPowerHp <= 0) return false;
            var weightToPower = vehicle.MassKg / engine.PeakPowerHp;
            if (weightToPower < recipe.TargetWeightToPowerMinKgPerHp || weightToPower > recipe.TargetWeightToPowerMaxKgPerHp)
            {
                return false;
            }

            if (recipe.RequiredDifferentialType != null && !DifferentialSatisfies(recipe.RequiredDifferentialType, vehicle.Differential))
            {
                return false;
            }

            return true;
        }
    }
}
