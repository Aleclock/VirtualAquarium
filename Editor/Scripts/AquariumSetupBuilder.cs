using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MultiCamCapture.Demo;

namespace MultiCamCapture.Editor
{
    public static class AquariumSetupBuilder
    {
        private const string BaseControllerPath = "Assets/Animations/AC_FishBase.controller";
        private const string SkyboxMatPath = "Assets/Materials/mSkyboxMarine.mat";
        private const string GroundMatPath = "Assets/Materials/mGroundSand.mat";
        private const string PrefabDir = "Assets/Prefabs/Fish";
        private const string SchoolsDir = "Assets/ScriptableObjects/Schools";
        private const string CatalogsDir = "Assets/ScriptableObjects/Catalogs";
        private const string CatalogAssetPath = "Assets/ScriptableObjects/Catalogs/DefaultAquariumCatalog.asset";

        private struct FishDefinition
        {
            public string folderName;
            public string fbxName;
            public string cleanName;
            public Vector2Int countRange;
            public float minSpeed;
            public float maxSpeed;
            public Vector2 scaleRange;
            public bool isPredator;
            public DepthZone depthPreference;

            public FishDefinition(
                string folder,
                string fbx,
                string name,
                Vector2Int count,
                float minSpd,
                float maxSpd,
                Vector2 scale,
                bool predator,
                DepthZone depth)
            {
                folderName = folder;
                fbxName = fbx;
                cleanName = name;
                countRange = count;
                minSpeed = minSpd;
                maxSpeed = maxSpd;
                scaleRange = scale;
                isPredator = predator;
                depthPreference = depth;
            }
        }

