using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WTRL.Racing;
using WTRL.UI;

namespace WTRL.EditorTools
{
    /// <summary>
    /// Builds one real scene per track that already has generated FBX
    /// geometry + textures (racinggame/BlenderPipeline/scripts/
    /// generate_world_tracks.py's output, copied into
    /// Art/Tracks/*.fbx + Art/Tracks/Textures/*.png) but was, until now,
    /// only ever loaded into the single Foundry-Row-specific
    /// VerticalSlice scene. Closes "only Foundry Row is wired into
    /// Unity" from the world-content gap list -- the other 7 tracks'
    /// mesh/texture assets already existed, they were just never
    /// instantiated into a scene anyone (or any test) could load.
    ///
    /// This is intentionally NOT a full parity rebuild of VerticalSlice
    /// per track (no garage/dyno/HUD/audio rig, no hero-vehicle career
    /// wiring) -- it is the minimum real scene for each track: the
    /// actual textured road+barrier mesh, real waypoint markers from
    /// `Racing.SampleContent`, a single AI vehicle driving the line via
    /// `AiVehicleController`, camera, and lighting. Ground is a flat
    /// plane sized to each track's own real bounding box (not a shared
    /// terrain system) -- a genuinely per-track ground area, not a
    /// claim of a general terrain solution.
    /// </summary>
    public static class WorldTrackSceneBuilder
    {
        private const string ScenesFolder = "Assets/WrenchToRaceLegends/Scenes/Tracks";
        private const string TracksArtFolder = "Assets/WrenchToRaceLegends/Art/Tracks";
        private const string MarshModelPath = "Assets/WrenchToRaceLegends/Art/Vehicles/MarshNsx.fbx";
        private static readonly Quaternion ModelAxisCorrection = Quaternion.Euler(-90f, 0f, 0f);

        private sealed class TrackEntry
        {
            public string TrackId;
            public Func<TrackLineDefinition> Line;
            public string DisplayName;
        }

        // redline-raceway is deliberately excluded: it's a drag strip, and
        // Racing.SampleContent has never authored an AI racing LINE for
        // it (only World.SampleContent has its facility data) -- a
        // straight point-to-point line isn't the same kind of content as
        // the lap-based TrackLineDefinition every other track here uses,
        // and inventing one wasn't in scope for this pass. Its FBX/
        // texture assets exist and are unused; that is an honest,
        // documented gap, not fixed here.
        private static readonly TrackEntry[] Tracks =
        {
            new() { TrackId = "cutback-tri-oval", Line = SampleContent.CutbackTriOvalLine, DisplayName = "CutbackTriOval" },
            new() { TrackId = "longbow-speedway", Line = SampleContent.LongbowSpeedwayLine, DisplayName = "LongbowSpeedway" },
            new() { TrackId = "highbank-superspeedway", Line = SampleContent.HighbankSuperspeedwayLine, DisplayName = "HighbankSuperspeedway" },
            new() { TrackId = "whisperwood-forest-circuit", Line = SampleContent.WhisperwoodForestCircuitLine, DisplayName = "WhisperwoodForestCircuit" },
            new() { TrackId = "cliffside-coastal-circuit", Line = SampleContent.CliffsideCoastalCircuitLine, DisplayName = "CliffsideCoastalCircuit" },
            new() { TrackId = "ironclad-technical-circuit", Line = SampleContent.IroncladTechnicalCircuitLine, DisplayName = "IroncladTechnicalCircuit" },
        };

        [MenuItem("Assets/WTRL/Build All Track Scenes")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/WrenchToRaceLegends/Scenes"))
                {
                    AssetDatabase.CreateFolder("Assets/WrenchToRaceLegends", "Scenes");
                }
                AssetDatabase.CreateFolder("Assets/WrenchToRaceLegends/Scenes", "Tracks");
            }

            foreach (var entry in Tracks)
            {
                BuildOne(entry);
            }

            AssetDatabase.SaveAssets();
        }

        private static void BuildOne(TrackEntry entry)
        {
            var fbxPath = $"{TracksArtFolder}/{entry.TrackId}.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogWarning($"WorldTrackSceneBuilder: no track mesh at {fbxPath} -- skipping {entry.DisplayName}.");
                return;
            }

            var line = entry.Line();
            var bounds = ComputeNodeBounds(line);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildFlatGround(bounds);
            var trackInstance = BuildTrackMesh(entry.TrackId, fbxPath);
            BuildCircuitMarkers(line);
            BuildPostProcessing(entry.TrackId);

            var startNode = line.Nodes[0];
            var startPos = new Vector3((float)startNode.X, 0.35f, (float)startNode.Z);

