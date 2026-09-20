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
        /// the simulation GameObject, with the axis correction applied.
        /// The model has no real material/texture authoring yet (see
        /// REFERENCE-MODELING-ACCEPTANCE.md's own outstanding-work
        /// list), but its mesh objects DO carry real per-part names
        /// (confirmed via a direct Blender name dump: BODY_SHELL,
        /// TIRE.NNN, RIM.NNN, GLASSHOUSE, HEADLAMP/TAIL_LAMP, etc.), so
        /// materials are now assigned per part-category instead of
        /// painting every renderer -- including tires and glass -- the
        /// single flat body-paint color, which was a real visual defect
        /// in the earlier pass.</summary>
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

            var paint = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = paintColor };
            paint.SetFloat("_Metallic", 0.6f);
            paint.SetFloat("_Smoothness", 0.7f);

            var tire = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.05f, 0.05f, 0.05f, 1f) };
            tire.SetFloat("_Metallic", 0f);
            tire.SetFloat("_Smoothness", 0.15f);

            var rim = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.75f, 0.75f, 0.78f, 1f) };
            rim.SetFloat("_Metallic", 0.9f);
            rim.SetFloat("_Smoothness", 0.85f);

            var glass = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.08f, 0.1f, 0.12f, 0.55f) };
            glass.SetFloat("_Metallic", 0.1f);
            glass.SetFloat("_Smoothness", 0.95f);
            glass.SetFloat("_Surface", 1f); // transparent
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.SetInt("_ZWrite", 0);
            glass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            var trim = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.82f, 0.82f, 0.85f, 1f) };
            trim.SetFloat("_Metallic", 0.95f);
            trim.SetFloat("_Smoothness", 0.9f);

            var interior = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.12f, 0.1f, 0.09f, 1f) };
            interior.SetFloat("_Metallic", 0f);
            interior.SetFloat("_Smoothness", 0.3f);

            var lamp = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.9f, 0.85f, 0.7f, 1f) };
            lamp.SetFloat("_Metallic", 0.2f);
            lamp.SetFloat("_Smoothness", 0.9f);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var partName = renderer.gameObject.name.ToUpperInvariant();
                Material chosen;
                if (partName.Contains("TIRE")) chosen = tire;
                else if (partName.Contains("RIM")) chosen = rim;
                else if (partName.Contains("GLASSHOUSE")) chosen = glass;
                else if (partName.Contains("MIRROR") || partName.Contains("BUMPER") || partName.Contains("GRILLE")
                    || partName.Contains("HANDLE") || partName.Contains("VALANCE") || partName.Contains("ROCKER")
                    || partName.Contains("PILLAR") || partName.Contains("BEZEL")) chosen = trim;
                else if (partName.Contains("HEADLAMP") || partName.Contains("TAIL_LAMP") || partName.Contains("LAMP_PANEL")) chosen = lamp;
                else if (partName.Contains("SEAT") || partName.Contains("DASHBOARD") || partName.Contains("STEERING")
                    || partName.Contains("BRAKE") || partName.Contains("V8_BLOCK") || partName.Contains("INTAKE")) chosen = interior;
                else chosen = paint;

                var materials = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < materials.Length; i++) materials[i] = chosen;
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

        /// <summary>The surrounding terrain. Previously a single flat
        /// `PrimitiveType.Plane` with zero height variation at all --
        /// a real, previously-acknowledged limitation, not just a texture
        /// problem. This now builds an actual subdivided mesh (240x240
        /// quads over a 600x600m area, centered on the track, not on the
        /// world origin) with real per-vertex height displacement from a
        /// layered `Mathf.PerlinNoise` field, so the ground genuinely
        /// rolls rather than only looking textured.
        ///
        /// The displacement is faded to exactly zero inside a rectangular
        /// flat zone (`FlatZoneMin`/`FlatZoneMax`) sized to the REAL
        /// Foundry Row footprint plus margin. This matters because
        /// `Racing.SampleContent.FoundryRowCircuitLine()`'s actual
        /// waypoints span x:[-20,220], z:[0,100] -- NOT centered on the
        /// world origin. An earlier draft of this method centered both
        /// the terrain and its flat zone on (0,0,0) with only a 45m
        /// radius, which would have left most of the track (whose real
        /// bounding-box center is (100,50), radius ~150m from the
        /// origin) sitting on sloped, noisy terrain while the track's own
        /// mesh stayed flat -- caught by checking the actual waypoint
        /// coordinates against the flat-zone size before ever rebuilding
        /// the scene, not by a screenshot. The flat zone also covers both
        /// facility markers (at x=-30, z=-20/+20). Everything placed at
        /// fixed, non-adjustable Y coordinates elsewhere in this builder
        /// is therefore not undercut or floated by terrain height.
        ///
        /// Still uses the same procedurally generated grass/dirt texture
        /// as before (tiled via UVs baked into the mesh). Verified by
        /// checking the built mesh's own vertex Y-value range (min/max
        /// logged) and by confirming the flat-zone rectangle covers the
        /// real track/facility coordinates, rather than a screenshot --
        /// no interactive-Editor visual confirmation exists in this
        /// environment.</summary>
        private const float TerrainHalfSizeM = 300f; // terrain spans this far each side of TerrainCenter
        private const int TerrainResolution = 120; // quads per side
        private const float TerrainHeightM = 6f;
        private static readonly Vector2 TerrainCenter = new(100f, 50f); // matches Foundry Row's real bounding-box center
        private static readonly Vector2 FlatZoneMin = new(-60f, -40f); // Foundry Row bbox (-20..220, 0..100) plus 40m margin, covers facility markers too
        private static readonly Vector2 FlatZoneMax = new(260f, 140f);
        private const float FlatZoneFeatherM = 25f;

        private static void BuildGround()
        {
            var ground = new GameObject("Ground");
            ground.transform.position = Vector3.zero;

            var meshFilter = ground.AddComponent<MeshFilter>();
            var meshRenderer = ground.AddComponent<MeshRenderer>();
            var meshCollider = ground.AddComponent<MeshCollider>();

            var mesh = BuildTerrainMesh();
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh; // was left null in an earlier draft -- ground had a visual mesh but no actual collision surface

            var groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/WrenchToRaceLegends/Art/Environment/Textures/world_ground_grass.png");

            var grass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (groundTex != null)
            {
                grass.mainTexture = groundTex;
            }
            else
            {
                grass.color = new Color(0.16f, 0.16f, 0.17f);
            }
            grass.SetFloat("_Smoothness", 0.1f);
            meshRenderer.sharedMaterial = grass;
        }

        /// <summary>Signed distance (meters) from point (x,z) to outside the
        /// [FlatZoneMin, FlatZoneMax] rectangle. 0 or negative = inside.</summary>
        private static float DistanceOutsideFlatZone(float x, float z)
        {
            var dx = Mathf.Max(FlatZoneMin.x - x, 0f, x - FlatZoneMax.x);
            var dz = Mathf.Max(FlatZoneMin.y - z, 0f, z - FlatZoneMax.y);
            return new Vector2(dx, dz).magnitude;
        }

        private static Mesh BuildTerrainMesh()
        {
            var verticesPerSide = TerrainResolution + 1;
            var vertices = new Vector3[verticesPerSide * verticesPerSide];
            var uvs = new Vector2[vertices.Length];
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            for (var zi = 0; zi < verticesPerSide; zi++)
            {
                for (var xi = 0; xi < verticesPerSide; xi++)
                {
                    var u = (float)xi / TerrainResolution;
                    var v = (float)zi / TerrainResolution;
                    var x = TerrainCenter.x + (u - 0.5f) * TerrainHalfSizeM * 2f;
                    var z = TerrainCenter.y + (v - 0.5f) * TerrainHalfSizeM * 2f;

                    var noise = Mathf.PerlinNoise(x * 0.03f, z * 0.03f) * 0.7f
                        + Mathf.PerlinNoise(x * 0.08f, z * 0.08f) * 0.3f;
                    var height = (noise - 0.5f) * 2f * TerrainHeightM;

                    var distanceOutsideFlatZone = DistanceOutsideFlatZone(x, z);
                    var falloff = Mathf.SmoothStep(0f, 1f, distanceOutsideFlatZone / FlatZoneFeatherM);
                    height *= falloff;

                    // The Ground GameObject stays at world-origin transform.position
                    // (Vector3.zero, set in BuildGround), so vertices are authored
                    // directly in world/local-equals-world coordinates -- NOT offset
                    // by -TerrainCenter, which would silently re-center the mesh back
                    // onto (0,0,0) and undo the whole point of TerrainCenter.
                    var index = zi * verticesPerSide + xi;
                    vertices[index] = new Vector3(x, height, z);
                    uvs[index] = new Vector2(x * 0.2f, z * 0.2f); // tiled roughly every 5m
                    minY = Mathf.Min(minY, height);
                    maxY = Mathf.Max(maxY, height);
                }
            }

            var triangles = new int[TerrainResolution * TerrainResolution * 6];
            var t = 0;
            for (var zi = 0; zi < TerrainResolution; zi++)
            {
                for (var xi = 0; xi < TerrainResolution; xi++)
                {
                    var a = zi * verticesPerSide + xi;
                    var b = a + 1;
                    var c = a + verticesPerSide;
                    var d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }

            var mesh = new Mesh { name = "TerrainMesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log($"VerticalSliceSceneBuilder: terrain height range [{minY:F2}, {maxY:F2}] m, flat zone x:[{FlatZoneMin.x},{FlatZoneMax.x}] z:[{FlatZoneMin.y},{FlatZoneMax.y}].");

            return mesh;
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

            // Textures are separately-imported PNG assets, not extracted
            // from the FBX -- Blender's `embed_textures=True` export
            // produced a valid-looking FBX (confirmed the texture bytes
            // are present via a raw string search of the file), but
            // Unity's FBX importer never wired the resulting material's
            // `mainTexture` regardless of how the image was packed/saved
            // on the Blender side. Rather than keep chasing that
            // interop gap, the generator now also writes each texture as
            // a plain PNG (racinggame/BlenderPipeline/export/
            // world_tracks/textures/), copied here and loaded directly.
            var asphaltTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/WrenchToRaceLegends/Art/Tracks/Textures/foundry-row-circuit_asphalt.png");
            var barrierTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/WrenchToRaceLegends/Art/Tracks/Textures/foundry-row-circuit_barrier_stripe.png");

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var isBarrier = renderer.gameObject.name.Contains("barrier");
                var tex = isBarrier ? barrierTex : asphaltTex;
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (tex != null) mat.mainTexture = tex;
                mat.SetFloat("_Smoothness", isBarrier ? 0.5f : 0.3f);
                renderer.sharedMaterial = mat;
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

        /// <summary>Builds a facility placeholder as a small building
        /// silhouette (a box body plus a peaked roof wedge) instead of a
        /// single bare colored cube -- so "Garage" and "Gas Station" read
        /// as distinct structures at a glance rather than two identical
        /// cubes that only differ by color. Still a placeholder, not real
        /// building art (no real facility geometry exists in the research
        /// corpus to model against).</summary>
        private static GameObject BuildFacilityMarker(string name, Vector3 position, Color color)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 1.5f, 0);
            body.transform.localScale = new Vector3(4, 3, 4);
            var bodyMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            bodyMaterial.SetFloat("_Smoothness", 0.2f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(root.transform);
            roof.transform.localPosition = new Vector3(0, 3.3f, 0);
            roof.transform.localRotation = Quaternion.Euler(0, 45, 0);
            roof.transform.localScale = new Vector3(3.1f, 0.3f, 3.1f);
            var roofMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.25f, 0.22f, 0.2f) };
            roofMaterial.SetFloat("_Smoothness", 0.35f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;

            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign";
            sign.transform.SetParent(root.transform);
            sign.transform.localPosition = new Vector3(0, 3.9f, 2.2f);
            sign.transform.localScale = new Vector3(2.4f, 0.5f, 0.1f);
            var signMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
            signMaterial.EnableKeyword("_EMISSION");
            signMaterial.SetColor("_EmissionColor", color * 2f);
            sign.GetComponent<Renderer>().sharedMaterial = signMaterial;

            return root;
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
