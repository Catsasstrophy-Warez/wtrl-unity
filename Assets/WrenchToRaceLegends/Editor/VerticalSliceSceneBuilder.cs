using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

        [MenuItem("Assets/WTRL/Build Vertical Slice Scene")]
        public static void Build()
        {
            var heroAsset = AssetDatabase.LoadAssetAtPath<VehicleDefinitionAsset>(HeroAssetPath);
            if (heroAsset == null)
            {
                Debug.LogError("VerticalSliceSceneBuilder: run HeroContentBuilder.BuildHero1965 first -- " +
                    HeroAssetPath + " doesn't exist yet.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGround();
            BuildCircuitMarkers();
            var garage = BuildFacilityMarker("Garage", new Vector3(-30, 0, -20), new Color(0.5f, 0.35f, 0.2f));
            var gasStation = BuildFacilityMarker("Gas Station", new Vector3(-30, 0, 20), new Color(0.2f, 0.5f, 0.2f));

            var careerHolder = new GameObject("CareerState").AddComponent<CareerStateHolder>();

            var vehicleGo = new GameObject("HeroVehicle");
            var controller = vehicleGo.AddComponent<VehicleRuntimeController>();
            controller.vehicle = heroAsset;
            vehicleGo.transform.position = new Vector3(
                (float)SampleContent.FoundryRowCircuitLine().Nodes[0].X, 0.5f,
                (float)SampleContent.FoundryRowCircuitLine().Nodes[0].Z);

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

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();
            var followCam = cameraGo.AddComponent<SimpleFollowCamera>();
            SetPrivateField(followCam, "target", vehicleGo.transform);
            cameraGo.transform.position = vehicleGo.transform.position + new Vector3(0, 4, -8);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            if (!AssetDatabase.IsValidFolder("Assets/WrenchToRaceLegends/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/WrenchToRaceLegends", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("VerticalSliceSceneBuilder: saved " + ScenePath);
        }

        private static void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(20, 1, 20); // Unity plane primitive is 10x10 units
        }

        private static void BuildCircuitMarkers()
        {
            var line = SampleContent.FoundryRowCircuitLine();
            var parent = new GameObject("FoundryRowCircuitMarkers");
            for (var i = 0; i < line.Nodes.Count; i++)
            {
                var node = line.Nodes[i];
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Node_{i}";
                marker.transform.SetParent(parent.transform);
                marker.transform.position = new Vector3((float)node.X, 0.5f, (float)node.Z);
                marker.transform.localScale = Vector3.one * 1.5f;
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
