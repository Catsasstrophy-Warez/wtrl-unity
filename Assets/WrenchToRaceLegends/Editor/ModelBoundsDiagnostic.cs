using UnityEditor;
using UnityEngine;

namespace WTRL.EditorTools
{
    /// <summary>Diagnostic-only, temporary: logs the combined mesh bounds
    /// of an imported FBX so an obviously-wrong import scale (e.g. a car
    /// that imports as 1000 units tall) can be caught without a human
    /// looking at it. Not part of the game -- safe to delete once the
    /// hero/Marsh FBX imports are confirmed sane.</summary>
    public static class ModelBoundsDiagnostic
    {
        [MenuItem("Assets/WTRL/Diagnose Marsh Asset Load")]
        public static void DiagnoseMarshLoad()
        {
            AssetDatabase.Refresh();
            const string path = "Assets/WrenchToRaceLegends/Content/Generated/VehicleDefinition_MarshGen1.asset";
            var asObject = AssetDatabase.LoadAssetAtPath<Object>(path);
            var asVehicle = AssetDatabase.LoadAssetAtPath<WTRL.Content.VehicleDefinitionAsset>(path);
            var guid = AssetDatabase.AssetPathToGUID(path);
            Debug.Log($"guid='{guid}' asObject={(asObject == null ? "null" : asObject.GetType().FullName)} " +
                $"asVehicle={(asVehicle == null ? "null" : "ok")}");
        }

        [MenuItem("Assets/WTRL/Diagnose Imported Model Bounds")]
        public static void Run()
        {
            foreach (var path in new[]
            {
                "Assets/WrenchToRaceLegends/Art/Vehicles/HeroCrownfire.fbx",
                "Assets/WrenchToRaceLegends/Art/Vehicles/MarshNsx.fbx",
            })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"ModelBoundsDiagnostic: could not load {path}");
                    continue;
                }

                var instance = Object.Instantiate(prefab);
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0)
                {
                    Debug.LogError($"ModelBoundsDiagnostic: {path} has no MeshRenderers");
                    Object.DestroyImmediate(instance);
                    continue;
                }

                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                Debug.Log($"ModelBoundsDiagnostic: {path} -> {renderers.Length} renderers, " +
                    $"size={bounds.size}, center={bounds.center}");
                Object.DestroyImmediate(instance);
            }
        }
    }
}
