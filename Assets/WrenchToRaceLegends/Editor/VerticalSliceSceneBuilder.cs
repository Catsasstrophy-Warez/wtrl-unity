using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WTRL.Content;
using WTRL.Racing;
using WTRL.UI;

namespace WTRL.EditorTools
{
    /// <summary>
    /// Assembles every piece built across this whole content/UI pass
    /// (Content assets, RaceSession/EventPreflightService, TelemetryHud,
    /// GarageScreen, DynoScreen, WorldStreamingController,
    /// VehicleAudioController, touch/tilt input) into one real scene --
    /// closes "author one real hub-world layout fragment" in the
    /// narrowest honest form. This is a Foundry Row circuit blockout
    /// (primitive markers at the real `SampleContent.FoundryRowCircuitLine`
    /// waypoints, not authored art) plus two facility markers (Garage,
    /// Gas Station -- the minimum PROJECT-MAP-UNITY-MOBILE.md's vertical
    /// slice calls for), not a real Blackridge hub-world layout. No mesh,
    /// texture, or lighting authoring happened here -- primitives and
    /// Unity's default materials only.
    ///
    /// Run via `Assets/WTRL/Build Vertical Slice Scene`, or in batchmode
    /// with `-executeMethod WTRL.EditorTools.VerticalSliceSceneBuilder.Build`.
    /// Requires `HeroContentBuilder.BuildHero1965` to have already run
    /// (this method does not create content assets itself, only wires up
    /// existing ones).
    /// </summary>
    public static class VerticalSliceSceneBuilder
    {
        private const string ScenePath = "Assets/WrenchToRaceLegends/Scenes/VerticalSlice.unity";
        private const string HeroAssetPath = "Assets/WrenchToRaceLegends/Content/Generated/VehicleDefinition_Hero1965.asset";
        private const string MarshAssetPath = "Assets/WrenchToRaceLegends/Content/Generated/VehicleDefinition_MarshGen1.asset";
        private const string HeroModelPath = "Assets/WrenchToRaceLegends/Art/Vehicles/HeroCrownfire.fbx";
        private const string MarshModelPath = "Assets/WrenchToRaceLegends/Art/Vehicles/MarshNsx.fbx";

        // The source .blend files (racinggame/BlenderPipeline/export/
        // correct_axis_heroes/) author length along their own Y axis and
        // height along Z -- confirmed empirically via ModelBoundsDiagnostic
        // (raw bounds ~2.35 x 4.99 x 1.46, matching width/length/height in
        // that order, not Unity's width/height/length). A -90 degree
        // rotation about X swaps those two axes into Unity's convention
        // (Y = height, Z = length/forward).
        private static readonly Quaternion ModelAxisCorrection = Quaternion.Euler(-90f, 0f, 0f);

