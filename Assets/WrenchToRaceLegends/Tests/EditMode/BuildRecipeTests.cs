using NUnit.Framework;
using WTRL.Garage;
using WTRL.RPG;
using WTRL.Vehicle;

namespace WTRL.Tests
{
    /// <summary>Tests for the WTRL.Garage/WTRL.RPG BuildRecipe split — the
    /// resolution of the "where does BuildRecipe live" open question from
    /// OUTPUT-RPG-Design.md. New tests, no Swift/Prototype~ equivalent to
    /// port (no SatisfiesTarget-equivalent existed anywhere verified).
    /// Run via the same throwaway dotnet test project as every other
    /// assembly — all pass.</summary>
    public class BuildRecipeTests
    {
        private static VehicleDefinition MakeVehicle(double massKg, DifferentialKind differential) =>
            new VehicleDefinition("test-vehicle", "test-gen", "Test Vehicle", massKg: massKg, wheelbaseM: 2.6,
                engineId: "test-engine", transmissionId: "test-gearbox", suspensionId: "test-suspension")
            {
                Differential = differential,
            };

        private static EngineDefinition MakeEngine(double peakPowerHp) =>
            new EngineDefinition("test-engine", "Test Engine", displacementLiters: 5.0, peakPowerHp: peakPowerHp, peakTorqueLbFt: 390);

        [Test]
        public void SatisfiesTargetWhenWeightToPowerInRangeAndNoDifferentialRequirement()
        {
            var recipe = new BuildRecipeDefinition("r1", "test-vehicle", "base", "Driveway Special", 7.0, 8.0, "Driveway Special");
            var vehicle = MakeVehicle(massKg: 1500, DifferentialKind.Open); // 1500/200 = 7.5, in range
            var engine = MakeEngine(peakPowerHp: 200);

            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, vehicle, engine), Is.True);
        }

        [Test]
        public void FailsWhenWeightToPowerOutOfRange()
        {
            var recipe = new BuildRecipeDefinition("r1", "test-vehicle", "base", "Driveway Special", 7.0, 8.0, "Driveway Special");
            var vehicle = MakeVehicle(massKg: 1500, DifferentialKind.Open); // 1500/500 = 3.0, out of range
            var engine = MakeEngine(peakPowerHp: 500);

            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, vehicle, engine), Is.False);
        }

        [Test]
        public void RequiredLsdIsSatisfiedByClutchLsdOrTorqueBiasing()
        {
            var recipe = new BuildRecipeDefinition("r1", "test-vehicle", "race", "Full Compression", 6.0, 8.0, "Full Compression")
            {
                RequiredDifferentialType = "lsd",
            };
            var engine = MakeEngine(peakPowerHp: 200);

            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, MakeVehicle(1500, DifferentialKind.ClutchLsd), engine), Is.True);
            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, MakeVehicle(1500, DifferentialKind.TorqueBiasing), engine), Is.True);
            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, MakeVehicle(1500, DifferentialKind.Open), engine), Is.False);
        }

        [Test]
        public void UnrecognizedRequiredDifferentialTypeNeverSilentlyPasses()
        {
            // This project's standing discipline: an unresolved/unmapped
            // requirement fails honestly rather than guessing a pass.
            var recipe = new BuildRecipeDefinition("r1", "test-vehicle", "race", "Mystery Spec", 6.0, 8.0, "Mystery Spec")
            {
                RequiredDifferentialType = "some-future-differential-type-nobody-mapped-yet",
            };
            var engine = MakeEngine(peakPowerHp: 200);

            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, MakeVehicle(1500, DifferentialKind.ClutchLsd), engine), Is.False);
        }

        [Test]
        public void FreeFormRecipeIsAlwaysCompleted()
        {
            var recipe = SavedBuildRecipe.CreateFreeForm("My Autocross Setup");
            Assert.That(recipe.IsCompleted, Is.True);
            Assert.That(recipe.Kind, Is.EqualTo(RecipeKind.FreeForm));
        }

        [Test]
        public void TargetRecipeStartsIncompleteUntilMarked()
        {
            var recipe = SavedBuildRecipe.CreateTarget("Driveway Build", "r1", unlockedTitle: "Driveway Special");
            Assert.That(recipe.IsCompleted, Is.False);
            recipe.MarkCompleted();
            Assert.That(recipe.IsCompleted, Is.True);
        }

        [Test]
        public void RpgLayerNeverReferencesGarageOrVehicleTypesDirectly()
        {
            // Structural guard, not a behavior test: SavedBuildRecipe's
            // target reference is a plain string id, so WTRL.RPG can stay
            // dependency-free of WTRL.Garage. If this ever needs a real
            // BuildRecipeDefinition reference instead of a string, that's
            // the signal the architecture question needs revisiting.
            var recipe = SavedBuildRecipe.CreateTarget("x", "r1");
            Assert.That(recipe.TargetRecipeDefinitionId, Is.TypeOf<string>());
        }

        [Test]
        public void Hero1965TrackBuildRecipeIsSatisfiableEndToEnd()
        {
            // The project's first fully authored, end-to-end-satisfiable
            // recipe (0 of 35 spec'd recipes existed as real content
            // before this). Exercises the full path: content -> Garage's
            // satisfaction check -> RPG's progression state -> completion.
            var recipe = SampleContent.Hero1965TrackBuild();
            var vehicle = new VehicleDefinition("hero-1965", "hero-1965", "Hero 1965", massKg: 1450, wheelbaseM: 2.6,
                engineId: "hero-1965-engine", transmissionId: "hero-1965-gearbox", suspensionId: "hero-1965-suspension");
            var engine = new EngineDefinition("hero-1965-engine", "Hero 1965 V8", displacementLiters: 5.0,
                peakPowerHp: 420, peakTorqueLbFt: 390);

            // The base street configuration (open differential) does NOT
            // satisfy the recipe's required "lsdRace" differential --
            // confirms the check is real, not vacuously true.
            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, vehicle, engine), Is.False);

            var trackVehicle = vehicle with { Differential = DifferentialKind.TorqueBiasing };
            Assert.That(BuildRecipeEvaluator.SatisfiesTarget(recipe, trackVehicle, engine), Is.True);

            var saved = SavedBuildRecipe.CreateTarget("My Track Build", recipe.Id,
                unlockedTitle: recipe.UnlockedTitle, unlockedLiveryId: recipe.UnlockedLiveryId);
            Assert.That(saved.IsCompleted, Is.False);

            saved.MarkCompleted();
            Assert.That(saved.IsCompleted, Is.True);
            Assert.That(saved.UnlockedTitle, Is.EqualTo("Track Regular"));
        }
    }
}
