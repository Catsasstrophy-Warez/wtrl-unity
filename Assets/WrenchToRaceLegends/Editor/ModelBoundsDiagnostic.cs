using UnityEditor;
using UnityEngine;

namespace WTRL.EditorTools
{
    /// <summary>Logs the combined mesh bounds of every imported FBX
    /// asset this project ships, so an obviously-wrong import scale or
    /// axis mismatch (e.g. a car importing 1000 units tall, or a track's
    /// length landing on the wrong axis) can be caught without a human
    /// looking at it. Real, reproduced bugs of exactly this kind were
    /// caught this way for both the vehicle models and the track meshes
    /// -- see UI/CONTRACT.md's "Real vehicle geometry + visual pass" and
    /// Career/CONTRACT.md's world-content notes for the full accounts.
    /// Uses `Renderer.bounds` on a freshly-instantiated, untouched
    /// (zero-rotation) copy of each prefab -- reliable for that case, but
    /// NOTE: `Renderer.bounds`/`Transform.localToWorldMatrix` were both
    /// found to not reliably reflect a rotation applied to an object
    /// AFTER instantiation, in this environment's `-nographics`
    /// headless batchmode specifically. Don't reuse this pattern to
    /// verify a post-instantiation rotation fix -- compute world
    /// positions from `Matrix4x4.TRS(transform.position, transform
    /// .rotation, transform.lossyScale)` built fresh instead, or (more
    /// reliably still) bake any needed correction into the exported mesh
    /// data itself, as `generate_world_tracks.py` does.</summary>
    public static class ModelBoundsDiagnostic
    {
        [MenuItem("Assets/WTRL/Diagnose Imported Model Bounds")]
        public static void Run()
        {
            foreach (var path in new[]
            {
                "Assets/WrenchToRaceLegends/Art/Vehicles/HeroCrownfire.fbx",
                "Assets/WrenchToRaceLegends/Art/Vehicles/MarshNsx.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/foundry-row-circuit.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/redline-raceway.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/cutback-tri-oval.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/longbow-speedway.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/highbank-superspeedway.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/whisperwood-forest-circuit.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/cliffside-coastal-circuit.fbx",
                "Assets/WrenchToRaceLegends/Art/Tracks/ironclad-technical-circuit.fbx",
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