        [MenuItem("Assets/WTRL/Build Vertical Slice Scene")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var heroAsset = AssetDatabase.LoadAssetAtPath<VehicleDefinitionAsset>(HeroAssetPath);
            if (heroAsset == null)
            {
                Debug.LogError("VerticalSliceSceneBuilder: run HeroContentBuilder.BuildHero1965 first -- " +
                    HeroAssetPath + " doesn't exist yet.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGround();
            BuildTrackMesh();
            BuildCircuitMarkers();
            BuildFacilityMarker("Garage", new Vector3(-30, 0, -20), new Color(0.5f, 0.35f, 0.2f));
            BuildFacilityMarker("Gas Station", new Vector3(-30, 0, 20), new Color(0.2f, 0.5f, 0.2f));
            BuildPostProcessing();

            var careerHolder = new GameObject("CareerState").AddComponent<CareerStateHolder>();

            var vehicleGo = new GameObject("HeroVehicle");
            var controller = vehicleGo.AddComponent<VehicleRuntimeController>();
            controller.vehicle = heroAsset;
            vehicleGo.transform.position = new Vector3(
                (float)SampleContent.FoundryRowCircuitLine().Nodes[0].X, 0.35f,
                (float)SampleContent.FoundryRowCircuitLine().Nodes[0].Z);

            AttachVehicleModel(vehicleGo, HeroModelPath, new Color(0.55f, 0.05f, 0.05f, 1f)); // deep red paint

            var hud = vehicleGo.AddComponent<TelemetryHud>();
            SetPrivateField(hud, "vehicle", controller);

            var garageScreen = vehicleGo.AddComponent<GarageScreen>();
            SetPrivateField(garageScreen, "careerState", careerHolder);

            var dynoScreen = vehicleGo.AddComponent<DynoScreen>();
            SetPrivateField(dynoScreen, "vehicleAsset", heroAsset);
            SetPrivateField(dynoScreen, "careerState", careerHolder);

            var streaming = vehicleGo.AddComponent<WorldStreamingController>();
            SetPrivateField(streaming, "followTarget", vehicleGo.transform);

            BuildAudioRig(vehicleGo, controller);

            // Reloaded here, right before use, rather than held from
            // before EditorSceneManager.NewScene() ran -- an earlier
            // version loaded this alongside heroAsset up front and it
            // reproducibly evaluated as null by the time it was used,
            // even though the same path loaded fine everywhere else
            // (a standalone diagnostic method, the non-generic overload,
            // and this same call moved to just before use all confirm
            // the asset itself is fine). Likely explanation: creating a
            // new scene lets Unity unload ScriptableObject assets not
            // yet referenced by anything -- heroAsset survived because it
            // was assigned to `controller.vehicle` immediately, but
            // marshAsset held as a bare local for many lines had nothing
            // keeping the underlying native object alive in the interim.
            var marshAsset = AssetDatabase.LoadAssetAtPath<VehicleDefinitionAsset>(MarshAssetPath);
            if (marshAsset != null)
            {
                var rivalGo = new GameObject("MarshVehicle");
                var rivalController = rivalGo.AddComponent<AiVehicleController>();
                rivalController.vehicle = marshAsset;
                var startNode = SampleContent.FoundryRowCircuitLine().Nodes[2]; // stagger the start position
                rivalGo.transform.position = new Vector3((float)startNode.X, 0.35f, (float)startNode.Z);
                AttachVehicleModel(rivalGo, MarshModelPath, new Color(0.85f, 0.85f, 0.9f, 1f)); // silver paint
            }
            else
            {
                Debug.LogWarning("VerticalSliceSceneBuilder: " + MarshAssetPath +
                    " doesn't exist -- run MarshContentBuilder.BuildMarshGen1 to add the rival vehicle.");
            }

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            var followCam = cameraGo.AddComponent<SimpleFollowCamera>();
            SetPrivateField(followCam, "target", vehicleGo.transform);
            cameraGo.transform.position = vehicleGo.transform.position + new Vector3(0, 4, -8);
            camera.allowHDR = true;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f); // warm sunlight, not neutral white
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
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 300f;

            if (!AssetDatabase.IsValidFolder("Assets/WrenchToRaceLegends/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/WrenchToRaceLegends", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("VerticalSliceSceneBuilder: saved " + ScenePath);
        }

        /// <summary>Instantiates the real Blender-modeled vehicle body
        /// (see racinggame/BlenderPipeline/export/correct_axis_heroes/,
        /// the project's own documented "accepted blockout baseline" --
        /// not the disqualified procedural fleet output) as a child of
        /// the simulation GameObject, with the axis correction and a
        /// paint-color material applied to every renderer. The model has
        /// no real material/texture authoring yet (see REFERENCE-
        /// MODELING-ACCEPTANCE.md's own outstanding-work list), so a
        /// flat paint color is a genuine improvement over Unity's default
        /// magenta/gray missing-material look, not a finished paint job.</summary>
        private static void AttachVehicleModel(GameObject parent, string modelPath, Color paintColor)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null)
            {
                Debug.LogWarning($"VerticalSliceSceneBuilder: no model at {modelPath} -- vehicle will be invisible.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = ModelAxisCorrection;

            var paint = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = paintColor,
            };
            paint.SetFloat("_Metallic", 0.6f);
            paint.SetFloat("_Smoothness", 0.7f);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < materials.Length; i++) materials[i] = paint;
                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>A modest, safe-default URP post-processing stack
        /// (subtle bloom, ACES-style contrast lift, light vignette) --
        /// chosen because these are well-understood defaults that read
        /// as an improvement over no post-processing in nearly any
        /// lighting setup, not because anyone has looked at this scene
        /// and tuned it.</summary>
        private static void BuildPostProcessing()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.25f);

            var colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0.1f);
            colorAdjustments.contrast.Override(8f);
            colorAdjustments.saturation.Override(6f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.4f);