        private static readonly FishDefinition[] AllFishDefinitions = new FishDefinition[]
        {
            // --- Reef / Tropical Mid-Water Schooling Fish ---
            new FishDefinition("Clownfish", "Clownfish.fbx", "Clownfish", new Vector2Int(10, 18), 1.6f, 3.2f, new Vector2(0.25f, 0.35f), false, DepthZone.MidWater),
            new FishDefinition("Zebra Clown Fish", "ZebraClownFish.fbx", "Zebra Clown Fish", new Vector2Int(8, 15), 1.6f, 3.2f, new Vector2(0.25f, 0.35f), false, DepthZone.MidWater),
            new FishDefinition("Blue Tang", "BlueTang.fbx", "Blue Tang", new Vector2Int(6, 12), 2.0f, 4.0f, new Vector2(0.3f, 0.42f), false, DepthZone.MidWater),
            new FishDefinition("Yellow Tang", "YellowTang.fbx", "Yellow Tang", new Vector2Int(6, 12), 2.0f, 3.8f, new Vector2(0.3f, 0.42f), false, DepthZone.MidWater),
            new FishDefinition("Tang", "Tang.fbx", "Tang", new Vector2Int(6, 12), 2.0f, 3.8f, new Vector2(0.3f, 0.42f), false, DepthZone.MidWater),
            new FishDefinition("Tetra", "Tetra.fbx", "Neon Tetra", new Vector2Int(15, 25), 1.8f, 3.5f, new Vector2(0.2f, 0.3f), false, DepthZone.MidWater),
            new FishDefinition("Cardinal Fish", "CardinalFish.fbx", "Cardinal Fish", new Vector2Int(10, 18), 1.4f, 3.0f, new Vector2(0.25f, 0.35f), false, DepthZone.MidWater),
            new FishDefinition("Butterfly Fish", "ButterflyFish.fbx", "Butterfly Fish", new Vector2Int(4, 8), 1.5f, 3.2f, new Vector2(0.3f, 0.42f), false, DepthZone.MidWater),
            new FishDefinition("Moorish Idol", "MoorishIdol.fbx", "Moorish Idol", new Vector2Int(3, 6), 1.8f, 3.6f, new Vector2(0.32f, 0.45f), false, DepthZone.MidWater),
            new FishDefinition("Royal Gramma", "RoyalGramma.fbx", "Royal Gramma", new Vector2Int(4, 8), 1.5f, 3.0f, new Vector2(0.25f, 0.35f), false, DepthZone.MidWater),
            new FishDefinition("Parrot Fish", "ParrotFish.fbx", "Parrot Fish", new Vector2Int(3, 6), 1.8f, 3.5f, new Vector2(0.35f, 0.5f), false, DepthZone.MidWater),
            new FishDefinition("Coral Grouper", "CoralGrouper.fbx", "Coral Grouper", new Vector2Int(2, 4), 1.5f, 3.2f, new Vector2(0.35f, 0.5f), false, DepthZone.MidWater),
            new FishDefinition("Red Snapper", "RedSnapper.fbx", "Red Snapper", new Vector2Int(4, 8), 2.0f, 4.2f, new Vector2(0.35f, 0.5f), false, DepthZone.MidWater),
            new FishDefinition("Humphead", "Humphead.fbx", "Humphead Wrasse", new Vector2Int(1, 2), 1.2f, 2.8f, new Vector2(0.55f, 0.75f), false, DepthZone.MidWater),
            new FishDefinition("Mandarin Fish", "MandarinFish.fbx", "Mandarin Fish", new Vector2Int(2, 4), 1.0f, 2.2f, new Vector2(0.25f, 0.35f), false, DepthZone.MidWater),
            new FishDefinition("Flower Horn", "FlowerHorn.fbx", "Flower Horn", new Vector2Int(2, 4), 1.4f, 3.0f, new Vector2(0.35f, 0.5f), false, DepthZone.MidWater),
            new FishDefinition("Puffer", "Puffer.fbx", "Pufferfish", new Vector2Int(2, 4), 1.1f, 2.5f, new Vector2(0.3f, 0.45f), false, DepthZone.MidWater),

            // --- Freshwater / Pond Fish ---
            new FishDefinition("Goldfish", "Goldfish.fbx", "Goldfish", new Vector2Int(6, 12), 1.2f, 2.6f, new Vector2(0.3f, 0.45f), false, DepthZone.MidWater),
            new FishDefinition("Blue Goldfish", "BlueGoldfish.fbx", "Blue Goldfish", new Vector2Int(4, 8), 1.2f, 2.6f, new Vector2(0.3f, 0.45f), false, DepthZone.MidWater),
            new FishDefinition("Koi", "Koi.fbx", "Koi", new Vector2Int(5, 10), 1.4f, 3.0f, new Vector2(0.4f, 0.6f), false, DepthZone.MidWater),

            // --- Surface Dwellers (Upper Water Column) ---
            new FishDefinition("Betta", "Betta.fbx", "Betta", new Vector2Int(2, 3), 1.0f, 2.4f, new Vector2(0.28f, 0.38f), false, DepthZone.Surface),
            new FishDefinition("Sunfish", "Sunfish.fbx", "Ocean Sunfish", new Vector2Int(1, 1), 1.0f, 2.2f, new Vector2(0.65f, 0.95f), false, DepthZone.Surface),
            new FishDefinition("Tuna", "Tuna.fbx", "Bluefin Tuna", new Vector2Int(4, 8), 3.0f, 6.0f, new Vector2(0.55f, 0.8f), false, DepthZone.Surface),
            new FishDefinition("Swordfish", "Swordfish.fbx", "Swordfish", new Vector2Int(1, 2), 2.8f, 5.5f, new Vector2(0.6f, 0.9f), true, DepthZone.Surface),

            // --- Bottom / Benthic Dwellers (Floor / Sand Substrate) ---
            new FishDefinition("Flatfish", "Flatfish.fbx", "Flatfish", new Vector2Int(2, 4), 0.9f, 2.2f, new Vector2(0.35f, 0.48f), false, DepthZone.Bottom),
            new FishDefinition("Turbot", "Turbot.fbx", "Turbot", new Vector2Int(2, 4), 0.9f, 2.2f, new Vector2(0.35f, 0.48f), false, DepthZone.Bottom),
            new FishDefinition("Armored Catfish", "ArmoredCatfish.fbx", "Armored Catfish", new Vector2Int(3, 5), 1.0f, 2.5f, new Vector2(0.35f, 0.5f), false, DepthZone.Bottom),
            new FishDefinition("Blobfish", "Blobfish.fbx", "Blobfish", new Vector2Int(1, 2), 0.6f, 1.6f, new Vector2(0.35f, 0.5f), false, DepthZone.Bottom),
            new FishDefinition("Cowfish", "Cowfish.fbx", "Cowfish", new Vector2Int(2, 3), 0.9f, 2.2f, new Vector2(0.3f, 0.42f), false, DepthZone.Bottom),

            // --- Predators / Danger (Flee Trigger for Prey) ---
            new FishDefinition("Shark", "Shark.fbx", "Reef Shark", new Vector2Int(1, 2), 2.2f, 4.8f, new Vector2(0.6f, 0.9f), true, DepthZone.FullColumn),
            new FishDefinition("Goblin Shark", "GoblinShark.fbx", "Goblin Shark", new Vector2Int(1, 1), 1.2f, 2.6f, new Vector2(0.55f, 0.8f), true, DepthZone.Bottom),
            new FishDefinition("Anglerfish", "Anglerfish.fbx", "Anglerfish", new Vector2Int(1, 2), 0.8f, 2.0f, new Vector2(0.35f, 0.5f), true, DepthZone.Bottom),
            new FishDefinition("Lionfish", "Lionfish.fbx", "Lionfish", new Vector2Int(1, 3), 1.0f, 2.4f, new Vector2(0.35f, 0.5f), true, DepthZone.Bottom),
            new FishDefinition("Black Lion Fish", "BlackLionFish.fbx", "Black Lion Fish", new Vector2Int(1, 2), 1.0f, 2.4f, new Vector2(0.35f, 0.5f), true, DepthZone.Bottom),
            new FishDefinition("Piranha", "Piranha.fbx", "Piranha", new Vector2Int(6, 12), 2.2f, 4.5f, new Vector2(0.28f, 0.4f), true, DepthZone.MidWater),
        };

