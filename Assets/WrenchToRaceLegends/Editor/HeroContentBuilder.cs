using System.IO;
using UnityEditor;
using UnityEngine;
using WTRL.Content;
using WTRL.Vehicle;

namespace WTRL.EditorTools
{
    /// <summary>
    /// The project's first real `WTRL.Editor` content: a builder that
    /// creates `WTRL.Content` ScriptableObject asset instances for the
    /// hero-1965 vehicle programmatically, via `AssetDatabase.CreateAsset`,
    /// instead of hand-authoring `.asset` YAML (which prior CONTRACT.md
    /// entries deliberately avoided as too easy to get subtly wrong) or
    /// requiring a human to click through the Create menu by hand.
    ///
    /// Run via `Assets > WTRL > Build Hero-1965 Content Assets` in the
    /// Editor, or in batchmode with:
    ///   Unity.exe -batchmode -quit -projectPath &lt;path&gt;
    ///     -executeMethod WTRL.EditorTools.HeroContentBuilder.BuildHero1965
    ///
    /// VALUES NOTE: these numbers mirror the fixture values already used
    /// throughout `Tests/EditMode` (see e.g. `WTRLRuntimeTests.MakeVehicle
    /// /MakeEngine/...`) for consistency with what the test suite already
    /// treats as a plausible mid-60s muscle-car baseline. They are NOT
    /// sourced/cited research-corpus data the way the project's other
    /// content is graded (see `10-vehicle-research-library`).
    ///
    /// NAMING DECISION (resolved 2026-09-20, previously an open
    /// question): the `hero-1965` id is kept permanently, NOT renamed to
    /// the research corpus's `hero_1967`. Checked what replacing it
    /// would actually require: `ImportedVehicleCorpus/Content/HeroCars/
    /// hero_generation_catalog.json`'s own `hero_1967` entry lists
    /// `"researchGates": ["OEM torque values", "period routing"]` --
    /// the corpus itself has NOT resolved real numeric specs for this
    /// generation yet, so there is no better sourced data to switch to
    /// today. Renaming the id without better data would just move the
    /// placeholder-fixture problem to a differently-spelled id, while
    /// breaking `hero-1965` as `CareerState.SelectedVehicleId`'s
    /// default, every persistence test, and all 5 `hero1965-*` entries
    /// in `Garage.CanonicalBuildRecipes` -- real, currently-passing
    /// tests, for zero real gain. If/when hero_1967's OEM specs are
    /// actually researched, that is the point to revisit this id
    /// (and re-derive `CanonicalBuildRecipes`' hero1965-* weight-to-
    /// power bands from the real horsepower figures at the same time,
    /// same as every other generation's recipes already are).
    /// </summary>
    public static class HeroContentBuilder
    {
        private const string OutputDir = "Assets/WrenchToRaceLegends/Content/Generated";

        [MenuItem("Assets/WTRL/Build Hero-1965 Content Assets")]
        public static void BuildHero1965()
        {
            AssetDatabase.Refresh();
            EnsureFolder();

            var engine = ScriptableObject.CreateInstance<EngineDefinitionAsset>();
            engine.id = "hero-1965-engine";
            engine.displayName = "Hero 1965 V8";
            engine.displacementLiters = 5.0;
            engine.peakPowerHp = 420;
            engine.peakTorqueLbFt = 390;
            engine.redlineRpm = 7000;
            engine.idleRpm = 800;
            Create(engine, "EngineDefinition_Hero1965.asset");

            var transmission = ScriptableObject.CreateInstance<TransmissionDefinitionAsset>();
            transmission.id = "hero-1965-gearbox";
            transmission.displayName = "Hero 1965 4-Speed";
            transmission.ratios = new[] { 3.36, 2.07, 1.43, 1.00, 0.84 };
            transmission.finalDrive = 3.55;
            transmission.kind = TransmissionKind.Manual;
            Create(transmission, "TransmissionDefinition_Hero1965.asset");

            var suspension = ScriptableObject.CreateInstance<SuspensionDefinitionAsset>();
            suspension.id = "hero-1965-suspension";
            suspension.displayName = "Hero 1965 Suspension";
            suspension.frontLayout = "double-wishbone";
            suspension.rearLayout = "multi-link";
            Create(suspension, "SuspensionDefinition_Hero1965.asset");

            var tire = ScriptableObject.CreateInstance<TireDefinitionAsset>();
            tire.id = "hero-1965-tire";
            tire.displayName = "Hero 1965 Street Performance";
            tire.longitudinalStiffness = 8.5;
            tire.corneringStiffness = 5.5;
            tire.peakSlipRatio = 0.12;
            tire.peakSlipAngleRadians = 0.11;
            Create(tire, "TireDefinition_Hero1965.asset");

            var surface = ScriptableObject.CreateInstance<SurfaceDefinitionAsset>();
            surface.id = "asphalt";
            surface.kind = SurfaceKind.Asphalt;
            surface.dryGripMultiplier = 1.0;
            Create(surface, "SurfaceDefinition_Asphalt.asset");

            var vehicle = ScriptableObject.CreateInstance<VehicleDefinitionAsset>();
            vehicle.id = "hero-1965";
            vehicle.generation = "hero-1965";
            vehicle.displayName = "Hero 1965";
            vehicle.massKg = 1450;
            vehicle.wheelbaseM = 2.6;
            vehicle.engine = engine;
            vehicle.transmission = transmission;
            vehicle.suspension = suspension;
            vehicle.tire = tire;
            Create(vehicle, "VehicleDefinition_Hero1965.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HeroContentBuilder: created hero-1965 content assets in " + OutputDir);
        }

        private static void Create(Object asset, string fileName)
        {
            var path = Path.Combine(OutputDir, fileName).Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
            {
                AssetDatabase.CreateFolder("Assets/WrenchToRaceLegends/Content", "Generated");
            }
        }
    }
}
