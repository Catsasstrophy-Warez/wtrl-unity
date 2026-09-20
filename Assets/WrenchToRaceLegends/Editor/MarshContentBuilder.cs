using System.IO;
using UnityEditor;
using UnityEngine;
using WTRL.Content;
using WTRL.Vehicle;

namespace WTRL.EditorTools
{
    /// <summary>
    /// Second content builder, same pattern as <see cref="HeroContentBuilder"/>:
    /// creates real `.asset` instances for Marsh's first vehicle
    /// generation (`marsh_gen1_1991`), the rival roster's first Blender
    /// blockout target (see `racinggame/BlenderPipeline/export/
    /// rival_blockouts_pass1/`) and the first non-hero vehicle to exist
    /// as real WTRL.Content data.
    ///
    /// VALUES NOTE: approximate NSX-class numbers for a ~1991-era
    /// naturally-aspirated V6, chosen to be plausible for the "Marsh
    /// Constant" archetype (see `MASTER-VEHICLE-REGISTRY.csv`'s VEH-013
    /// row and `marsh_nsx91.blend`'s pre-existing filename) -- NOT
    /// sourced/cited research-corpus data. Mass/wheelbase mirror the
    /// `marsh_gen1_1991` profile already committed to `catalog_manifest
    /// .json` during the rival blockout pass, so at least those two
    /// numbers are consistent with the Blender asset's proportions.
    /// </summary>
    public static class MarshContentBuilder
    {
        private const string OutputDir = "Assets/WrenchToRaceLegends/Content/Generated";

        [MenuItem("Assets/WTRL/Build Marsh-Gen1 Content Assets")]
        public static void BuildMarshGen1()
        {
            AssetDatabase.Refresh();
            EnsureFolder();

            var engine = ScriptableObject.CreateInstance<EngineDefinitionAsset>();
            engine.id = "marsh-gen1-engine";
            engine.displayName = "Marsh Gen1 V6";
            engine.displacementLiters = 3.0;
            engine.peakPowerHp = 270;
            engine.peakTorqueLbFt = 210;
            engine.redlineRpm = 8000;
            engine.idleRpm = 900;
            Create(engine, "EngineDefinition_MarshGen1.asset");

            var transmission = ScriptableObject.CreateInstance<TransmissionDefinitionAsset>();
            transmission.id = "marsh-gen1-gearbox";
            transmission.displayName = "Marsh Gen1 5-Speed";
            transmission.ratios = new[] { 3.23, 2.05, 1.48, 1.15, 0.91 };
            transmission.finalDrive = 4.06;
            transmission.kind = TransmissionKind.Manual;
            Create(transmission, "TransmissionDefinition_MarshGen1.asset");

            var suspension = ScriptableObject.CreateInstance<SuspensionDefinitionAsset>();
            suspension.id = "marsh-gen1-suspension";
            suspension.displayName = "Marsh Gen1 Suspension";
            suspension.frontLayout = "double-wishbone";
            suspension.rearLayout = "double-wishbone";
            Create(suspension, "SuspensionDefinition_MarshGen1.asset");

            var tire = ScriptableObject.CreateInstance<TireDefinitionAsset>();
            tire.id = "marsh-gen1-tire";
            tire.displayName = "Marsh Gen1 Sport Tire";
            tire.longitudinalStiffness = 9.0;
            tire.corneringStiffness = 6.2;
            tire.peakSlipRatio = 0.11;
            tire.peakSlipAngleRadians = 0.10;
            Create(tire, "TireDefinition_MarshGen1.asset");

            var vehicle = ScriptableObject.CreateInstance<VehicleDefinitionAsset>();
            vehicle.id = "marsh-gen1";
            vehicle.generation = "marsh_gen1_1991";
            vehicle.displayName = "Marsh Constant Gen 1 (1991)";
            // Mirrors catalog_manifest.json's marsh_gen1_1991 profile
            // (length 4.43 / width 1.81 / wheelbase 2.53) -- mass is not
            // tracked by that Blender-only manifest, so it's an
            // independent NSX-class estimate.
            vehicle.massKg = 1365;
            vehicle.wheelbaseM = 2.53;
            vehicle.engine = engine;
            vehicle.transmission = transmission;
            vehicle.suspension = suspension;
            vehicle.tire = tire;
            vehicle.driveLayout = DriveLayout.Rwd;
            vehicle.differential = DifferentialKind.Open;
            Create(vehicle, "VehicleDefinition_MarshGen1.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MarshContentBuilder: created marsh-gen1 content assets in " + OutputDir);
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