            var marshAsset = AssetDatabase.LoadAssetAtPath<WTRL.Content.VehicleDefinitionAsset>(
                "Assets/WrenchToRaceLegends/Content/Generated/VehicleDefinition_MarshGen1.asset");
            GameObject vehicleGo = null;
            if (marshAsset != null)
            {
                vehicleGo = new GameObject("MarshVehicle");
                var aiController = vehicleGo.AddComponent<AiVehicleController>();
                aiController.vehicle = marshAsset;
                vehicleGo.transform.position = startPos;
                AttachVehicleModel(vehicleGo, MarshModelPath, new Color(0.85f, 0.85f, 0.9f, 1f));
            }
            else
            {
                Debug.LogWarning("WorldTrackSceneBuilder: Marsh vehicle asset missing -- track scene will have no vehicle.");
            }

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            camera.allowHDR = true;
            if (vehicleGo != null)
            {
                var followCam = cameraGo.AddComponent<SimpleFollowCamera>();
                SetPrivateField(followCam, "target", vehicleGo.transform);
                cameraGo.transform.position = startPos + new Vector3(0, 4, -8);
            }
            else
            {
                var center = bounds.center;
                cameraGo.transform.position = new Vector3(center.x, bounds.size.magnitude * 0.6f + 20f, center.z - 10f);
                cameraGo.transform.LookAt(new Vector3(center.x, 0, center.z));
            }

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(45, -35, 0);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.18f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.7f, 0.78f, 0.85f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 500f;

            var scenePath = $"{ScenesFolder}/{entry.DisplayName}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"WorldTrackSceneBuilder: saved {scenePath} (track bounds {bounds.size}).");
        }

        private static Bounds ComputeNodeBounds(TrackLineDefinition line)
        {
            var min = new Vector3(float.MaxValue, 0, float.MaxValue);
            var max = new Vector3(float.MinValue, 0, float.MinValue);
            foreach (var node in line.Nodes)
            {
                min.x = Mathf.Min(min.x, (float)node.X);
                min.z = Mathf.Min(min.z, (float)node.Z);
                max.x = Mathf.Max(max.x, (float)node.X);
                max.z = Mathf.Max(max.z, (float)node.Z);
            }
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            bounds.Expand(60f); // margin around the real waypoint extent
            return bounds;
        }

        private static void BuildFlatGround(Bounds bounds)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(bounds.center.x, 0, bounds.center.z);
            // Unity's plane primitive is 10x10 units at scale 1.
            var scaleX = Mathf.Max(bounds.size.x / 10f, 4f);
            var scaleZ = Mathf.Max(bounds.size.z / 10f, 4f);
            ground.transform.localScale = new Vector3(scaleX, 1, scaleZ);

            var groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/WrenchToRaceLegends/Art/Environment/Textures/world_ground_grass.png");
            var grass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (groundTex != null)
            {
                grass.mainTexture = groundTex;
                grass.mainTextureScale = new Vector2(scaleX * 2f, scaleZ * 2f);
            }
            else
            {
                grass.color = new Color(0.16f, 0.16f, 0.17f);
            }
            grass.SetFloat("_Smoothness", 0.1f);
            ground.GetComponent<Renderer>().sharedMaterial = grass;
        }

        private static GameObject BuildTrackMesh(string trackId, string fbxPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"{trackId}_track";
            instance.transform.position = new Vector3(0, 0.02f, 0);

            var asphaltTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{TracksArtFolder}/Textures/{trackId}_asphalt.png");
            var barrierTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"{TracksArtFolder}/Textures/{trackId}_barrier_stripe.png");

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var isBarrier = renderer.gameObject.name.Contains("barrier");
                var tex = isBarrier ? barrierTex : asphaltTex;
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (tex != null) mat.mainTexture = tex;
                mat.SetFloat("_Smoothness", isBarrier ? 0.5f : 0.3f);
                renderer.sharedMaterial = mat;
            }

            return instance;
        }

        private static void BuildCircuitMarkers(TrackLineDefinition line)
        {
            var parent = new GameObject($"{line.TrackId}_Markers");
            var markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(1f, 0.35f, 0f),
            };
            markerMaterial.EnableKeyword("_EMISSION");
            markerMaterial.SetColor("_EmissionColor", new Color(1f, 0.35f, 0f) * 1.5f);

            for (var i = 0; i < line.Nodes.Count; i++)
            {
                var node = line.Nodes[i];
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = $"Node_{i}";
                marker.transform.SetParent(parent.transform);
                marker.transform.position = new Vector3((float)node.X, 0.4f, (float)node.Z);
                marker.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);
                marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            }
        }

        private static void BuildPostProcessing(string trackId)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.9f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.25f;

            var colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.overrideState = true; colorAdjustments.postExposure.value = 0.1f;
            colorAdjustments.contrast.overrideState = true; colorAdjustments.contrast.value = 8f;
            colorAdjustments.saturation.overrideState = true; colorAdjustments.saturation.value = 6f;

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true; vignette.intensity.value = 0.2f;
            vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.4f;

            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets/WrenchToRaceLegends/Scenes", "Tracks");
            }
            var assetPath = $"{ScenesFolder}/{trackId}_PostProcessing.asset";
            AssetDatabase.CreateAsset(profile, assetPath);

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = profile;
        }

        private static void AttachVehicleModel(GameObject parent, string modelPath, Color paintColor)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = ModelAxisCorrection;

            var paint = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = paintColor };
            paint.SetFloat("_Metallic", 0.6f);
            paint.SetFloat("_Smoothness", 0.7f);

            var tire = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.05f, 0.05f, 0.05f, 1f) };
            var rim = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.75f, 0.75f, 0.78f, 1f) };
            rim.SetFloat("_Metallic", 0.9f); rim.SetFloat("_Smoothness", 0.85f);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var partName = renderer.gameObject.name.ToUpperInvariant();
                Material chosen = partName.Contains("TIRE") ? tire : partName.Contains("RIM") ? rim : paint;
                var materials = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < materials.Length; i++) materials[i] = chosen;
                renderer.sharedMaterials = materials;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