        // MenuItem removed to prevent accidental overwrite of scene setup and seabed decorations.
        // Use "Aquarium/Decorate Seabed" instead.
        public static void RunFullSetup()
        {
            EnsureDirectories();
            var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
            if (baseController == null)
            {
                Debug.LogError($"[AquariumSetupBuilder] Base AnimatorController not found at: {BaseControllerPath}");
                return;
            }

            // 0. Ensure all FBX animation clips are set to loopTime = true
            ConfigureLoopableAnimations();

            // 1. Generate / verify all AnimatorOverrideControllers
            var overrideControllers = GenerateOverrideControllers(baseController);

            // 2. Generate / verify all Fish Prefabs
            var prefabs = GenerateFishPrefabs(overrideControllers);

            // 3. Generate FishSchoolSO assets and FishCatalogSO
            var catalog = GenerateCatalogAndSchools(prefabs);

            // 4. Setup the Scene
            SetupScene(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AquariumSetupBuilder] Successfully completed full aquarium setup with loopable animations!");
        }

        private static void ConfigureLoopableAnimations()
        {
            for (int i = 0; i < AllFishDefinitions.Length; i++)
            {
                var def = AllFishDefinitions[i];
                string fbxPath = $"Assets/Resources/{def.folderName}/{def.fbxName}";

                ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null) continue;

                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                if (clips != null && clips.Length > 0)
                {
                    bool modified = false;
                    for (int c = 0; c < clips.Length; c++)
                    {
                        var clip = clips[c];
                        if (!clip.loopTime || !clip.loopPose)
                        {
                            clip.loopTime = true;
                            clip.loopPose = true;
                            clip.lockRootRotation = true;
                            clip.lockRootHeightY = true;
                            clip.lockRootPositionXZ = true;
                            clips[c] = clip;
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        importer.clipAnimations = clips;
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(SchoolsDir)) Directory.CreateDirectory(SchoolsDir);
            if (!Directory.Exists(CatalogsDir)) Directory.CreateDirectory(CatalogsDir);
        }

        private static Dictionary<string, AnimatorOverrideController> GenerateOverrideControllers(RuntimeAnimatorController baseController)
        {
            var result = new Dictionary<string, AnimatorOverrideController>();
            var baseClips = baseController.animationClips;

            for (int i = 0; i < AllFishDefinitions.Length; i++)
            {
                var def = AllFishDefinitions[i];
                string folder = $"Assets/Resources/{def.folderName}";
                string fbxPath = $"{folder}/{def.fbxName}";
                string cleanId = def.cleanName.Replace(" ", "");
                string aocPath = $"{folder}/AOC_{cleanId}.overrideController";

                if (!File.Exists(fbxPath))
                {
                    Debug.LogWarning($"[AquariumSetupBuilder] FBX not found: {fbxPath}");
                    continue;
                }

                var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                var fbxClips = allSubAssets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();

                AnimatorOverrideController aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(aocPath);
                if (aoc == null)
                {
                    aoc = new AnimatorOverrideController(baseController);
                    AssetDatabase.CreateAsset(aoc, aocPath);
                }
                else
                {
                    aoc.runtimeAnimatorController = baseController;
                }

                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                foreach (var baseClip in baseClips)
                {
                    string baseName = baseClip.name;
                    AnimationClip match = null;

                    if (baseName.Contains("Swimming_Normal"))
                        match = fbxClips.FirstOrDefault(c => c.name.Contains("Swimming_Normal") || c.name.Contains("Swim"));
                    else if (baseName.Contains("Swimming_Impulse"))
                        match = fbxClips.FirstOrDefault(c => c.name.Contains("Swimming_Impulse") || c.name.Contains("Swim"));
                    else if (baseName.Contains("Swimming_Fast"))
                        match = fbxClips.FirstOrDefault(c => c.name.Contains("Swimming_Fast") || c.name.Contains("Fast"));
                    else if (baseName.Contains("Attack"))
                        match = fbxClips.FirstOrDefault(c => c.name.Contains("Attack"));
                    else if (baseName.Contains("Death"))
                        match = fbxClips.FirstOrDefault(c => c.name.Contains("Death") || c.name.Contains("Die"));
                    else if (baseName.Contains("Out_Of_Water") || baseName.Contains("OutOfWater"))
                        match = fbxClips.FirstOrDefault(c => c.name.Contains("Out_Of_Water") || c.name.Contains("Flop"));

                    if (match == null)
                    {
                        match = fbxClips.FirstOrDefault(c => c.name.Contains("Swim")) ?? (fbxClips.Count > 0 ? fbxClips[0] : null);
                    }

                    if (match != null)
                    {
                        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(baseClip, match));
                    }
                }

                aoc.ApplyOverrides(overrides);
                EditorUtility.SetDirty(aoc);
                result[def.cleanName] = aoc;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static Dictionary<string, GameObject> GenerateFishPrefabs(Dictionary<string, AnimatorOverrideController> aocs)
        {
            var result = new Dictionary<string, GameObject>();

            for (int i = 0; i < AllFishDefinitions.Length; i++)
            {
                var def = AllFishDefinitions[i];
                string cleanId = def.cleanName.Replace(" ", "");
                string prefabPath = $"{PrefabDir}/{cleanId}.prefab";
                string fbxPath = $"Assets/Resources/{def.folderName}/{def.fbxName}";

                GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbxObj == null)
                {
                    Debug.LogWarning($"[AquariumSetupBuilder] Could not load FBX: {fbxPath}");
                    continue;
                }

                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = cleanId;

                var anim = instance.GetComponentInChildren<Animator>();
                if (anim == null)
                {
                    anim = instance.AddComponent<Animator>();
                }
                if (aocs.TryGetValue(def.cleanName, out var aoc))
                {
                    anim.runtimeAnimatorController = aoc;
                }

                var agent = instance.GetComponent<FishAgent>();
                if (agent == null)
                {
                    agent = instance.AddComponent<FishAgent>();
                }
                agent.MinSpeed = def.minSpeed;
                agent.MaxSpeed = def.maxSpeed;
                agent.FlockId = i;
                agent.IsPredator = def.isPredator;
                agent.DepthPreference = def.depthPreference;

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);

                result[def.cleanName] = prefab;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static FishCatalogSO GenerateCatalogAndSchools(Dictionary<string, GameObject> prefabs)
        {
            FishCatalogSO catalog = AssetDatabase.LoadAssetAtPath<FishCatalogSO>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FishCatalogSO>();
                catalog.catalogName = "Tropical & Marine Aquarium Catalog";
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            var serializedCatalog = new SerializedObject(catalog);
            var schoolsProp = serializedCatalog.FindProperty("schools");
            schoolsProp.ClearArray();
            serializedCatalog.ApplyModifiedProperties();

            for (int i = 0; i < AllFishDefinitions.Length; i++)
            {
                var def = AllFishDefinitions[i];
                string cleanId = def.cleanName.Replace(" ", "");
                string schoolPath = $"{SchoolsDir}/School_{cleanId}.asset";

                if (!prefabs.TryGetValue(def.cleanName, out var prefab))
                {
                    continue;
                }

                FishSchoolSO schoolSO = AssetDatabase.LoadAssetAtPath<FishSchoolSO>(schoolPath);
                if (schoolSO == null)
                {
                    schoolSO = ScriptableObject.CreateInstance<FishSchoolSO>();
                    AssetDatabase.CreateAsset(schoolSO, schoolPath);
                }

                schoolSO.speciesName = def.cleanName;
                schoolSO.fishPrefab = prefab;
                schoolSO.countRange = def.countRange;
                schoolSO.flockId = i;
                schoolSO.minSpeed = def.minSpeed;
                schoolSO.maxSpeed = def.maxSpeed;
                schoolSO.scaleMultiplierRange = def.scaleRange;
                schoolSO.isPredator = def.isPredator;
                schoolSO.depthPreference = def.depthPreference;

                EditorUtility.SetDirty(schoolSO);
                catalog.AddSchool(schoolSO);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void SetupScene(FishCatalogSO catalog)
        {
            // 1. Skybox material
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMatPath);
            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
                RenderSettings.ambientMode = AmbientMode.Skybox;
            }

            // 2. Sand Floor Plane
            Material sandMat = AssetDatabase.LoadAssetAtPath<Material>(GroundMatPath);
            GameObject sandFloor = GameObject.Find("Sand Floor");
            if (sandFloor == null)
            {
                sandFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                sandFloor.name = "Sand Floor";
            }
            sandFloor.transform.position = new Vector3(0f, -9.05f, 0f);
            sandFloor.transform.localScale = new Vector3(10f, 1f, 8f); // 100m x 80m
            if (sandMat != null)
            {
                var renderer = sandFloor.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = sandMat;
            }

            // 3. Aquarium Controller
            GameObject controllerObj = GameObject.Find("Aquarium Controller");
            if (controllerObj == null)
            {
                controllerObj = new GameObject("Aquarium Controller");
            }
            controllerObj.transform.position = Vector3.zero;

            var bounds = controllerObj.GetComponent<AquariumBounds>();
            if (bounds == null) bounds = controllerObj.AddComponent<AquariumBounds>();

            // Configure bounds size (50m width, 18m height, 30m depth)
            var serBounds = new SerializedObject(bounds);
            serBounds.FindProperty("size").vector3Value = new Vector3(50f, 18f, 30f);
            serBounds.FindProperty("margin").floatValue = 3.5f;
            serBounds.FindProperty("boundaryRepulsionWeight").floatValue = 6.0f;
            serBounds.ApplyModifiedProperties();

            // Fish Spawner
            var spawner = controllerObj.GetComponent<FishSpawner>();
            if (spawner == null) spawner = controllerObj.AddComponent<FishSpawner>();

            var serSpawner = new SerializedObject(spawner);
            serSpawner.FindProperty("aquariumBounds").objectReferenceValue = bounds;
            serSpawner.FindProperty("fishCatalog").objectReferenceValue = catalog;
            serSpawner.FindProperty("spawnOnStart").boolValue = true;
            serSpawner.ApplyModifiedProperties();

            // 4. Main Camera Positioning
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                camObj.transform.position = new Vector3(0f, 0f, -32f);
                camObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                var cam = camObj.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.fieldOfView = 60f;
                    cam.farClipPlane = 300f;
                }
            }

            // 5. Lighting
            var lightObj = GameObject.Find("Directional Light");
            if (lightObj != null)
            {
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var light = lightObj.GetComponent<Light>();
                if (light != null)
                {
                    light.color = new Color(0.85f, 0.95f, 1.0f);
                    light.intensity = 1.2f;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }
    }
}
