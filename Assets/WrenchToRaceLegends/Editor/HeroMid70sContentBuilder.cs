using System.IO;
using UnityEditor;
using UnityEngine;
using WTRL.Content;
using WTRL.Vehicle;

namespace WTRL.EditorTools
{
    /// <summary>
    /// The project's third real vehicle content builder (after
    /// `HeroContentBuilder`/`MarshContentBuilder`), for `hero-mid70s` --
    /// closes one entry of "6 of 7 hero generations referenced by
    /// `Garage.CanonicalBuildRecipes` have no real `VehicleDefinitionAsset`"
    /// from the content-completeness audit. All 5 `hero75-*` recipes in
    /// `CanonicalBuildRecipes.cs` can now actually be evaluated against a
    /// real vehicle, not just exist as unattached data.
    ///
    /// REAL, CITED DATA (unlike `HeroContentBuilder`'s hero-1965 numbers,
    /// which are explicitly placeholder fixture values): mass (1580kg)
    /// and peak power (140hp, the top of the real cited 139-140hp
    /// range) come directly from `CanonicalBuildRecipes.cs`'s own
    /// comment on the mid-70s generation ("Mid-70s (mass 1580kg) --
    /// compressed ladder: base and performance share the same real
    /// 139-140hp figure"), itself sourced from the research corpus per
    /// that file's own citation discipline. This builder does not
    /// re-derive or re-cite the original source doc directly -- it
    /// reuses the number already verified real elsewhere in this
    /// codebase, rather than risking a second, possibly inconsistent
    /// citation of the same fact.
    ///
    /// EVERYTHING ELSE is a placeholder, same explicit-caveat pattern as
    /// `HeroContentBuilder`: wheelbase, transmission ratios, suspension
    /// layout, and tire characteristics have no cited source and are
    /// chosen only to be a plausible mid-1970s American V8 baseline
    /// (a period 3-speed automatic-era ratio spread, a period double-
    /// wishbone/live-axle layout, bias-ply-adjacent street tire
    /// stiffness slightly lower than hero-1965's radial figures) --
    /// not sourced, not balance-tested.
    /// </summary>
    public static class HeroMid70sContentBuilder
    {
        private const string OutputDir = "Assets/WrenchToRaceLegends/Content/Generated";

        [MenuItem("Assets/WTRL/Build Hero-Mid70s Content Assets")]
        public static void BuildHeroMid70s()
        {
            AssetDatabase.Refresh();
            EnsureFolder();

            var engine = ScriptableObject.CreateInstance<EngineDefinitionAsset>();
            engine.id = "hero-mid70s-engine";
            engine.displayName = "Hero Mid-70s V8";
            engine.displacementLiters = 5.8; // period big-block-adjacent displacement, not cited
            engine.peakPowerHp = 140; // real, cited (see class doc comment)
            engine.peakTorqueLbFt = 280; // not cited -- plausible for a low-compression mid-70s big-block
            engine.redlineRpm = 5200; // lower than hero-1965's -- reflects the real horsepower/emissions-era detuning this generation represents
            engine.idleRpm = 700;
            Create(engine, "EngineDefinition_HeroMid70s.asset");

            var transmission = ScriptableObject.CreateInstance<TransmissionDefinitionAsset>();
            transmission.id = "hero-mid70s-gearbox";
            transmission.displayName = "Hero Mid-70s 3-Speed Automatic";
            transmission.ratios = new[] { 2.46, 1.46, 1.00 };
            transmission.finalDrive = 2.75; // period tall-geared economy/emissions-era final drive, not cited
            transmission.kind = TransmissionKind.Automatic;
            Create(transmission, "TransmissionDefinition_HeroMid70s.asset");

            var suspension = ScriptableObject.CreateInstance<SuspensionDefinitionAsset>();
            suspension.id = "hero-mid70s-suspension";
            suspension.displayName = "Hero Mid-70s Suspension";
            suspension.frontLayout = "double-wishbone";
            suspension.rearLayout = "live-axle"; // period-correct layout choice, not cited
            Create(suspension, "SuspensionDefinition_HeroMid70s.asset");

            var tire = ScriptableObject.CreateInstance<TireDefinitionAsset>();
            tire.id = "hero-mid70s-tire";
            tire.displayName = "Hero Mid-70s Bias-Belted";
            tire.longitudinalStiffness = 6.5; // lower than hero-1965's radial figures -- period bias-belted tires had less grip, not cited
            tire.corneringStiffness = 4.2;
            tire.peakSlipRatio = 0.15;
            tire.peakSlipAngleRadians = 0.14;
            Create(tire, "TireDefinition_HeroMid70s.asset");

            // Reuses the same shared "asphalt" surface asset every
            // vehicle content builder creates -- Create() deletes and
            // recreates it identically each time, so this is a no-op if
            // HeroContentBuilder/MarshContentBuilder already ran, not a
            // second competing surface definition.
            var surface = ScriptableObject.CreateInstance<SurfaceDefinitionAsset>();
            surface.id = "asphalt";
            surface.kind = SurfaceKind.Asphalt;
            surface.dryGripMultiplier = 1.0;
            Create(surface, "SurfaceDefinition_Asphalt.asset");

            var vehicle = ScriptableObject.CreateInstance<VehicleDefinitionAsset>();
            vehicle.id = "hero-mid70s";
            vehicle.generation = "hero-mid70s";
            vehicle.displayName = "Hero Mid-70s";
            vehicle.massKg = 1580; // real, cited (see class doc comment)
            vehicle.wheelbaseM = 2.95; // plausible mid-70s full-size wheelbase, not cited
            vehicle.engine = engine;
            vehicle.transmission = transmission;
            vehicle.suspension = suspension;
            vehicle.tire = tire;
            Create(vehicle, "VehicleDefinition_HeroMid70s.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HeroMid70sContentBuilder: created hero-mid70s content assets in " + OutputDir);
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