            if (!AssetDatabase.IsValidFolder("Assets/WrenchToRaceLegends/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/WrenchToRaceLegends", "Scenes");
            }
            AssetDatabase.CreateAsset(profile, "Assets/WrenchToRaceLegends/Scenes/VerticalSlicePostProcessing.asset");

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = profile;
        }

        private static void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(20, 1, 20); // Unity plane primitive is 10x10 units

            var asphalt = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.16f, 0.16f, 0.17f),
            };
            asphalt.SetFloat("_Smoothness", 0.25f);
            ground.GetComponent<Renderer>().sharedMaterial = asphalt;
        }

        /// <summary>Instantiates the real ribbon-road mesh generated by
        /// racinggame/BlenderPipeline/scripts/generate_world_tracks.py
        /// (following the exact same waypoint coordinates as
        /// `WTRL.Racing.SampleContent.FoundryRowCircuitLine`) as the
        /// actual track surface, replacing what used to be plain sphere/
        /// cylinder markers as the only visual indication of the
        /// circuit's shape. The node markers below are kept alongside it
        /// (useful for seeing the AI's actual waypoints, distinct from
        /// the road surface itself).
        ///
        /// Unlike the vehicle models, this mesh needs NO extra rotation
        /// on import: the exporter's known axis-remap behavior (which
        /// required a runtime rotation for the vehicle FBXs) was
        /// pre-compensated directly in the Blender export script's own
        /// vertex construction instead, confirmed via
        /// `ModelBoundsDiagnostic` to land exactly on the C# waypoints'
        /// real coordinates (center matches the node bounding box
        /// precisely) with an untouched, identity-rotation import. See
        /// generate_world_tracks.py's own comments for why baking the
        /// correction into mesh data was chosen over a runtime rotation
        /// this time -- a separate, reproduced bug meant Transform
        /// .rotation changes made after instantiation didn't reliably
        /// propagate to computed vertex positions in this environment's
        /// headless batchmode, making runtime rotation an unverifiable
        /// fix for a flat, direction-sensitive mesh like a track surface
        /// (unlike the vehicle bodies, where any residual orientation
        /// error is harder to notice at a glance).</summary>
        private static void BuildTrackMesh()
        {
            const string path = "Assets/WrenchToRaceLegends/Art/Tracks/foundry-row-circuit.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"VerticalSliceSceneBuilder: no track mesh at {path}.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "FoundryRowCircuitTrack";
            instance.transform.position = new Vector3(0, 0.02f, 0); // lifted slightly to avoid z-fighting with the ground plane

            var road = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.14f, 0.14f, 0.15f),
            };
            road.SetFloat("_Smoothness", 0.35f);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = road;
            }
        }

        private static void BuildCircuitMarkers()
        {
            var line = SampleContent.FoundryRowCircuitLine();
            var parent = new GameObject("FoundryRowCircuitMarkers");

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
                marker.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f); // squat cone-like marker cylinder
                marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            }
        }

        private static GameObject BuildFacilityMarker(string name, Vector3 position, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.position = position + Vector3.up * 1.5f;
            marker.transform.localScale = new Vector3(4, 3, 4);
            var renderer = marker.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            renderer.sharedMaterial = material;
            return marker;
        }

        private static void BuildAudioRig(GameObject vehicleGo, VehicleRuntimeController controller)
        {
            var audioController = vehicleGo.AddComponent<VehicleAudioController>();
            SetPrivateField(audioController, "vehicle", controller);

            foreach (var layerName in new[] { "engineMechanical", "intake", "exhaust", "driveline", "tire", "wind" })
            {
                var sourceGo = new GameObject($"Audio_{layerName}");
                sourceGo.transform.SetParent(vehicleGo.transform);
                sourceGo.transform.localPosition = Vector3.zero;
                var source = sourceGo.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                // No clips assigned -- see VehicleAudioController's own
                // doc comment; these are silent until real recorded
                // loops are authored and assigned.
                SetPrivateField(audioController, layerName, source);
            }
        }

        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                Debug.LogError($"VerticalSliceSceneBuilder: field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            field.SetValue(target, value);
        }
    }
}
